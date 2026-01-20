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
        if(SystemInfo.deviceType == DeviceType.Desktop)
        {
            experimentLogEmitter = new ExperimentLogEmitter(this);
            Debug.Log($"Experiment Log Emitter {experimentLogEmitter}");    
        }
    }

    void Start()
    {

        context  = NetworkScene.Register(this);
        Debug.Log($"[HeadAndHandsAvatarLoggingController] Network: Registered with ID {context.Id}");

        // if is server or host rather than device type
        if(SystemInfo.deviceType == DeviceType.Desktop)
        {
            avatarManager = AvatarManager.Find(this);

            if (avatarManager == null)
            {
                Debug.Log("No avatar manager in scene!");
            }

            if(avatarManager) {
                avatarManager.OnAvatarCreated.AddListener(OnAvatarCreated);
                avatarManager.OnAvatarDestroyed.AddListener(OnAvatarDestroyed);
            }

            foreach (var avatar in avatarManager.Avatars)
            {
                Track(avatar);
            }   
        }

        componentLogEmitter = new ComponentLogEmitter(this);

    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
    {
        var data = msg.FromJson<DeviceIdMessage>();
        peerToDeviceId[data.avatarId] = (data.uniqueId, data.id);
        Debug.Log($"[HeadAndHands] {msg.objectid}, {data.uniqueId}, {data.avatarId}, {data.id}");
        componentLogEmitter.Log("DeviceId", data.id);
    }


    private void Track(Avatar avatar)
    {

        Debug.Log($"AvatarID = {avatar.NetworkId}");
        var headHandsAvatar = avatar.GetComponentInChildren<HeadAndHandsAvatar>();

        if (!headHandsAvatar)
        {
            Debug.Log($"No HeadAndHandsAvatar found for {avatar.name}");
            return;
        }

        Debug.Log($"Tracking {avatar.name}");

        headHandsAvatar.OnHeadUpdate.AddListener(pose => OnHead(avatar, pose));
        headHandsAvatar.OnLeftHandUpdate.AddListener(pose => OnLeftHand(avatar, pose));
        headHandsAvatar.OnRightHandUpdate.AddListener(pose => OnRightHand(avatar, pose));
    }

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
        experimentLogEmitter.Log("Head", SystemInfo.deviceName, pose.value.position, pose.value.rotation, peerToDeviceId[avatar.NetworkId].uniqueId, peerToDeviceId[avatar.NetworkId].deviceId);
    }

    private void OnLeftHand(Avatar avatar, InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("leftHand", SystemInfo.deviceName, pose.value.position, pose.value.rotation, peerToDeviceId[avatar.NetworkId].uniqueId, peerToDeviceId[avatar.NetworkId].deviceId);
    }

    private void OnRightHand(Avatar avatar, InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("rightHand", SystemInfo.deviceName, pose.value.position, pose.value.rotation, peerToDeviceId[avatar.NetworkId].uniqueId, peerToDeviceId[avatar.NetworkId].deviceId);
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

    public static HeadAndHandsAvatarLoggingController Find()
    {
        var envController = GameObject.Find("Environment Controller");
        if (!envController) return null;
        return envController.GetComponentInChildren<HeadAndHandsAvatarLoggingController>();
    }


}