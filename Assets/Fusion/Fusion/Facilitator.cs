using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Rooms;
using Ubiq.Spawning;
using Ubiq.Voip;
using Ubiq.Logging;
using Ubiq.Avatars;
using System.Collections.Generic;

public class Facilitator : MonoBehaviour
{
    private NetworkScene networkScene;
    private RoomClient roomClient;
    private ExperimentLogEmitter appEvents;
    private VoipPeerConnectionManager voipManager;
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    private float lastPingTime = 0f;
    private const float PING_INTERVAL = 1f;

    [SerializeField]
    private string roomName = "Experiment Room";

    [SerializeField]
    private bool joinRoomOnStart = true;

    [SerializeField]
    private bool enableVoice = true;

    [SerializeField]
    private bool enableAvatars = true;

    void Start()
    {
        // Initialize loggers
        appEvents = new ExperimentLogEmitter(this);

        // Get required components
        networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponent<RoomClient>();
        voipManager = networkScene.GetComponent<VoipPeerConnectionManager>();

        // Configure RoomClient to maintain connection
        if (roomClient != null)
        {
            roomClient.timeoutBehaviour = RoomClient.TimeoutBehaviour.Reconnect;
        }

        // Subscribe to room events
        roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);
        roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);

        // Subscribe to peer connection events
        if (voipManager != null)
        {
            voipManager.OnPeerConnection.AddListener(OnPeerConnectionCreated);
        }

        // Configure avatar visibility
        var avatarManager = networkScene.GetComponent<AvatarManager>();
        if (avatarManager != null)
        {
            var localAvatar = avatarManager.LocalAvatar;
            if (localAvatar != null)
            {
                localAvatar.gameObject.SetActive(false);
            }
        }

        if (joinRoomOnStart)
        {
            CreateRoom();
        }
    }

    void Update()
    {
        // Send periodic pings to maintain connection
        if (Time.time - lastPingTime > PING_INTERVAL)
        {
            lastPingTime = Time.time;
            if (roomClient != null && roomClient.JoinedRoom)
            {
                roomClient.Ping();
            }
        }
    }

    private void OnPeerConnectionCreated(VoipPeerConnection connection)
    {
        peerConnections.Add(connection);
        UpdateVoiceSettings();
    }

    private void UpdateVoiceSettings()
    {
        foreach (var connection in peerConnections)
        {
            if (connection != null)
            {
                connection.gameObject.SetActive(enableVoice);
            }
        }
    }

    public void CreateRoom()
    {
        appEvents.Log("Facilitator creating room", roomName);
        roomClient.Join(roomName, true);
        roomClient.Ping();
    }

    private void OnJoinedRoom(IRoom room)
    {
        appEvents.Log("Facilitator joined room", room.Name);
        ConfigureFacilitatorComponents();
    }

    private void OnRoomUpdated(IRoom room)
    {
        if (roomClient.Room == room)
        {
            appEvents.Log("Facilitator room updated", room.Name);
        }
    }

    private void ConfigureFacilitatorComponents()
    {
        // Configure avatar manager
        var avatarManager = networkScene.GetComponent<AvatarManager>();
        if (avatarManager != null)
        {
            var localAvatar = avatarManager.LocalAvatar;
            if (localAvatar != null)
            {
                localAvatar.gameObject.SetActive(false);
            }

            if (!enableAvatars)
            {
                foreach (var avatar in avatarManager.Avatars)
                {
                    if (avatar != localAvatar)
                    {
                        avatar.gameObject.SetActive(false);
                    }
                }
            }
        }

        UpdateVoiceSettings();

        var spawnManager = networkScene.GetComponent<NetworkSpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.enabled = true;
        }
    }

    void OnDestroy()
    {
        if (roomClient != null)
        {
            roomClient.OnRoomUpdated.RemoveListener(OnRoomUpdated);
            roomClient.OnJoinedRoom.RemoveListener(OnJoinedRoom);
        }
        if (voipManager != null)
        {
            voipManager.OnPeerConnection.RemoveListener(OnPeerConnectionCreated);
        }
    }
}
