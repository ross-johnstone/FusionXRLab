using UnityEngine;
using UnityEngine.XR;
using Ubiq.Logging;
using System.Collections.Generic;

public class XRRotationControl : MonoBehaviour
{
    [Header("Scene References")]
    public Transform xrRig; // The XR Origin/Rig transform
    public Transform vrEnvironment; // The VR Environment object transform
    public Transform networkScene; // The networked scene transform

    [Header("Control Settings")]
    public float maxRotationSpeed = 90f;
    public float exponentialFactor = 2f;

    private InputDevice leftHand;
    private InputDevice rightHand;
    private ExperimentLogEmitter events;

    void Start()
    {
        events = new ExperimentLogEmitter(this);
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        RefreshControllers();
    }

    void Update()
    {
        if (!leftHand.isValid || !rightHand.isValid) return;

        HandleThumbstickRotation();
    }

    private void HandleThumbstickRotation()
    {
        // Left thumbstick controls XR Rig rotation
        if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftThumbstickValue) && 
            leftThumbstickValue != Vector2.zero)
        {
            float horizontalInput = leftThumbstickValue.x;
            float scaledInput = Mathf.Sign(horizontalInput) * Mathf.Pow(Mathf.Abs(horizontalInput), exponentialFactor);
            float rotationAmount = scaledInput * Time.deltaTime * maxRotationSpeed;
            
            if (Mathf.Abs(rotationAmount) > 0.01f)
            {
                RotateXRRig(rotationAmount);
            }
        }

        // Right thumbstick controls VR Environment rotation
        if (rightHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightThumbstickValue) && 
            rightThumbstickValue != Vector2.zero)
        {
            float horizontalInput = rightThumbstickValue.x;
            float scaledInput = Mathf.Sign(horizontalInput) * Mathf.Pow(Mathf.Abs(horizontalInput), exponentialFactor);
            float rotationAmount = scaledInput * Time.deltaTime * maxRotationSpeed;
            
            if (Mathf.Abs(rotationAmount) > 0.01f)
            {
                RotateVREnvironment(rotationAmount);
            }
        }
    }

    private void RotateXRRig(float rotationAmount)
    {
        if (xrRig == null) return;

        // Store original position
        Vector3 originalPosition = xrRig.position;

        // Rotate the XR Rig around its current position
        xrRig.Rotate(Vector3.up, rotationAmount, Space.World);

        // Restore position to prevent any drift
        xrRig.position = originalPosition;

        LogRotation("XR Rig", rotationAmount);
    }

    private void RotateVREnvironment(float rotationAmount)
    {
        if (vrEnvironment == null) return;

        // Store original position
        Vector3 originalPosition = vrEnvironment.position;

        // Rotate the VR Environment around its current position
        vrEnvironment.Rotate(Vector3.up, rotationAmount, Space.World);

        // Restore position to prevent any drift
        vrEnvironment.position = originalPosition;

        LogRotation("VR Environment", rotationAmount);
    }

    private void LogRotation(string target, float rotationAmount)
    {
        Debug.Log($"{target} rotated {rotationAmount:F2} degrees");
        events?.Log($"{target} rotated {rotationAmount:F2} degrees");
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
            Debug.Log($"Found left controller: {leftHand.name}");
        }

        if (rightHandDevices.Count > 0)
        {
            rightHand = rightHandDevices[0];
            Debug.Log($"Found right controller: {rightHand.name}");
        }
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            leftHand = device;
            Debug.Log($"Left controller connected: {leftHand.name}");
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            rightHand = device;
            Debug.Log($"Right controller connected: {rightHand.name}");
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
                Debug.Log("Right controller disconnected");
            }
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            if (leftHand == device)
            {
                leftHand = default(InputDevice);
                Debug.Log("Left controller disconnected");
            }
        }
    }

    void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }
}
