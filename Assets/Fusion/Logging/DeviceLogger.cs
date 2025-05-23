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
                if (Time.time - lastGazeLogTime >= gazeLogInterval &&
                    (Vector3.Distance(position, lastGazePosition) > gazePositionThreshold ||
                     Quaternion.Angle(rotation, lastGazeRotation) > gazeRotationThreshold))
                {
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
            if (Time.time - lastGazeLogTime >= gazeLogInterval &&
                (Vector3.Distance(gazePosition, lastGazePosition) > gazePositionThreshold ||
                 Quaternion.Angle(mainCamera.transform.rotation, lastGazeRotation) > gazeRotationThreshold))
            {
                gazeEvents.Log("GazeData", 
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

        // Only log hand data if we have valid subsystem and events
        if (handSubsystem != null && handEvents != null)
        {
            CheckAndLogHandTrackingState(handSubsystem.leftHand, "Left");
            CheckAndLogHandTrackingState(handSubsystem.rightHand, "Right");
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
                handEvents.Log($"{label}HandTrackingStarted", Time.time);
            }
            else
            {
                appEvents.Log($"{label} Hand tracking stopped");
                handEvents.Log($"{label}HandTrackingStopped", Time.time);
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
}
