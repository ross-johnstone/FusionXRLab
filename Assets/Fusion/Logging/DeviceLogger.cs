using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using System.Net.NetworkInformation;
using System.Linq;

public class DeviceLogger : MonoBehaviour
{
    private XRHandSubsystem handSubsystem;
    private ComponentLogEmitter appEvents; // For application events
    private ExperimentLogEmitter headEvents; // For head tracking
    private ExperimentLogEmitter handEvents; // For hand tracking
    private ExperimentLogEmitter gazeEvents; // For gaze tracking
    private Camera mainCamera; // For gaze tracking
    private InputDevice headset; // For XR headset tracking

    [Header("Logging Settings")]
    [SerializeField] private float gazeLogInterval = 0.1f; // Log gaze data every 0.1 seconds
    private float lastGazeLogTime = 0f;
    private Vector3 lastGazePosition;
    private Quaternion lastGazeRotation;
    private float gazePositionThreshold = 0.01f; // Minimum position change to log
    private float gazeRotationThreshold = 0.1f; // Minimum rotation change to log

    // Track hand states
    private bool leftHandWasTracked = false;
    private bool rightHandWasTracked = false;

    private string GetMACAddress()
    {
        string macAddress = "Unknown";
        try
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                if (adapter.OperationalStatus == OperationalStatus.Up)
                {
                    macAddress = adapter.GetPhysicalAddress().ToString();
                    if (!string.IsNullOrEmpty(macAddress))
                    {
                        // Format MAC address with colons
                        macAddress = string.Join(":", Enumerable.Range(0, 6)
                            .Select(i => macAddress.Substring(i * 2, 2)));
                        break;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Error getting MAC address: {e.Message}");
        }
        return macAddress;
    }

    void Start()
    {
        try
        {
            // Initialize all loggers
            appEvents = new ComponentLogEmitter(this, Ubiq.Logging.EventType.Application);
            headEvents = new ExperimentLogEmitter(this);
            handEvents = new ExperimentLogEmitter(this);
            gazeEvents = new ExperimentLogEmitter(this);

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

            appEvents.Log("Device Info",
                "Unique ID: " + SystemInfo.deviceUniqueIdentifier,
                "Device Model: " + SystemInfo.deviceModel,
                "Device Name: " + SystemInfo.deviceName,
                "Device Type: " + SystemInfo.deviceType,
                "Operating System: " + SystemInfo.operatingSystem,
                "System Memory Size: " + SystemInfo.systemMemorySize,
                "MAC Address: " + GetMACAddress());
                

            Debug.Log("Unique ID: " + SystemInfo.deviceUniqueIdentifier + 
                " Device Model: " + SystemInfo.deviceModel + 
                " Device Name: " + SystemInfo.deviceName + 
                " Device Type: " + SystemInfo.deviceType + 
                " Operating System: " + SystemInfo.operatingSystem + 
                " Processor Type: " + SystemInfo.processorType + 
                " Processor Count: " + SystemInfo.processorCount + 
                " System Memory Size: " + SystemInfo.systemMemorySize + 
                " MAC Address: " + GetMACAddress());

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
#if !UNITY_EDITOR
        TrackHead();
        TrackEyes();

        //// Only log hand data if we have valid subsystem and events
        /// Not tracking hands in this version, but keeping the code for future use

        //if (handSubsystem != null && handEvents != null)
        //{
        //    CheckAndLogHandTrackingState(handSubsystem.leftHand, "Left");
        //    CheckAndLogHandTrackingState(handSubsystem.rightHand, "Right");
        //}

        TrackControllers();
#endif
    }

    private void TrackControllers()
    {
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.LeftHand || nodeState.nodeType == XRNode.RightHand)
            {
                Vector3 position;
                Quaternion rotation;
                if (nodeState.TryGetPosition(out position) && nodeState.TryGetRotation(out rotation))
                {
                    string label = nodeState.nodeType == XRNode.LeftHand ? "LeftController" : "RightController";
                    handEvents.Log($"{label}Data", position, rotation, Time.time);
                }
            }
        }
    }

