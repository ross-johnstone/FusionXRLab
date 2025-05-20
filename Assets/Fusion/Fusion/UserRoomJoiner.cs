using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Networking;
using Ubiq.Messaging;
using System.Collections.Generic;


public class UserRoomJoiner : MonoBehaviour
{
    [SerializeField]
    private string facilitatorRoomName = "Experiment Room";

    private RoomClient roomClient;
    private NetworkScene networkScene;

    void Start()
    {
        // Get required components
        networkScene = NetworkScene.Find(this);
        roomClient = networkScene.GetComponent<RoomClient>();

        // Subscribe to room events
        roomClient.OnRooms.AddListener(OnRoomsDiscovered);
        
        // Start discovering rooms
        roomClient.DiscoverRooms();
    }

    private void OnRoomsDiscovered(List<IRoom> rooms, RoomsDiscoveredRequest request)
    {
        // Look for the facilitator's room
        foreach (var room in rooms)
        {
            if (room.Name == facilitatorRoomName)
            {
                // Found the facilitator's room, join it
                roomClient.Join(room.JoinCode);
                return;
            }
        }

        // If we didn't find the room, try again in a few seconds
        Invoke("RetryJoin", 5f);
    }

    private void RetryJoin()
    {
        roomClient.DiscoverRooms();
    }

    void OnDestroy()
    {
        if (roomClient != null)
        {
            roomClient.OnRooms.RemoveListener(OnRoomsDiscovered);
        }
    }
} 