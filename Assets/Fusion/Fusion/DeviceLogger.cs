using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

public class DeviceLogger : MonoBehaviour
{
    private XRHandSubsystem handSubsystem;
    private ComponentLogEmitter appEvents; // For application events
    private ExperimentLogEmitter headEvents; // For head tracking
    private ExperimentLogEmitter handEvents; // For hand tracking
    private ExperimentLogEmitter gazeEvents; // For gaze tracking
    private ExperimentLogEmitter controllerEvents; // For controller input
    private Camera mainCamera; // For gaze tracking
    private InputDevice headset; // For XR headset tracking

    void Start()
    {
        try
        {
            // Initialize all loggers
            appEvents = new ComponentLogEmitter(this, Ubiq.Logging.EventType.Application);
            headEvents = new ExperimentLogEmitter(this);
            handEvents = new ExperimentLogEmitter(this);
            gazeEvents = new ExperimentLogEmitter(this);
            controllerEvents = new ExperimentLogEmitter(this);

            Debug.Log("XRTracker is starting...");
            mainCamera = Camera.main;

            // Initialize headset tracking
            var inputDevices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);
            if (inputDevices.Count > 0)
            {
                headset = inputDevices[0];
                appEvents.Log("Headset tracking initialized.");
            }
            else
            {
                appEvents.Log("No headset found. Gaze tracking will be limited.");
            }

            // Initialize hand tracking subsystem
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            if (subsystems.Count > 0)
            {
                handSubsystem = subsystems[0];
                if (handSubsystem != null)
                {
                    handSubsystem.Start();
                    appEvents.Log("Hand tracking subsystem started.");
                }
                else
                {
                    Debug.LogWarning("Hand subsystem is null after retrieval");
                }
            }
            else
            {
                appEvents.Log("No XRHandSubsystem found. Hand tracking will not be available.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error initializing DeviceLogger: {e.Message}\n{e.StackTrace}");
        }
    }

    void Update()
    {
        // Enhanced gaze tracking
        if (headset.isValid)
        {
            // Get headset position and rotation
            if (headset.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                // Get the forward direction from the rotation
                Vector3 gazeDirection = rotation * Vector3.forward;
                
                // Log detailed gaze information
                gazeEvents.Log("GazeData", 
                    position, // Headset position
                    rotation, // Headset rotation
                    gazeDirection, // Gaze direction vector
                    Time.time // Timestamp
                );

                // Optional: Cast a ray to see what the user is looking at
                RaycastHit hit;
                if (Physics.Raycast(position, gazeDirection, out hit))
                {
                    gazeEvents.Log("GazeTarget", 
                        hit.point, // Where the gaze ray hits
                        hit.distance, // How far the user is looking
                        hit.collider.gameObject.name // What they're looking at
                    );
                }
            }
        }
        else if (mainCamera != null)
        {
            // Fallback to camera-based tracking if headset is not available
            Vector3 gazePosition = mainCamera.transform.position;
            Vector3 gazeDirection = mainCamera.transform.forward;
            gazeEvents.Log("GazeData", 
                gazePosition,
                mainCamera.transform.rotation,
                gazeDirection,
                Time.time
            );
        }

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
                    headEvents.Log("HeadData", headPosition, headRotation, Time.time);
                }
            }
        }

        // Track controllers with null checks
        if (controllerEvents != null)
        {
            TrackController(XRNode.LeftHand, "Left Hand");
            TrackController(XRNode.RightHand, "Right Hand");
        }

        // Only capture controller inputs if we have valid events
        if (controllerEvents != null)
        {
            CaptureControllerInputs();
        }

        // Only log hand data if we have valid subsystem and events
        if (handSubsystem != null && handEvents != null)
        {
            LogHandData(handSubsystem.leftHand, "Left");
            LogHandData(handSubsystem.rightHand, "Right");
        }
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
                    controllerEvents.Log($"{name}Data", position, rotation, Time.time);
                }
            }
        }
    }

    void CaptureControllerInputs()
    {
        CaptureInputsForDevice(InputDevices.GetDeviceAtXRNode(XRNode.LeftHand), "Left");
        CaptureInputsForDevice(InputDevices.GetDeviceAtXRNode(XRNode.RightHand), "Right");
    }

    void CaptureInputsForDevice(InputDevice device, string hand)
    {
        if (!device.isValid)
        {
            appEvents.Log($"{hand} Controller not found or not valid.");
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
                controllerEvents.Log($"{hand}Button", feature.name, boolValue, Time.time);
            }
            else if (feature.type == typeof(float) && device.TryGetFeatureValue(feature.As<float>(), out float floatValue))
            {
                controllerEvents.Log($"{hand}Axis", feature.name, floatValue, Time.time);
            }
            else if (feature.type == typeof(Vector2) && device.TryGetFeatureValue(feature.As<Vector2>(), out Vector2 vec2Value))
            {
                controllerEvents.Log($"{hand}Vector2", feature.name, vec2Value, Time.time);
            }
            else if (feature.type == typeof(Vector3) && device.TryGetFeatureValue(feature.As<Vector3>(), out Vector3 vec3Value))
            {
                controllerEvents.Log($"{hand}Vector3", feature.name, vec3Value, Time.time);
            }
            else if (feature.type == typeof(Quaternion) && device.TryGetFeatureValue(feature.As<Quaternion>(), out Quaternion quatValue))
            {
                controllerEvents.Log($"{hand}Rotation", feature.name, quatValue, Time.time);
            }
        }
    }

    void LogHandData(XRHand hand, string label)
    {
        if (!hand.isTracked)
        {
            appEvents.Log($"{label} Hand is NOT tracked.");
            return;
        }

        appEvents.Log($"{label} Hand is tracked.");

        foreach (XRHandJointID jointId in System.Enum.GetValues(typeof(XRHandJointID)))
        {
            // Skip EndMarker and any negative/invalid values
            if (jointId <= XRHandJointID.Invalid || jointId >= XRHandJointID.EndMarker)
                continue;

            XRHandJoint joint = hand.GetJoint(jointId);
            if (joint.TryGetPose(out Pose pose))
            {
                handEvents.Log($"{label}Joint", jointId, pose.position, pose.rotation, Time.time);
            }
        }
    }
}
