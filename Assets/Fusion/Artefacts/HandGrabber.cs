using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;


public class HandGrabber : MonoBehaviour
{
    public Transform handTransform;
    public XRNode handNode = XRNode.RightHand;
    public string grabTag = "Grabbable";

    private GameObject heldObject = null;
    private Vector3 grabOffset;
    private Quaternion grabRotationOffset;

    XRHandSubsystem handSubsystem;

    private ExperimentLogEmitter logEmitter;

    void Start()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];

        logEmitter = new ExperimentLogEmitter(this);
    }

    void Update()
    {
        if (handSubsystem == null || !IsGrabbing())
        {
            if (heldObject != null)
                Release();
            return;
        }

        if (heldObject == null)
            TryGrab();

        if (heldObject)
        {
            logEmitter.Log("Holding object: " + heldObject.name, heldObject.transform.position, heldObject.transform.rotation);
            heldObject.transform.position = handTransform.position + handTransform.rotation * grabOffset;
            heldObject.transform.rotation = handTransform.rotation * grabRotationOffset;
        }
    }

    bool IsGrabbing()
    {
        if (handSubsystem == null)
            return false;

        XRHand hand = handNode == XRNode.LeftHand ? handSubsystem.leftHand : handSubsystem.rightHand;

        if (hand == null || !hand.isTracked)
            return false;

        var thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
        var indexTip = hand.GetJoint(XRHandJointID.IndexTip);

        if (!thumbTip.TryGetPose(out Pose thumbPose) || !indexTip.TryGetPose(out Pose indexPose))
            return false;

        float pinchDistance = Vector3.Distance(thumbPose.position, indexPose.position);
        return pinchDistance < 0.03f; // You can adjust this threshold
    }

    void TryGrab()
    {
        Collider[] hits = Physics.OverlapSphere(handTransform.position, 0.05f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag(grabTag))
            {
                heldObject = hit.gameObject;
                grabOffset = Quaternion.Inverse(handTransform.rotation) * (heldObject.transform.position - handTransform.position);
                grabRotationOffset = Quaternion.Inverse(handTransform.rotation) * heldObject.transform.rotation;
                heldObject.GetComponent<Rigidbody>().isKinematic = true;
                break;
            }
        }
    }

    void Release()
    {
        if (heldObject != null)
        {
            heldObject.GetComponent<Rigidbody>().isKinematic = false;
            heldObject = null;
        }
    }
}