using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;

public class EnvironmentAlignment : MonoBehaviour
{
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform environmentRoot;
    [SerializeField] private float alignmentThreshold = 0.1f;
    [SerializeField] private float maxAlignmentDistance = 2.0f;

    private ComponentLogEmitter appEvents;
    private Vector3 lastHeadPosition;
    private Quaternion lastHeadRotation;
    private bool isAligned = false;
    private InputDevice headset;

    void Start()
    {
        appEvents = new ComponentLogEmitter(this, Ubiq.Logging.EventType.Application);
        
        // Initialize headset tracking
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);
        if (inputDevices.Count > 0)
        {
            headset = inputDevices[0];
            appEvents.Log("Headset tracking initialized");
        }
        else
        {
            appEvents.Log("No headset found. Using camera for tracking.");
        }

        if (environmentRoot == null)
        {
            environmentRoot = transform;
        }
    }

    void Update()
    {
        if (headset.isValid)
        {
            // Get headset position and rotation
            if (headset.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) &&
                headset.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            {
                // Check if we need to update alignment
                if (!isAligned || 
                    Vector3.Distance(position, lastHeadPosition) > alignmentThreshold ||
                    Quaternion.Angle(rotation, lastHeadRotation) > alignmentThreshold)
                {
                    UpdateAlignment(position, rotation);
                }
            }
        }
        else if (Camera.main != null)
        {
            // Fallback to camera-based tracking
            Vector3 position = Camera.main.transform.position;
            Quaternion rotation = Camera.main.transform.rotation;

            if (!isAligned || 
                Vector3.Distance(position, lastHeadPosition) > alignmentThreshold ||
                Quaternion.Angle(rotation, lastHeadRotation) > alignmentThreshold)
            {
                UpdateAlignment(position, rotation);
            }
        }
    }

    private void UpdateAlignment(Vector3 headPosition, Quaternion headRotation)
    {
        // Calculate the offset between the head and environment
        Vector3 offset = headPosition - environmentRoot.position;
        
        // Only update if within max distance
        if (offset.magnitude <= maxAlignmentDistance)
        {
            // Update environment position
            environmentRoot.position = headPosition;
            
            // Update environment rotation to match head rotation
            environmentRoot.rotation = headRotation;

            // Log alignment update
            appEvents.Log("Environment aligned", 
                environmentRoot.position,
                environmentRoot.rotation,
                Time.time
            );

            isAligned = true;
        }
        else
        {
            appEvents.Log("Environment alignment skipped - too far", 
                offset.magnitude,
                maxAlignmentDistance,
                Time.time
            );
        }

        // Update last known position and rotation
        lastHeadPosition = headPosition;
        lastHeadRotation = headRotation;
    }

    public void ResetAlignment()
    {
        isAligned = false;
        appEvents.Log("Environment alignment reset");
    }
}
