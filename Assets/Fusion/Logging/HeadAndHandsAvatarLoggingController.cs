using UnityEngine;
using System.Collections.Generic;
using System.IO;

using Ubiq;
using Ubiq.Logging;
using Ubiq.Avatars;
using Ubiq.Geometry;
using Ubiq.Messaging;
using Avatar = Ubiq.Avatars.Avatar;


public class HeadAndHandsAvatarLoggingController : MonoBehaviour
{

    public NetworkContext context;
    private Dictionary<NetworkId, (string uniqueId, string deviceId)> peerToDeviceId = new();
    private AvatarManager avatarManager;
    private GameObject avatars;
    ExperimentLogEmitter experimentLogEmitter;
    private LogEmitter componentLogEmitter;

    void Awake()
    {
        // If this is the laptop / host machine then this will act as the LogCollector
        if(SystemInfo.deviceType == DeviceType.Desktop)
        {
            experimentLogEmitter = new ExperimentLogEmitter(this);
            Debug.Log($"Experiment Log Emitter {experimentLogEmitter}");    
        }
    }

    void Start()
    {
        // Register as a networked component to receive device_id from headset
        context  = NetworkScene.Register(this);
        Debug.Log($"[HeadAndHandsAvatarLoggingController] Network: Registered with ID {context.Id}");

        // TODO if is server or host rather than device type
        if(SystemInfo.deviceType == DeviceType.Desktop)
        {
            // Get the AvatarManager to listen to events
            avatarManager = AvatarManager.Find(this);

            if (avatarManager == null)
            {
                Debug.Log("No avatar manager in scene!");
            }

            // Bind the OnAvatarCreated / OnAvatarDestroyed methods to the events emitted from the AvatarManager
            if(avatarManager) {
                avatarManager.OnAvatarCreated.AddListener(OnAvatarCreated);
                avatarManager.OnAvatarDestroyed.AddListener(OnAvatarDestroyed);
            }

            // if there are already registered Avatars, track all the update events
            foreach (var avatar in avatarManager.Avatars)
            {
                Track(avatar);
            }   
        }

        componentLogEmitter = new ComponentLogEmitter(this);

    }

    // Process message from RPMLoader, to receive and map peer_id of avatar to the sent device_id
    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        var data = msg.FromJson<DeviceIdMessage>();
        // Local avatar id mapped to the name of the headset e.g Green
        peerToDeviceId[data.avatarId] = (data.uniqueId, data.id);
        Debug.Log($"[HeadAndHands] objectid = {msg.objectid}, uniqueId = {data.uniqueId}, avatarId = {data.avatarId}, id = {data.id}");
        componentLogEmitter.Log("DeviceId", data.id);
    }


    private void Track(Avatar avatar)
    {
        // Find the head and hands avatar, that is the low level avatar representation that handles the bone movement in the avatar
        Debug.Log($"AvatarID = {avatar.NetworkId}");
        var headHandsAvatar = avatar.GetComponentInChildren<HeadAndHandsAvatar>();

        if (!headHandsAvatar)
        {
            Debug.Log($"No HeadAndHandsAvatar found for {avatar.name}");
            return;
        }

        Debug.Log($"Tracking {avatar.name}");

        // Bind the OnHead, OnLeftHand and OnRightHand methods to the update events in the HeadAndHandsAvatar class
        headHandsAvatar.OnHeadUpdate.AddListener(pose => OnHead(avatar, pose));
        headHandsAvatar.OnLeftHandUpdate.AddListener(pose => OnLeftHand(avatar, pose));
        headHandsAvatar.OnRightHandUpdate.AddListener(pose => OnRightHand(avatar, pose));
    }

    // TODO manage onAvatarDestroyed events. Currently unimplemented, as logs will no longer print this avatar (as no update events will happen)
    // When avatar rejoins scene, the new peer_id will be mapped to the same device_id
    private void OnAvatarDestroyed(Avatar avatar)
    {
        Debug.Log($"Avatar {avatar.name} Destroyed.");
    }

    private void OnAvatarCreated(Avatar avatar)
    {
        Track(avatar);
    }

    private void OnHead(Avatar avatar, InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        // print pose, SystemInfo.deviceUniqueIdentifier and Manual device name 
        // SystemInfo.deviceUniqueIdentifier changes application to application
        experimentLogEmitter.Log("Head", pose.value.position, pose.value.rotation, peerToDeviceId[avatar.NetworkId].uniqueId, peerToDeviceId[avatar.NetworkId].deviceId);
    }

    private void OnLeftHand(Avatar avatar, InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("leftHand", pose.value.position, pose.value.rotation, peerToDeviceId[avatar.NetworkId].uniqueId, peerToDeviceId[avatar.NetworkId].deviceId);
    }

    private void OnRightHand(Avatar avatar, InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("rightHand", pose.value.position, pose.value.rotation, peerToDeviceId[avatar.NetworkId].uniqueId, peerToDeviceId[avatar.NetworkId].deviceId);
    }

    void OnDisable()
    {
        if (avatarManager)
        {
            avatarManager.OnAvatarCreated.RemoveListener(OnAvatarCreated);
            avatarManager.OnAvatarDestroyed.RemoveListener(OnAvatarDestroyed);
        }   
    }


    private void OnDestroy()
    {
        if (avatarManager)
        {
            avatarManager.OnAvatarCreated.RemoveListener(OnAvatarCreated);
            avatarManager.OnAvatarDestroyed.RemoveListener(OnAvatarDestroyed);
        }
    }

    // Used to find controller in RPMLoader to send device id
    public static HeadAndHandsAvatarLoggingController Find()
    {
        var envController = GameObject.Find("Environment Controller");
        if (!envController) return null;
        return envController.GetComponentInChildren<HeadAndHandsAvatarLoggingController>();
    }


}