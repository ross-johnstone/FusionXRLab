using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Rooms;
using Ubiq.Spawning;
using Ubiq.Voip;
using Ubiq.Logging;
using Ubiq.Avatars;
using System.Collections.Generic;
using UnityEngine.XR;

public class RoomManager : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField] private string roomName = "Experiment Room";
    [SerializeField] private bool enableVoice = true;
    [SerializeField] private bool forceFacilitatorMode = false;
    [SerializeField] private bool forceUserMode = false;
    #endregion

    #region Private Fields
    private NetworkScene networkScene;
    private RoomClient roomClient;
    private ComponentLogEmitter appEvents;
    private VoipPeerConnectionManager voipManager;
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    private float lastPingTime = 0f;
    private const float PING_INTERVAL = 1f;
    private AvatarManager avatarManager;
    private bool isAvatarHidden = true;
    private float lastDiscoveryTime = 0f;
    private const float DISCOVERY_INTERVAL = 2f;
    private bool isFacilitator;
    #endregion

    #region Unity Lifecycle Methods
    void Start()
    {
        InitializeComponents();
        DetermineRole();
        SetupEventListeners();
        ConfigureAvatarVisibility();
    }

    void Update()
    {
        if (isFacilitator)
        {
            MaintainConnection();
        }
        else
        {
            // User mode: Periodically try to discover rooms if we haven't joined one
            if (!roomClient.JoinedRoom && Time.time - lastDiscoveryTime > DISCOVERY_INTERVAL)
            {
                StartRoomDiscovery();
            }
        }
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
        appEvents = new ComponentLogEmitter(this, Ubiq.Logging.EventType.Application);

        // Get required components
        networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponent<RoomClient>();
        voipManager = networkScene.GetComponent<VoipPeerConnectionManager>();
        avatarManager = AvatarManager.Find(this);

        if (roomClient == null)
        {
            Debug.LogError("RoomManager: RoomClient component not found!");
            return;
        }

        // Configure RoomClient for connection maintenance
        roomClient.timeoutBehaviour = RoomClient.TimeoutBehaviour.Reconnect;
    }

    private void DetermineRole()
    {
        // Check for forced modes first
        if (forceFacilitatorMode)
        {
            isFacilitator = true;
        }
        else if (forceUserMode)
        {
            isFacilitator = false;
        }
        else
        {
            // Auto-detect based on platform
            #if UNITY_EDITOR
            isFacilitator = true;
            #else
            isFacilitator = false;
            #endif
        }

        appEvents.Log($"RoomManager: Running in {(isFacilitator ? "Facilitator" : "User")} mode");

        if (isFacilitator)
        {
            CreateRoom();
        }
        else
        {
            StartRoomDiscovery();
        }
    }

    private void SetupEventListeners()
    {
        if (roomClient != null)
        {
            roomClient.OnRooms.AddListener(OnRoomsDiscovered);
            roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
            roomClient.OnJoinRejected.AddListener(OnJoinRejected);
            roomClient.OnRoomUpdated.AddListener(OnRoomUpdated);
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
            if (isFacilitator)
            {
                // Set the avatar prefab to null to prevent avatar creation for facilitator
                avatarManager.avatarPrefab = null;
                isAvatarHidden = true;
            }
        }
    }
    #endregion

    #region Room Management Methods
    public void CreateRoom()
    {
        if (!roomClient.JoinedRoom)
        {
            appEvents.Log("RoomManager: Creating room", roomName);
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

    private void StartRoomDiscovery()
    {
        lastDiscoveryTime = Time.time;
        appEvents.Log("RoomManager: Starting room discovery");
        roomClient.DiscoverRooms();
    }
    #endregion

    #region Event Handlers
    private void OnRoomsDiscovered(List<IRoom> rooms, RoomsDiscoveredRequest request)
    {
        appEvents.Log($"RoomManager: Discovered {rooms.Count} rooms");
        
        // Look for the facilitator's room
        foreach (var room in rooms)
        {
            if (room.Name == roomName)
            {
                appEvents.Log($"RoomManager: Found room '{room.Name}', attempting to join");
                roomClient.Join(room.JoinCode);
                return;
            }
        }

        appEvents.Log("RoomManager: Room not found, will retry");
    }

    private void OnJoinedRoom(IRoom room)
    {
        appEvents.Log($"RoomManager: Successfully joined room '{room.Name}'");
        if (isFacilitator)
        {
            ConfigureFacilitatorComponents();
        }
    }

    private void OnJoinRejected(Rejection rejection)
    {
        appEvents.Log($"RoomManager: Join rejected - {rejection.reason}");
    }

    private void OnRoomUpdated(IRoom room)
    {
        if (roomClient.Room == room)
        {
            appEvents.Log("RoomManager: Room updated", room.Name);
        }
    }

    private void OnPeerConnectionCreated(VoipPeerConnection connection)
    {
        peerConnections.Add(connection);
        UpdateVoiceSettings();
    }

    private void OnAvatarCreated(Ubiq.Avatars.Avatar avatar)
    {
        if (isFacilitator && avatar.IsLocal)
        {
            // Ensure local avatar stays hidden for facilitator
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
        if (avatarManager != null && isFacilitator)
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
        if (avatarManager != null && isFacilitator)
        {
            if (isAvatarHidden)
            {
                // Show avatar by setting the default prefab
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
            roomClient.OnRooms.RemoveListener(OnRoomsDiscovered);
            roomClient.OnJoinedRoom.RemoveListener(OnJoinedRoom);
            roomClient.OnJoinRejected.RemoveListener(OnJoinRejected);
            roomClient.OnRoomUpdated.RemoveListener(OnRoomUpdated);
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
    [UnityEditor.CustomEditor(typeof(RoomManager))]
    public class RoomManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            RoomManager manager = (RoomManager)target;
            
            UnityEditor.EditorGUILayout.Space();
            if (GUILayout.Button("Create Room"))
            {
                manager.CreateRoom();
            }

            if (manager.isFacilitator)
            {
                UnityEditor.EditorGUILayout.Space();
                if (GUILayout.Button("Toggle Facilitator Avatar"))
                {
                    manager.ToggleFacilitatorAvatar();
                }
            }
        }
    }
    #endif
    #endregion
} 