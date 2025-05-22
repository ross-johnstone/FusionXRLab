using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Networking;
using Ubiq.Messaging;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR;
// using Oculus;
// using Oculus.VR;

public class UserRoomJoiner : MonoBehaviour
{
    [SerializeField]
    private string facilitatorRoomName = "Experiment Room";

    private RoomClient roomClient;
    private NetworkScene networkScene;
    private ExperimentLogEmitter appEvents;
    private float lastDiscoveryTime = 0f;
    private const float DISCOVERY_INTERVAL = 2f;

    void Start()
    {
        // Initialize logging
        appEvents = new ExperimentLogEmitter(this);

        // Get required components
        networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponent<RoomClient>();

        if (roomClient == null)
        {
            Debug.LogError("UserRoomJoiner: RoomClient component not found!");
            return;
        }

        // Subscribe to room events
        roomClient.OnRooms.AddListener(OnRoomsDiscovered);
        roomClient.OnJoinedRoom.AddListener(OnJoinedRoom);
        roomClient.OnJoinRejected.AddListener(OnJoinRejected);
        
        // Start discovering rooms
        StartRoomDiscovery();

        // Disable Oculus system UI
        DisableOculusSystemUI();
    }

    void Update()
    {
        // Periodically try to discover rooms if we haven't joined one
        if (!roomClient.JoinedRoom && Time.time - lastDiscoveryTime > DISCOVERY_INTERVAL)
        {
            StartRoomDiscovery();
        }
    }

    private void DisableOculusSystemUI()
    {
        // Check both hands for presence
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool leftHandPresent = false;
        bool rightHandPresent = false;

        if (leftHand.isValid)
        {
            leftHand.TryGetFeatureValue(CommonUsages.userPresence, out leftHandPresent);
        }

        if (rightHand.isValid)
        {
            rightHand.TryGetFeatureValue(CommonUsages.userPresence, out rightHandPresent);
        }

        // If either hand is present, disable the system UI
        if (leftHandPresent || rightHandPresent)
        {
            // #if UNITY_ANDROID && !UNITY_EDITOR
            // OVRPlugin.SetSystemOverlayPresented(false);
            // #endif
            // appEvents.Log("Oculus system UI disabled");
        }
    }

    private void StartRoomDiscovery()
    {
        lastDiscoveryTime = Time.time;
        appEvents.Log("UserRoomJoiner: Starting room discovery");
        roomClient.DiscoverRooms();
    }

    private void OnRoomsDiscovered(List<IRoom> rooms, RoomsDiscoveredRequest request)
    {
        appEvents.Log($"UserRoomJoiner: Discovered {rooms.Count} rooms");
        
        // Look for the facilitator's room
        foreach (var room in rooms)
        {
            if (room.Name == facilitatorRoomName)
            {
                appEvents.Log($"UserRoomJoiner: Found facilitator room '{room.Name}', attempting to join");
                roomClient.Join(room.JoinCode);
                return;
            }
        }

        appEvents.Log("UserRoomJoiner: Facilitator room not found, will retry");
    }

    private void OnJoinedRoom(IRoom room)
    {
        appEvents.Log($"UserRoomJoiner: Successfully joined room '{room.Name}'");
    }

    private void OnJoinRejected(Rejection rejection)
    {
        appEvents.Log($"UserRoomJoiner: Join rejected - {rejection.reason}");
    }

    void OnDestroy()
    {
        if (roomClient != null)
        {
            roomClient.OnRooms.RemoveListener(OnRoomsDiscovered);
            roomClient.OnJoinedRoom.RemoveListener(OnJoinedRoom);
            roomClient.OnJoinRejected.RemoveListener(OnJoinRejected);
        }
    }
} 