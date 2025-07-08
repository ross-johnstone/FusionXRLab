using UnityEngine;
using Ubiq;
// using Ubiq.Avatars; // Remove this to avoid ambiguity

public class AvatarLogger : MonoBehaviour
{
    private Ubiq.Avatars.AvatarManager avatarManager;

    void Start()
    {
        // avatarManager = GetComponent<AvatarManager>();
        avatarManager = Ubiq.Avatars.AvatarManager.Find(this);
        avatarManager.OnAvatarCreated.AddListener(OnAvatarCreated);
    }

    private void OnAvatarCreated(Ubiq.Avatars.Avatar avatar)
    {
        var headAndHands = avatar.GetComponentInChildren<Ubiq.HeadAndHandsAvatar>();
        if (headAndHands != null)
        {
            headAndHands.OnHeadUpdate.AddListener((input) => LogPose(avatar, "Head", input));
            headAndHands.OnLeftHandUpdate.AddListener((input) => LogPose(avatar, "LeftHand", input));
            headAndHands.OnRightHandUpdate.AddListener((input) => LogPose(avatar, "RightHand", input));
            headAndHands.OnLeftGripUpdate.AddListener((input) => LogGrip(avatar, "LeftGrip", input));
            headAndHands.OnRightGripUpdate.AddListener((input) => LogGrip(avatar, "RightGrip", input));
        }
        else
        {
            Debug.LogWarning($"No HeadAndHandsAvatar found on avatar: {avatar.gameObject.name}");
        }
    }

    private void LogPose(Ubiq.Avatars.Avatar avatar, string label, InputVar<Pose> input)
    {
        if (input.valid)
        {
            Debug.Log($"[{avatar.Peer?.uuid}] {label} | Pos: {input.value.position} | Rot (euler): {input.value.rotation.eulerAngles} | Rot (quat): {input.value.rotation}");
        }
        else
        {
            Debug.Log($"[{avatar.Peer?.uuid}] {label} | No valid pose data");
        }
    }

    private void LogGrip(Ubiq.Avatars.Avatar avatar, string label, InputVar<float> input)
    {
        if (input.valid)
        {
            Debug.Log($"[{avatar.Peer?.uuid}] {label} | Value: {input.value}");
        }
        else
        {
            Debug.Log($"[{avatar.Peer?.uuid}] {label} | No valid grip data");
        }
    }
}
