using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Ubiq.Logging;

public class AdminControls : MonoBehaviour
{
    public Transform rootTransform; // The object that contains your whole scene
    ExperimentLogEmitter events;
    public bool adminControlsEnabled = true;
    BoundaryAligner aligner;
    float exponentialFactor = 2f; 
    public float maxRotationSpeed = 90f;
    Coroutine reenableCoroutine;
    private Quaternion savedRotation;

    InputDevice leftHand;
    InputDevice rightHand;

    //void Start()
    //{
    //    var leftHandDevices = new List<InputDevice>();
    //    InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
    //    if (leftHandDevices.Count > 0)
    //    {
    //        leftHand = leftHandDevices[0];
    //    }

    //    var rightHandDevices = new List<InputDevice>();
    //    InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
    //    if (rightHandDevices.Count > 0)
    //    {
    //        rightHand = rightHandDevices[0];
    //    }
    //}

    void Start()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        List<InputDevice> allDevices = new List<InputDevice>();
        InputDevices.GetDevices(allDevices);
        foreach (var device in allDevices)
        {
            OnDeviceConnected(device);
        }
    }


    void Update()
    {

        if (!leftHand.isValid || !rightHand.isValid)
        {
            List<InputDevice> allDevices = new List<InputDevice>();
            InputDevices.GetDevices(allDevices);
            foreach (var device in allDevices)
            {
                OnDeviceConnected(device);
            }
        }


        if (adminControlsEnabled) {

            if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftThumbstickValue) && leftThumbstickValue != Vector2.zero)
            {
                float horizontalInput = leftThumbstickValue.x;
                float scaledInput = Mathf.Sign(horizontalInput) * Mathf.Pow(Mathf.Abs(horizontalInput), exponentialFactor);
                float rotationAmount = scaledInput * Time.deltaTime * maxRotationSpeed;
                RotateSceneThumbstickAroundPoint(rootTransform.position, rotationAmount);
            }

             // Press B button to save rotation
            if (rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool bPressed) && bPressed)
            {
                SaveCurrentRotation();
            }

            // Press A button to restore rotation
            if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed) && aPressed)
            {
                RestoreSavedRotation();
            }

            // Right Trigger Button (Align scene to play area)
            if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTriggerPressed) && rightTriggerPressed)
            {
                if (aligner == null)
                {
                    aligner = FindFirstObjectByType<BoundaryAligner>();
                    if (aligner == null)
                    {
                        Debug.LogError("BoundaryAligner not found in the scene.");
                        return;
                    }
                }
                aligner.TryAlignSceneToPlayArea();
            }

            // SecondaryThumbstickDown (Left Thumbstick Y-axis Down)
            if (rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool rightGripPressed))
            {
                if (rightGripPressed) 
                {
                    adminControlsEnabled = false;
                }
            }
        } 
        else if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightAPressed) && 
                 rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightBPressed) &&
                 leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftAPressed) &&
                 leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftBPressed) &&
                 rightAPressed && rightBPressed && leftAPressed && leftBPressed)
        {
            adminControlsEnabled = true;
            Debug.Log("Admin controls re-enabled.");
        }

    }

    void RotateSceneThumbstickAroundPoint(Vector3 pivot, float rotationAmount)
    {
        if (rootTransform == null)
        {
            Debug.LogError("Root transform is not assigned!");
            return;
        }

        rootTransform.RotateAround(pivot, Vector3.up, rotationAmount);

        Debug.Log("Scene rotated "+ rotationAmount + " degrees around midpoint of hypotenuse.");
        events.Log("Scene rotated "+ rotationAmount + " degrees around midpoint of hypotenuse.");
    }

    void SaveCurrentRotation()
    {
        if (rootTransform == null) return;
        savedRotation = rootTransform.rotation;
        Debug.Log("Rotation saved: " + savedRotation.eulerAngles);
    }

    void RestoreSavedRotation()
    {
        if (rootTransform == null) return;
        rootTransform.rotation = savedRotation;
        Debug.Log("Rotation restored to: " + savedRotation.eulerAngles);
        InvokeRepeating("TryAlignSceneToPlayArea", 20f, 20f);
    }

    void OnDeviceConnected(InputDevice device)
    {
        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            if (!rightHand.isValid || rightHand != device)
            {
                rightHand = device;
                Debug.Log("Right controller connected: " + rightHand.name);
            }
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            if (!leftHand.isValid || leftHand != device)
            {
                leftHand = device;
                Debug.Log("Left controller connected: " + leftHand.name);
            }
        }
    }

}
