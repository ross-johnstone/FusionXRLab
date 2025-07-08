using UnityEngine;
using Ubiq.Avatars;

public class AvatarLogger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    AvatarManager avatarManager;
    void Start()
    {
        avatarManager = GetComponent<AvatarManager>();
        avatarManager.OnAvatarCreated.AddListener(LogAvatarJoints);
    }

    // Update is called once per frame
    void Update()
    {
                
    }

    private void LogAvatarJoints(Ubiq.Avatars.Avatar avatar)
    {
        // Try common paths for head and hands
        var head = avatar.transform.Find("Armature/Hips/Spine/Neck/Head") ??
                   avatar.transform.Find("Armature/Hips/Spine/Spine1/Spine2/Neck/Head");
        var leftHand = avatar.transform.Find("Armature/Hips/Spine/LeftHand") ??
                       avatar.transform.Find("Armature/Hips/Spine/Spine1/Spine2/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
        var rightHand = avatar.transform.Find("Armature/Hips/Spine/RightHand") ??
                        avatar.transform.Find("Armature/Hips/Spine/Spine1/Spine2/RightShoulder/RightArm/RightForeArm/RightHand");

        LogTransform("Head", head);
        LogTransform("LeftHand", leftHand, true);
        LogTransform("RightHand", rightHand, true);
    }

    private void LogTransform(string label, Transform t, bool logChildren = false)
    {
        if (t == null)
        {
            Debug.Log($"{label}: Not found");
            return;
        }
        Debug.Log($"{label}: {t.gameObject.name} | Pos: {t.position} | Rot: {t.rotation.eulerAngles}");
        if (logChildren)
        {
            foreach (Transform child in t)
            {
                Debug.Log($"  Child: {child.gameObject.name} | Pos: {child.position} | Rot: {child.rotation.eulerAngles}");
            }
        }
    }
}
