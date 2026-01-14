using UnityEngine;
using System.Collections.Generic;
using Ubiq;
using Ubiq.Logging;
using Ubiq.Avatars;
using Ubiq.Geometry;
using Ubiq.Messaging;
using Avatar = Ubiq.Avatars.Avatar;

public class HeadAndHandsAvatarLoggingController : MonoBehaviour
{

    private AvatarManager avatarManager;
    private GameObject avatars;
    ExperimentLogEmitter experimentLogEmitter;

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

    }


// TODO ask Bernard
    // void OnEnable()
    // {
    //     if(avatarManager) {
    //         avatarManager.OnAvatarCreated.AddListener(OnAvatarCreated);
    //         avatarManager.OnAvatarDestroyed.AddListener(OnAvatarDestroyed);
    //     }
    // }

    private void Track(Avatar avatar)
    {
        var headHandsAvatar = avatar.GetComponentInChildren<HeadAndHandsAvatar>();

        if (!headHandsAvatar)
        {
            Debug.Log($"No HeadAndHandsAvatar found for {avatar.name}");
            return;
        }

        Debug.Log($"Tracking {avatar.name}");

        headHandsAvatar.OnHeadUpdate.AddListener(OnHead);
        headHandsAvatar.OnLeftHandUpdate.AddListener(OnLeftHand);
        headHandsAvatar.OnRightHandUpdate.AddListener(OnRightHand);
    }

    private void OnAvatarDestroyed(Avatar avatar)
    {
        Debug.Log($"Avatar {avatar.name} Destroyed.");
    }

    private void OnAvatarCreated(Avatar avatar)
    {
        Track(avatar);
    }

    private void OnHead(InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("Head", SystemInfo.deviceName, pose.value.position, pose.value.rotation);
    }

    private void OnLeftHand(InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("leftHand", SystemInfo.deviceName, pose.value.position, pose.value.rotation);
    }

    private void OnRightHand(InputVar<Pose> pose)
    {
        if (!pose.valid) return;
        experimentLogEmitter.Log("rightHand", SystemInfo.deviceName, pose.value.position, pose.value.rotation);
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


}