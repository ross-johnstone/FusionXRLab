using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using System.Linq;
#if OCULUS_INTEGRATION
using Oculus.Platform;
using Oculus.Platform.Models;
#endif

public class XRRotationControl : MonoBehaviour
{
    [Header("Scene References")]
    public Transform xrRig; // The XR Origin/Rig transform
    public Transform vrEnvironment; // The VR Environment object transform
    public Transform networkScene; // The networked scene transform
    public Transform xrCamera; // Assign this in the inspector to your XR camera (e.g., CenterEyeAnchor)

    [Header("Control Settings")]
    public float maxRotationSpeed = 90f;
    public float exponentialFactor = 2f;

    [Header("Room Center (auto-calculated from OVRBoundary)")]
    [Tooltip("Automatically calculated from OVRBoundary PlayArea")] 
    public Vector3 roomCenter = Vector3.zero;
    public float moveSpeed = 2f; // Units per second

    [Header("Control Modes")]
    public bool RotationMode = true; // True = rotate with left/right, False = move with thumbstick

    private InputDevice leftHand;
    private InputDevice rightHand;

    private bool xButtonWasPressed = false;

    void Start()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        RefreshControllers();
        RefreshRoomCenter();
    }

    void Update()
    {
        if (!leftHand.isValid || !rightHand.isValid) return;

        if (ControlsManager.Instance.AreControlsEnabled())
        {
            HandleModeToggle();
            HandleThumbstickControls();
        }
    }

    private void HandleModeToggle()
    {
        // Toggle RotationMode when X (secondary button) on left controller is pressed
        if (leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool xPressed) && xPressed)
        {
            // Only toggle on button down, not every frame while held
            if (!xButtonWasPressed)
            {
                RotationMode = !RotationMode;
                Debug.Log($"[XRRotationControl] RotationMode set to {RotationMode}");
            }
            xButtonWasPressed = true;
        }
        else
        {
            xButtonWasPressed = false;
        }
    }

    private void HandleThumbstickControls()
    {
        // Left thumbstick: XR Rig
        if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftThumbstickValue))
        {
            if (RotationMode)
            {
                // Rotation mode: left/right rotates XR Rig
                HandleRotation(leftThumbstickValue.x, xrRig);
            }
            else
            {
                // Movement mode: move XR Rig
                HandleMovement(leftThumbstickValue, xrRig);
            }
        }
        // Right thumbstick: VR Environment
        if (rightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightThumbstickValue))
        {
            if (RotationMode)
            {
                // Rotation mode: left/right rotates VR Environment
                HandleRotation(rightThumbstickValue.x, vrEnvironment);
            }
            else
            {
                // Movement mode: move VR Environment
                HandleMovement(rightThumbstickValue, vrEnvironment);
            }
        }
    }

    private void HandleRotation(float horizontalInput, Transform target)
    {
        float scaledInput = Mathf.Sign(horizontalInput) * Mathf.Pow(Mathf.Abs(horizontalInput), exponentialFactor);
        float rotationAmount = scaledInput * Time.deltaTime * maxRotationSpeed;
        if (Mathf.Abs(rotationAmount) > 0.01f)
        {
            RotateTransform(rotationAmount, target);
        }
    }

    private void HandleMovement(Vector2 thumbstickValue, Transform target)
    {
        // Forward/back
        if (Mathf.Abs(thumbstickValue.y) > 0.01f)
        {
            MoveTransform(thumbstickValue.y, xrCamera != null ? xrCamera.forward : Camera.main.transform.forward, target);
        }
        // Left/right
        if (Mathf.Abs(thumbstickValue.x) > 0.01f)
        {
            MoveTransform(thumbstickValue.x, xrCamera != null ? xrCamera.right : Camera.main.transform.right, target);
        }
    }

    private void MoveTransform(float thumbstickDirection, Vector3 moveDirectionVector, Transform target)
    {
        moveDirectionVector.y = 0f;
        moveDirectionVector.Normalize();
        target.position += moveDirectionVector * thumbstickDirection * moveSpeed * Time.deltaTime;
    }

    private void RotateTransform(float rotationAmount, Transform target)
    {
        if (target == null) return;
        Vector3 originalPosition = target.position;
        target.Rotate(Vector3.up, rotationAmount, Space.World);
        target.position = originalPosition;
        LogRotation(target.name, rotationAmount);
    }

    /// <summary>
    /// Refreshes the room center by averaging the OVRBoundary PlayArea points.
    /// Call this after realignment.
    /// </summary>
    public void RefreshRoomCenter()
    {
#if !UNITY_EDITOR
        if (OVRManager.boundary != null && OVRManager.boundary.GetConfigured())
        {
            var boundaryPoints = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            if (boundaryPoints != null && boundaryPoints.Length > 0)
            {
                Vector3 center = Vector3.zero;
                foreach (var pt in boundaryPoints)
                {
                    center += new Vector3(pt.x, 0, pt.z);
                }
                center /= boundaryPoints.Length;
                roomCenter = center;
                Debug.Log($"[XRRotationControl] Room center updated to {roomCenter}");
            }
            else
            {
                Debug.LogWarning("[XRRotationControl] No boundary points found for PlayArea.");
            }
        }
        else
        {
            Debug.LogWarning("[XRRotationControl] OVRBoundary not configured or not available.");
        }
#endif
    }

    private void LogRotation(string target, float rotationAmount)
    {
        Debug.Log($"[XRRotationControl] {target} rotated {rotationAmount:F2} degrees");
    }

    private void RefreshControllers()
    {
        var leftHandDevices = new List<InputDevice>();
        var rightHandDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftHandDevices);
        
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightHandDevices);

        if (leftHandDevices.Count > 0)
        {
            leftHand = leftHandDevices[0];
            Debug.Log($"[XRRotationControl] Found left controller: {leftHand.name}");
        }

        if (rightHandDevices.Count > 0)
        {
            rightHand = rightHandDevices[0];
            Debug.Log($"[XRRotationControl] Found right controller: {rightHand.name}");
        }
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            leftHand = device;
            Debug.Log($"[XRRotationControl] Left controller connected: {leftHand.name}");
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            rightHand = device;
            Debug.Log($"[XRRotationControl] Right controller connected: {rightHand.name}");
        }
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            if (rightHand == device)
            {
                rightHand = default(InputDevice);
                Debug.Log("[XRRotationControl] Right controller disconnected");
            }
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            if (leftHand == device)
            {
                leftHand = default(InputDevice);
                Debug.Log("[XRRotationControl] Left controller disconnected");
            }
        }
    }

    void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }
}
