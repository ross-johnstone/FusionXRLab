using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class DeviceLogger : MonoBehaviour
{

    private XRHandSubsystem handSubsystem;


    ExperimentLogEmitter events;

    void Start()
    {
        events = new ExperimentLogEmitter(this);
        Debug.Log("XRTracker is starting...");

        // Initialize hand tracking subsystem
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
            handSubsystem.Start();
            Debug.Log("Hand tracking subsystem started.");
        }
        else
        {
            Debug.LogWarning("No XRHandSubsystem found. Hand tracking will not be available.");
        }
    }

    void Update()
    {

        // Get head position and rotation
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.Head)
            {
                Vector3 headPosition;
                Quaternion headRotation;
                if (nodeState.TryGetPosition(out headPosition) && nodeState.TryGetRotation(out headRotation))
                {
                    events.Log($"Head Position: {headPosition}, Rotation: {headRotation}");
            
                }
            }
    
            // Get left and right hand/controller positions
            TrackController(XRNode.LeftHand, "Left Hand");
            TrackController(XRNode.RightHand, "Right Hand");
        }

        CaptureControllerInputs();

        LogHandData(handSubsystem.leftHand, "Left");
        LogHandData(handSubsystem.rightHand, "Right");



    }

    private void TrackController(XRNode node, string name)
    {
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == node)
            {
                Vector3 position;
                Quaternion rotation;
                if (nodeState.TryGetPosition(out position) && nodeState.TryGetRotation(out rotation))
                {
                    events.Log($"{name} Position: {position}, Rotation: {rotation}");
                }
            }
        }
    }

    //void CaptureControllerInputs()
    //{
    //    InputDevice leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    //    InputDevice rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

    //    // Track left primary button press
    //    if (leftController.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonPressed) && primaryButtonPressed)
    //    {
    //        events.Log("Left Controller Primary Button Pressed");
    //    }

    //    // Track right trigger value
    //    if (rightController.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue))
    //    {
    //        events.Log($"Right Trigger Value: {triggerValue}");
    //    }

    //    // Track right joystick movement
    //    if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickValue))
    //    {
    //        events.Log($"Right Joystick: {joystickValue}");
    //    }
    //}

    void CaptureControllerInputs()
    {
        CaptureInputsForDevice(InputDevices.GetDeviceAtXRNode(XRNode.LeftHand), "Left");
        CaptureInputsForDevice(InputDevices.GetDeviceAtXRNode(XRNode.RightHand), "Right");
    }

    void CaptureInputsForDevice(InputDevice device, string hand)
    {
        if (!device.isValid)
        {
            events.Log($"{hand} Controller not found or not valid.");
            return;
        }

        // Query all available features
        List<InputFeatureUsage> features = new List<InputFeatureUsage>();
        device.TryGetFeatureUsages(features);

        foreach (var feature in features)
        {
            // Handle common types: bool, float, Vector2, Vector3, Quaternion
            if (feature.type == typeof(bool) && device.TryGetFeatureValue(feature.As<bool>(), out bool boolValue))
            {
                events.Log($"{hand} {feature.name}: {boolValue}");
            }
            else if (feature.type == typeof(float) && device.TryGetFeatureValue(feature.As<float>(), out float floatValue))
            {
                events.Log($"{hand} {feature.name}: {floatValue}");
            }
            else if (feature.type == typeof(Vector2) && device.TryGetFeatureValue(feature.As<Vector2>(), out Vector2 vec2Value))
            {
                events.Log($"{hand} {feature.name}: {vec2Value}");
            }
            else if (feature.type == typeof(Vector3) && device.TryGetFeatureValue(feature.As<Vector3>(), out Vector3 vec3Value))
            {
                events.Log($"{hand} {feature.name}: {vec3Value}");
            }
            else if (feature.type == typeof(Quaternion) && device.TryGetFeatureValue(feature.As<Quaternion>(), out Quaternion quatValue))
            {
                events.Log($"{hand} {feature.name}: {quatValue}");
            }
            else
            {
                // Skip unsupported types to avoid spam
                // events.Log($"{hand} {feature.name}: [Unsupported type: {feature.type}]");
            }
        }
    }

    //void LogHandData(XRHandSubsystem.Hands hand)
    //{
    //    if (handSubsystem == null || !handSubsystem.running)
    //        return;

    //    XRHand xrHand = (hand == XRHandSubsystem.Hands.Left) ? handSubsystem.leftHand : handSubsystem.rightHand;

    //    if (xrHand.isTracked)
    //    {
    //        events.Log($"{hand} Hand is tracked.");

    //        foreach (XRHandJointID jointId in System.Enum.GetValues(typeof(XRHandJointID)))
    //        {
    //            if (xrHand.TryGetJoint(jointId, out XRHandJoint joint) && joint.TryGetPose(out Pose pose))
    //            {
    //                events.Log($"{hand} {jointId}: Position {pose.position}, Rotation {pose.rotation}");
    //            }
    //        }
    //    }
    //    else
    //    {
    //        events.Log($"{hand} Hand is NOT tracked.");
    //    }
    //}

    void LogHandData(XRHand hand, string label)
    {
        if (!hand.isTracked)
        {
            events.Log($"{label} Hand is NOT tracked.");
            return;
        }

        events.Log($"{label} Hand is tracked.");

        foreach (XRHandJointID jointId in System.Enum.GetValues(typeof(XRHandJointID)))
        {
            // Skip EndMarker and any negative/invalid values
            if (jointId <= XRHandJointID.Invalid || jointId >= XRHandJointID.EndMarker)
                continue;

            XRHandJoint joint = hand.GetJoint(jointId);
            if (joint.TryGetPose(out Pose pose))
            {
                events.Log($"{label} {jointId}: Position {pose.position}, Rotation {pose.rotation}");
            }
        }

    }



}
