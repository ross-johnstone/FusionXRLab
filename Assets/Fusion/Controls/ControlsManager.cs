using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

// To be extended in the future to enable and disable controls based on the user's role

public class ControlsManager : MonoBehaviour
{
    private static ControlsManager instance;
    public static ControlsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ControlsManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("ControlsManager");
                    instance = obj.AddComponent<ControlsManager>();
                }
            }
            return instance;
        }
    }

    private InputDevice leftHand;
    private InputDevice rightHand;
    private bool controlsEnabled = true;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        RefreshControllers();
    }

    void Update()
    {
        if (!leftHand.isValid || !rightHand.isValid) return;

        CheckControlState();
    }

    private void CheckControlState()
    {
        // Check for disable combination (all triggers and grips)
        bool leftTrigger = GetButtonState(leftHand, CommonUsages.triggerButton);
        bool rightTrigger = GetButtonState(rightHand, CommonUsages.triggerButton);
        bool leftGrip = GetButtonState(leftHand, CommonUsages.gripButton);
        bool rightGrip = GetButtonState(rightHand, CommonUsages.gripButton);

        if (leftTrigger && rightTrigger && leftGrip && rightGrip)
        {
            DisableControls();
        }

        // Check for enable combination (A and B on both controllers)
        bool leftA = GetButtonState(leftHand, CommonUsages.primaryButton);
        bool rightA = GetButtonState(rightHand, CommonUsages.primaryButton);
        bool leftB = GetButtonState(leftHand, CommonUsages.secondaryButton);
        bool rightB = GetButtonState(rightHand, CommonUsages.secondaryButton);

        if (leftA && rightA && leftB && rightB)
        {
            EnableControls();
        }
    }

    private bool GetButtonState(InputDevice device, InputFeatureUsage<bool> button)
    {
        return device.TryGetFeatureValue(button, out bool pressed) && pressed;
    }

    public void EnableControls()
    {
        if (!controlsEnabled)
        {
            controlsEnabled = true;
            Debug.Log("[ControlsManager] Controls enabled");
        }
    }

    public void DisableControls()
    {
        if (controlsEnabled)
        {
            controlsEnabled = false;
            Debug.Log("[ControlsManager] Controls disabled");
        }
    }

    public bool AreControlsEnabled()
    {
        return controlsEnabled;
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
            Debug.Log($"[ControlsManager] Found left controller: {leftHand.name}");
        }

        if (rightHandDevices.Count > 0)
        {
            rightHand = rightHandDevices[0];
            Debug.Log($"[ControlsManager] Found right controller: {rightHand.name}");
        }
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            leftHand = device;
            Debug.Log($"[ControlsManager] Left controller connected: {leftHand.name}");
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            rightHand = device;
            Debug.Log($"[ControlsManager] Right controller connected: {rightHand.name}");
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
                Debug.Log("[ControlsManager] Right controller disconnected");
            }
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            if (leftHand == device)
            {
                leftHand = default(InputDevice);
                Debug.Log("[ControlsManager] Left controller disconnected");
            }
        }
    }

    void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }
}
