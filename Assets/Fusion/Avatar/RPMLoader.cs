//Add to avatar prefab RPM Body object if not already present

using System.IO;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Logging;


// Message used to deliver device information for mapping device to avatar in logs
[System.Serializable]
public struct DeviceIdMessage
{
    public NetworkId avatarId;
    public string uniqueId;
    public string id;
}

public class RPMLoader : MonoBehaviour
{

    private string deviceId;
    private string deviceUniqueId;
    private RoomClient roomClient;
    private AvatarManager avatarManager;
    // appended to persistent path which appears as "/sdcard/Android/data/<Package Name>/files/(PREFS/DEVICE)_FILE_NAME"
    private const string PREFS_FILE_NAME = "prefs";
    private const string DEVICE_FILE_NAME = "device";
    private const string PLAYER_PREFS_KEY = "avatars.readyplayerme.url";
    NetworkContext context;
    private LogEmitter componentLogEmitter;

    private HeadAndHandsAvatarLoggingController controller;


    void Awake()
    {
        deviceUniqueId = SystemInfo.deviceUniqueIdentifier;   
    }

    void Start()
    {

        // Create listener for when this headset joins a room. Only runs on headsets
        if(SystemInfo.deviceType != DeviceType.Desktop)
        {
            roomClient = RoomClient.Find(this);
            avatarManager = AvatarManager.Find(this);
            
            if (roomClient == null) 
            {
                Debug.Log("[RPMLoader] No RoomClient found!");
            } else 
            {
                // Call OnJoinedRoom() method when new room is joined on this headset
                roomClient.OnJoinedRoom.AddListener((room) => OnJoinedRoom());
            }

        }

        componentLogEmitter = new ComponentLogEmitter(this);

        // Register for network messages
        context = NetworkScene.Register(this);

        Debug.Log($"[RPMLoader] Network: Registered with ID {context.Id}");

        var avatar = GetComponentInParent<Ubiq.Avatars.Avatar>();
        if (avatar == null || !avatar.IsLocal)
        {
            return; // Don't load prefs or set URLs for remote peers
        }

        Debug.Log("[RPMLoader] Start called. Path: " + Application.persistentDataPath);

        string path = Path.Combine(Application.persistentDataPath, PREFS_FILE_NAME);

        if (File.Exists(path))
        {
            string fileContent = File.ReadAllText(path).Trim();
            Debug.Log($"[RPMLoader] Read from file: {fileContent}");

            if (fileContent.StartsWith("avatar_url="))
            {
                string url = fileContent.Substring("avatar_url=".Length).Trim();

                // Save to PlayerPrefs for future use  
                PlayerPrefs.SetString(PLAYER_PREFS_KEY, url);
                PlayerPrefs.Save();

                Debug.Log($"[RPMLoader] Parsed avatar URL: {url}");

                // Try to find the avatar sync component on this object or elsewhere  
                var avatarSync = GetComponent<ReadyPlayerMeSyncUrlAvatar>();
                if (avatarSync == null)
                {
                    avatarSync = Object.FindFirstObjectByType<ReadyPlayerMeSyncUrlAvatar>();
                }

                if (avatarSync != null)
                {
                    avatarSync.SetUrl(url);
                    Debug.Log("[RPMLoader] Avatar URL set on ReadyPlayerMeSyncUrlAvatar.");
                }
                else
                {
                    Debug.LogWarning("[RPMLoader] ReadyPlayerMeSyncUrlAvatar component not found in scene.");
                }
            }
            else
            {
                Debug.LogWarning("[RPMLoader] File content does not start with 'avatar_url='.");
            }
        }
        else
        {
            Debug.LogWarning($"[RPMLoader] File not found at {path}");
        }

    }

    // When new room is joined on this headset, get this device name (set via adb manually)
    //      e.g adb shell "echo 'device_id=Green' > /sdcard/Android/data/com.Fusion.XRLab/files/device"
    private void OnJoinedRoom()
    {
        deviceId = GetDeviceId();
        SendDeviceId(deviceId);
        componentLogEmitter.Log(deviceId);
    }

    string GetDeviceId()
    {
        // Get device id from device file

        Debug.Log("[HeadAndHandsAvatarLoggingController] Start called. Path: " + Application.persistentDataPath);

        string devicePath = Path.Combine(Application.persistentDataPath, DEVICE_FILE_NAME);

        if (File.Exists(devicePath))
        {
            string deviceFileContent = File.ReadAllText(devicePath).Trim();
            Debug.Log($"[HeadAndHandsAvatarLoggingController] Read from file: {deviceFileContent}");

            if (File.Exists(devicePath))
            {
                // Appears in device file as "device_id=<name>"
                string id = deviceFileContent.Substring("device_id=".Length).Trim();

                // Currently unused
                PlayerPrefs.SetString("device_id", id);

                return id;
            }

        }
        return "";
    }

    void SendDeviceId(string deviceId)
    {
        // Find the logging component
        controller = HeadAndHandsAvatarLoggingController.Find();
        // Get the Id of the avatar on the network - used for mapping the peer_id to device_id
        NetworkId localAvatarId = avatarManager.LocalAvatar.NetworkId;

        Debug.Log($"[RPMLoader] Sending to server {controller.context.Id}, {localAvatarId}, {deviceId}");

        // NetworkContext doesn't allow specific messaging to components, so must be sent through NetworkScene
        context.Scene.SendJson(controller.context.Id, new DeviceIdMessage 
        {
            avatarId = localAvatarId,
            uniqueId = deviceUniqueId,
            id = deviceId
        });
        Debug.Log($"[RPMLoader] Sent device_id to server: {deviceId}");
    }

    // Must be implemented, currently not receiving any messages
    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {

    } 
}
