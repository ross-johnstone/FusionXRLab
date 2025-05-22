using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Rooms;
using Ubiq.Spawning;
using Ubiq.Voip;
using Ubiq.Logging;
using Ubiq.Avatars;
using System.Collections.Generic;

/// <summary>
/// Facilitator component that manages room creation and connection maintenance.
/// Provides functionality to create rooms and handle participant connections.
/// </summary>
public class Facilitator : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private string roomName = "Experiment Room";
    [SerializeField] private bool enableVoice = true;
    #endregion

    #region Private Fields
    private NetworkScene networkScene;
    private RoomClient roomClient;
    private ExperimentLogEmitter appEvents;
    private VoipPeerConnectionManager voipManager;
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    private float lastPingTime = 0f;
    private const float PING_INTERVAL = 1f;
    private AvatarManager avatarManager;
    private bool isAvatarHidden = true;
    #endregion

    #region Unity Lifecycle Methods
    void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        ConfigureAvatarVisibility();
    }

    void Update()
    {
        MaintainConnection();
    }

    void OnDestroy()
    {
        CleanupEventListeners();
    }
    #endregion

    #region Initialization Methods
    private void InitializeComponents()
    {
        // Initialize logging
        appEvents = new ExperimentLogEmitter(this);

        // Get required components
        networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponent<RoomClient>();
        voipManager = networkScene.GetComponent<VoipPeerConnectionManager>();
        avatarManager = AvatarManager.Find(this);

        // Configure RoomClient for connection maintenance
        if (roomClient != null)
        {
            roomClient.timeoutBehaviour = RoomClient.TimeoutBehaviour.Reconnect;
        }
    }

    private void SetupEventListeners()
    {
        if (roomClient != null)
        {
            roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);
            roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
        }

        if (voipManager != null)
        {
            voipManager.OnPeerConnection.AddListener(OnPeerConnectionCreated);
        }

        if (avatarManager != null)
        {
            avatarManager.OnAvatarCreated.AddListener(OnAvatarCreated);
        }
    }

    private void ConfigureAvatarVisibility()
    {
        if (avatarManager != null)
        {
            // Set the avatar prefab to null to prevent avatar creation
            avatarManager.avatarPrefab = null;
            isAvatarHidden = true;
        }
    }
    #endregion

    #region Room Management Methods
    public void CreateRoom()
    {
        if (!roomClient.JoinedRoom)
        {
            appEvents.Log("Facilitator creating room", roomName);
            roomClient.Join(roomName, true);
            roomClient.Ping();
        }
    }

    private void MaintainConnection()
    {
        if (Time.time - lastPingTime > PING_INTERVAL)
        {
            lastPingTime = Time.time;
            if (roomClient != null && roomClient.JoinedRoom)
            {
                roomClient.Ping();
            }
        }
    }
    #endregion

    #region Event Handlers
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

    private void OnPeerConnectionCreated(VoipPeerConnection connection)
    {
        peerConnections.Add(connection);
        UpdateVoiceSettings();
    }

    private void OnAvatarCreated(Ubiq.Avatars.Avatar avatar)
    {
        if (avatar.IsLocal)
        {
            // Ensure local avatar stays hidden
            avatar.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Configuration Methods
    private void ConfigureFacilitatorComponents()
    {
        ConfigureAvatars();
        UpdateVoiceSettings();
        EnableSpawnManager();
    }

    private void ConfigureAvatars()
    {
        if (avatarManager != null)
        {
            // Ensure avatar prefab is null to prevent avatar creation
            avatarManager.avatarPrefab = null;
            isAvatarHidden = true;

            // Hide any existing avatars
            foreach (var avatar in avatarManager.Avatars)
            {
                if (avatar.IsLocal)
                {
                    avatar.gameObject.SetActive(false);
                }
            }
        }
    }

    public void ToggleFacilitatorAvatar()
    {
        if (avatarManager != null)
        {
            if (isAvatarHidden)
            {
                // Show avatar by setting the default prefab
                // Note: You'll need to assign the default avatar prefab in the inspector
                // or get it from somewhere in your project
                appEvents.Log("Facilitator avatar shown");
            }
            else
            {
                // Hide avatar by setting prefab to null
                avatarManager.avatarPrefab = null;
                appEvents.Log("Facilitator avatar hidden");
            }
            
            isAvatarHidden = !isAvatarHidden;
        }
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

    private void EnableSpawnManager()
    {
        var spawnManager = networkScene.GetComponent<NetworkSpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.enabled = true;
        }
    }
    #endregion

    #region Cleanup Methods
    private void CleanupEventListeners()
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
        if (avatarManager != null)
        {
            avatarManager.OnAvatarCreated.RemoveListener(OnAvatarCreated);
        }
    }
    #endregion

    #region Editor Integration
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(Facilitator))]
    public class FacilitatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Facilitator facilitator = (Facilitator)target;
            
            UnityEditor.EditorGUILayout.Space();
            if (GUILayout.Button("Create Room"))
            {
                facilitator.CreateRoom();
            }

            UnityEditor.EditorGUILayout.Space();
            if (GUILayout.Button("Toggle Facilitator Avatar"))
            {
                facilitator.ToggleFacilitatorAvatar();
            }
        }
    }
    #endif
    #endregion
}
