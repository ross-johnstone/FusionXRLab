using UnityEngine;

public class AvatarAligner : MonoBehaviour
{
    // Call this to align the XR Rig to the anchor
    // xrRig: the XR Rig GameObject to move
    // anchor: the anchor Transform to align to
    // userHead: the Transform of the user's head (e.g., camera)
    public void alignAvatarToAnchor(GameObject xrRig, Transform anchor, Transform userHead)
    {
        if (xrRig == null || anchor == null || userHead == null)
        {
            Debug.LogWarning("AvatarAligner: Missing reference for alignment.");
            return;
        }

        // Calculate the offset from the anchor to the user's head
        Vector3 offset = userHead.position - anchor.position;

        // Move the XR Rig so its origin is at the same offset from the anchor
        xrRig.transform.position = anchor.position + offset;
    }
} 