    private void TrackHead()
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
                    headEvents.Log("HeadData", headPosition, headRotation, Time.time);
                }
            }
        }
    }

    private void TrackEyes()
    {
        // Get left and right eye position and rotation
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);
        foreach (XRNodeState nodeState in nodeStates)
        {
            if (nodeState.nodeType == XRNode.LeftEye || nodeState.nodeType == XRNode.RightEye)
            {
                Vector3 eyePosition;
                Quaternion eyeRotation;
                if (nodeState.TryGetPosition(out eyePosition) && nodeState.TryGetRotation(out eyeRotation))
                {
                    string label = nodeState.nodeType == XRNode.LeftEye ? "LeftEye" : "RightEye";
                    headEvents.Log($"{label}Data", eyePosition, eyeRotation, Time.time);
                }
            }

            if (nodeState.nodeType == XRNode.CenterEye)
            {
                Vector3 centerEyePosition;
                Quaternion centerEyeRotation;
                if (nodeState.TryGetPosition(out centerEyePosition) && nodeState.TryGetRotation(out centerEyeRotation))
                {
                    headEvents.Log("CenterEyeData", centerEyePosition, centerEyeRotation, Time.time);
                }
            }
        }
    }

    private void CheckAndLogHandTrackingState(XRHand hand, string label)
    {
        bool isCurrentlyTracked = hand.isTracked;
        bool wasTracked = label == "Left" ? leftHandWasTracked : rightHandWasTracked;

        // Log state changes
        if (isCurrentlyTracked != wasTracked)
        {
            if (isCurrentlyTracked)
            {
                appEvents.Log($"{label} Hand tracking started");
            }
            else
            {
                appEvents.Log($"{label} Hand tracking stopped");
            }

            // Update tracking state
            if (label == "Left")
            {
                leftHandWasTracked = isCurrentlyTracked;
            }
            else
            {
                rightHandWasTracked = isCurrentlyTracked;
            }
        }

        // Only log hand data if the hand is being tracked
        if (isCurrentlyTracked)
        {
            LogHandData(hand, label);
        }
    }

    void LogHandData(XRHand hand, string label)
    {
        foreach (XRHandJointID jointId in System.Enum.GetValues(typeof(XRHandJointID)))
        {
            // Skip EndMarker and any negative/invalid values
            if (jointId <= XRHandJointID.Invalid || jointId >= XRHandJointID.EndMarker)
                continue;

            XRHandJoint joint = hand.GetJoint(jointId);
            if (joint.TryGetPose(out Pose pose))
            {
                // Create a unique key for each joint by combining the label and joint ID
                string jointKey = $"{label}Joint_{jointId}";
                handEvents.Log(jointKey, pose.position, pose.rotation, Time.time);
            }
        }
    }

    void OnDestroy()
    {
        if (handSubsystem != null)
        {
            handSubsystem.Stop();
        }
    }









    //XR Gaze Tracking with Debugging using non deprecated methods. Current XRNode states are going to be deprecated in the future. However it does not work with the current XR SDK.

    private void TrackGazeDebug()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("TrackGaze: mainCamera is null, cannot log gaze data.");
            return;
        }

        Vector3 gazePosition = mainCamera.transform.position;
        Quaternion gazeRotation = mainCamera.transform.rotation;
        float timeNow = Time.time;

        float positionDelta = Vector3.Distance(gazePosition, lastGazePosition);
        float rotationDelta = Quaternion.Angle(gazeRotation, lastGazeRotation);

        Debug.Log($"TrackGaze: time={timeNow}, position={gazePosition}, rotation={gazeRotation.eulerAngles}");
        Debug.Log($"TrackGaze: positionDelta={positionDelta}, rotationDelta={rotationDelta}, timeSinceLastLog={timeNow - lastGazeLogTime}");

        // Log if enough time has passed and the change exceeds thresholds
        if ((timeNow - lastGazeLogTime) >= gazeLogInterval &&
            (positionDelta > gazePositionThreshold || rotationDelta > gazeRotationThreshold))
        {
            Debug.Log("TrackGaze: Logging GazeData (Camera)");
            gazeEvents.Log("GazeData (Debug Camera)", gazePosition, gazeRotation, timeNow);
            lastGazeLogTime = timeNow;
            lastGazePosition = gazePosition;
            lastGazeRotation = gazeRotation;
        }
        else
        {
            Debug.Log("TrackGaze: Not logging this frame (thresholds or interval not met).");
        }
    }

    private void TrackGaze()
    {
        // Enhanced gaze tracking with throttling
        if (headset.isValid)
        {
            // Get headset position and rotation
            if (headset.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                // Get the forward direction from the rotation
                Vector3 gazeDirection = rotation * Vector3.forward;

                // Check if enough time has passed and if position/rotation has changed significantly
                if ((Vector3.Distance(position, lastGazePosition) > gazePositionThreshold ||
                     Quaternion.Angle(rotation, lastGazeRotation) > gazeRotationThreshold))
                {
                    // Log detailed gaze information
                    gazeEvents.Log("GazeData (Headset)",
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

                    // Update last logged values
                    lastGazeLogTime = Time.time;
                    lastGazePosition = position;
                    lastGazeRotation = rotation;
                }
            }
        }
        else if (mainCamera != null)
        {
            // Fallback to camera-based tracking if headset is not available
            Vector3 gazePosition = mainCamera.transform.position;
            Vector3 gazeDirection = mainCamera.transform.forward;

            // Check if enough time has passed and if position/rotation has changed significantly
            if ((Vector3.Distance(gazePosition, lastGazePosition) > gazePositionThreshold ||
                 Quaternion.Angle(mainCamera.transform.rotation, lastGazeRotation) > gazeRotationThreshold))
            {
                gazeEvents.Log("GazeData (Camera)",
                    gazePosition,
                    mainCamera.transform.rotation,
                    gazeDirection,
                    Time.time
                );

                // Update last logged values
                lastGazeLogTime = Time.time;
                lastGazePosition = gazePosition;
                lastGazeRotation = mainCamera.transform.rotation;
            }
        }
        else
        {
            appEvents.Log("Gaze tracking not available",
                "No valid headset or camera found for gaze tracking.");
        }
    }

}
