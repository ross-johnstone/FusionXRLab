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
    [SerializeField] private Light[] roomLights; // Array of lights to control
    [SerializeField] private GameObject[] roomLines; // Array of lines to control
    [SerializeField] private AudioSource roomAudioSource; // Audio source for playing sounds
    [SerializeField] private AudioClip[] availableAudioClips; // Array of available audio clips
    #endregion

    #region Private Fields
    private NetworkScene networkScene;
    private RoomClient roomClient;
    private ComponentLogEmitter appEvents;
    private ExperimentLogEmitter experimentLogger;
    private VoipPeerConnectionManager voipManager;
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    private float lastPingTime = 0f;
    private const float PING_INTERVAL = 1f;
    private AvatarManager avatarManager;
    private bool isAvatarHidden = true;
    private float lastDiscoveryTime = 0f;
    private const float DISCOVERY_INTERVAL = 2f;
    private bool isFacilitator;
    private bool lightsEnabled = true;
    private int selectedAudioClipIndex = 0;
    private bool isPlaying = false;
    private bool isExperimentRunning = false;
    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();
    #endregion

    #region Unity Lifecycle Methods
    void Start()
    {
        InitializeComponents();
        DetermineRole();
        SetupEventListeners();
        ConfigureAvatarVisibility();
        SetupAudioSource();
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
        // Initialize logging
        appEvents = new ComponentLogEmitter(this, Ubiq.Logging.EventType.Application);
        experimentLogger = new ExperimentLogEmitter(this);

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

    private void SetupAudioSource()
    {
        if (roomAudioSource == null)
        {
            roomAudioSource = gameObject.AddComponent<AudioSource>();
            roomAudioSource.spatialBlend = 1f; // Make it 3D audio
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
                    manager.ToggleDarkness();
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

    private void ScaleMaterialColor(GameObject obj, float scaleFactor)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null && renderer.materials != null)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat != null)
                {
                    // Store original color if we haven't seen this material before
                    if (!originalColors.ContainsKey(mat))
                    {
                        Color baseColor = mat.GetColor("_BaseColor");
                        originalColors[mat] = baseColor;
                    }

                    // Get the color to apply (either scaled down or original)
                    Color colorToApply = scaleFactor < 1f ? 
                        originalColors[mat] * scaleFactor : // Scale down
                        originalColors[mat]; // Restore original

                    mat.SetColor("_BaseColor", colorToApply);
                    mat.SetColor("_Color", colorToApply);
                }
            }
        }
    }

    #region Environment Control Methods
    private void ToggleLights()
    {
        if (roomLights == null || roomLights.Length == 0)
        {
            roomLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        }

        foreach (var light in roomLights)
        {
            if (light != null)
            {
                light.enabled = lightsEnabled;
            }
        }
    }

    private void HandleEmissiveMaterial(GameObject obj, string materialName, Color enabledColor, Color disabledColor)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (mat != null && mat.name.Contains(materialName))
                {
                    Color emissiveColor = lightsEnabled ? enabledColor : disabledColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissiveColor);
                    break;
                }
            }
        }
    }

    private void HandleSpecificMaterial(GameObject obj, string materialName, Color enabledColor, Color disabledColor)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (mat != null && mat.name.Contains(materialName))
                {
                    Color color = lightsEnabled ? enabledColor : disabledColor;
                    mat.SetColor("_BaseColor", color);
                    mat.SetColor("_Color", color);
                    break;
                }
            }
        }
    }

    private void ProcessChildObjects(GameObject parent, System.Action<GameObject> action)
    {
        if (parent != null)
        {
            foreach (Transform child in parent.transform)
            {
                action(child.gameObject);
            }
        }
    }

    public void ToggleDarkness()
    {
        lightsEnabled = !lightsEnabled;

        // Toggle all lights in the scene
        ToggleLights();

        // Process XRLabRoomC Walls & Lights children
        GameObject xrLabRoom = GameObject.Find("XRLabRoomC Walls & Lights");
        ProcessChildObjects(xrLabRoom, obj => ScaleMaterialColor(obj, lightsEnabled ? 1f : 0.2f));

        // Process wall lines
        GameObject linesParent = GameObject.Find("Lines");
        if (linesParent != null)
        {
            roomLines = new GameObject[linesParent.transform.childCount];
            for (int i = 0; i < linesParent.transform.childCount; i++)
            {
                roomLines[i] = linesParent.transform.GetChild(i).gameObject;
            }

            Color targetColor = lightsEnabled ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            foreach (GameObject line in roomLines)
            {
                if (line != null)
                {
                    foreach (Renderer renderer in line.GetComponentsInChildren<Renderer>())
                    {
                        if (renderer != null)
                        {
                            renderer.material.color = targetColor;
                        }
                    }
                }
            }
            Debug.Log($"[RoomManager] Changed color of {roomLines.Length} line objects to {(lightsEnabled ? "white" : "dark grey")}");
        }

        // Handle specific objects with their materials
        GameObject emissiveLight = GameObject.Find("SM_XR_Lights_01_0");
        HandleEmissiveMaterial(emissiveLight, "M_EmissiveLight", 
            new Color(1f, 167f/255f, 28f/255f), Color.black);

        GameObject floor = GameObject.Find("SM_XR_Floor_01_44");
        HandleSpecificMaterial(floor, "M_Floor",
            new Color(188f/255f, 190f/255f, 190f/255f), new Color(0.18f, 0.2f, 0.2f));

        GameObject wall = GameObject.Find("SM_XR_Walls_01_52");
        HandleSpecificMaterial(wall, "M_Wall",
            new Color(255f/255f, 254f/255f, 242f/255f), new Color(0.25f, 0.254f, 0.242f));

        // Process Hololens objects
        GameObject cleanBoxsParent = GameObject.Find("CleanBoxs");
        if (cleanBoxsParent != null)
        {
            var hololensObjects = new List<GameObject>();
            foreach (Transform child in cleanBoxsParent.GetComponentsInChildren<Transform>())
            {
                if (child.gameObject.name == "Hololens")
                {
                    hololensObjects.Add(child.gameObject);
                }
            }

            foreach (GameObject hololens in hololensObjects)
            {
                HandleEmissiveMaterial(hololens, "M_General_ORM",
                    Color.white, new Color(0.2f, 0.2f, 0.2f));
            }
        }

        // Process Switches
        GameObject switchesParent = GameObject.Find("Switches");
        ProcessChildObjects(switchesParent, obj => ScaleMaterialColor(obj, lightsEnabled ? 1f : 0.2f));

        Debug.Log($"[RoomManager] Room environment {(lightsEnabled ? "enabled" : "disabled")}");
    }
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
                // Stop any currently playing audio
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