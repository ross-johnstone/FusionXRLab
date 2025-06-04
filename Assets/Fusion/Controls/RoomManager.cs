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
    [SerializeField] private AudioSource roomAudioSource;
    [SerializeField] private AudioClip[] availableAudioClips;
    #endregion

    #region Private Fields
    private NetworkScene networkScene;
    private RoomClient roomClient;
    private ComponentLogEmitter appEvents;
    private ExperimentLogEmitter experimentLogger;
    private VoipPeerConnectionManager voipManager;
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    private AvatarManager avatarManager;
    private DarknessController darknessController;
    private bool isFacilitator;
    private bool isAvatarHidden = true;
    private int selectedAudioClipIndex = 0;
    private bool isPlaying = false;
    private bool isExperimentRunning = false;
    private float lastPingTime = 0f;
    private float lastDiscoveryTime = 0f;
    private const float PING_INTERVAL = 1f;
    private const float DISCOVERY_INTERVAL = 2f;
    #endregion

    #region Unity Lifecycle Methods
    void Start()
    {
        InitializeComponents();
        DetermineRole();
        SetupEventListeners();
        ConfigureAvatarVisibility();
        SetupAudioSource();
        
        // Get or add DarknessController
        darknessController = GetComponent<DarknessController>();
        if (darknessController == null)
        {
            darknessController = gameObject.AddComponent<DarknessController>();
        }
    }

    void Update()
    {
        if (isFacilitator)
        {
            MaintainConnection();
        }
        else if (!roomClient.JoinedRoom && Time.time - lastDiscoveryTime > DISCOVERY_INTERVAL)
        {
            StartRoomDiscovery();
        }

        // Check if audio has finished playing
        if (isPlaying && roomAudioSource != null && !roomAudioSource.isPlaying)
        {
            isPlaying = false;
            Debug.Log("[RoomManager] Audio clip finished playing");
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
        try
        {
            // Initialize logging
            appEvents = new ComponentLogEmitter(this, Ubiq.Logging.EventType.Application);
            experimentLogger = new ExperimentLogEmitter(this);

            // Get required components
            networkScene = NetworkScene.Find(this);
            if (networkScene == null)
            {
                Debug.LogError("[RoomManager] NetworkScene not found! Make sure there is a NetworkScene in the scene hierarchy.");
                return;
            }

            roomClient = networkScene.GetComponent<RoomClient>();
            if (roomClient == null)
            {
                Debug.LogError("[RoomManager] RoomClient component not found on NetworkScene!");
                return;
            }

            voipManager = networkScene.GetComponent<VoipPeerConnectionManager>();
            avatarManager = AvatarManager.Find(this);

            // Configure RoomClient for connection maintenance
            roomClient.timeoutBehaviour = RoomClient.TimeoutBehaviour.Reconnect;
            Debug.Log("[RoomManager] Components initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RoomManager] Error initializing components: {e.Message}\n{e.StackTrace}");
        }
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
        if (avatarManager != null && isFacilitator)
        {
            avatarManager.avatarPrefab = null;
            isAvatarHidden = true;
        }
    }

    private void SetupAudioSource()
    {
        if (roomAudioSource == null)
        {
            roomAudioSource = gameObject.AddComponent<AudioSource>();
            roomAudioSource.spatialBlend = 1f;
            roomAudioSource.minDistance = 1f;
            roomAudioSource.maxDistance = 20f;
            roomAudioSource.rolloffMode = AudioRolloffMode.Linear;
            roomAudioSource.playOnAwake = false;
            roomAudioSource.loop = false;
        }
    }
    #endregion

    #region Room Management Methods
    public void CreateRoom()
    {
        try
        {
            if (!roomClient.JoinedRoom)
            {
                Debug.Log($"[RoomManager] Creating room: {roomName}");
                roomClient.Join(roomName, true);
                roomClient.Ping();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RoomManager] Error creating room: {e.Message}\n{e.StackTrace}");
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
            avatarManager.avatarPrefab = null;
            isAvatarHidden = true;

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
                appEvents.Log("Facilitator avatar shown");
            }
            else
            {
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

            UnityEditor.EditorGUILayout.Space();

            if (GUILayout.Button(manager.isExperimentRunning ? "Stop Experiment" : "Start Experiment"))
            {   
                manager.isExperimentRunning = !manager.isExperimentRunning;
                if (manager.isExperimentRunning)
                {
                    manager.experimentLogger.Log("Experiment Started");
                }
                else
                {
                    manager.experimentLogger.Log("Experiment Ended");
                }
            }

            UnityEditor.EditorGUILayout.Space();

            if (manager.isFacilitator)
            {
                UnityEditor.EditorGUILayout.Space();
                if (GUILayout.Button("Toggle Facilitator Avatar"))
                {
                    manager.ToggleFacilitatorAvatar();
                }

                UnityEditor.EditorGUILayout.Space();
                if (GUILayout.Button("Toggle Room Environment"))
                {
                    manager.darknessController.ToggleDarkness();
                }

                UnityEditor.EditorGUILayout.Space();
                UnityEditor.EditorGUILayout.LabelField("Audio Controls", UnityEditor.EditorStyles.boldLabel);
                
                if (manager.availableAudioClips != null && manager.availableAudioClips.Length > 0)
                {
                    string[] clipNames = new string[manager.availableAudioClips.Length];
                    for (int i = 0; i < manager.availableAudioClips.Length; i++)
                    {
                        clipNames[i] = manager.availableAudioClips[i] != null ? 
                            manager.availableAudioClips[i].name : "None";
                    }

                    manager.selectedAudioClipIndex = UnityEditor.EditorGUILayout.Popup(
                        "Select Audio Clip", 
                        manager.selectedAudioClipIndex, 
                        clipNames
                    );

                    if (GUILayout.Button(manager.isPlaying ? "Playing..." : "Play Audio"))
                    {
                        manager.PlaySelectedAudio();
                    }
                }
                else
                {
                    UnityEditor.EditorGUILayout.HelpBox("No audio clips assigned. Please add audio clips to the Available Audio Clips array.", UnityEditor.MessageType.Info);
                }
            }
        }
    }
    #endif
    #endregion

    public void PlaySelectedAudio()
    {
        if (roomAudioSource == null)
        {
            SetupAudioSource();
        }

        if (availableAudioClips != null && 
            availableAudioClips.Length > 0 && 
            selectedAudioClipIndex >= 0 && 
            selectedAudioClipIndex < availableAudioClips.Length)
        {
            AudioClip selectedClip = availableAudioClips[selectedAudioClipIndex];
            if (selectedClip != null)
            {
                if (roomAudioSource.isPlaying)
                {
                    roomAudioSource.Stop();
                }

                roomAudioSource.clip = selectedClip;
                roomAudioSource.Play();
                isPlaying = true;
                Debug.Log($"[RoomManager] Playing audio clip: {selectedClip.name}");
            }
        }
    }
} 