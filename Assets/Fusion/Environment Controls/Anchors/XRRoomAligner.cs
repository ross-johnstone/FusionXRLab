using UnityEngine;

public class XRRoomAligner : MonoBehaviour
{
    // [Header("Assign the two anchor points (local to XR Rig)")]
    public Transform anchor1; // p1
    public Transform anchor2; // p2

    // [Header("Assign the XR Rig root (the object whose parent will be moved)")]
    public Transform xrRig; // The XR Rig root

    [ContextMenu("Align XR Rig")]
    public void AlignXRRig(Transform anchor1, Transform anchor2)
    {
        if (anchor1 == null || anchor2 == null || xrRig == null)
        {
            Debug.LogError("Please assign anchor1, anchor2, and xrRig.");
            return;
        }

        Vector3 p1 = anchor1.position;
        Vector3 p2 = anchor2.position;

        // Build the rotation so that p1->p2 is forward, and up is Y+
        Quaternion rotation = Quaternion.LookRotation(p2 - p1, Vector3.up);

        // Build the transformation matrix
        Matrix4x4 m =
            Matrix4x4.Rotate(Quaternion.Inverse(rotation)) *
            Matrix4x4.Translate(-p1);

        // Apply to the XR Rig's parent
        if (xrRig.parent != null)
        {
            Vector3 newPosition = m.GetPosition();
            newPosition.y = xrRig.parent.position.y; // Keep the Y position of the parent
            //xrRig.parent.SetPositionAndRotation(newPosition, m.rotation);
            xrRig.parent.position = newPosition;
        }
        else
        {
            Debug.LogWarning("xrRig has no parent to move.");
        }
    }
}
