using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;

public class SpatialAnchorManager : MonoBehaviour
{

    private InputDevice rightHand;
    private InputDevice leftHand;
    public Transform rightControllerTransform;
    private AnchorPlacer anchorPlacer;
    private AnchorAlignmentManager anchorAlignmentManager;
    private AnchorVisual anchorVisual;
    private bool rightTriggerLastFrame = false;
    private bool leftTriggerLastFrame = false;
    private bool rightPrimaryButtonLastFrame = false;
    private bool leftPrimaryButtonLastFrame = false;
    private bool anchorsVisible = true;
    public static SpatialAnchorManager Instance { get; private set; }

    void Start()
    {
        anchorPlacer = FindFirstObjectByType<AnchorPlacer>();
        anchorAlignmentManager = FindFirstObjectByType<AnchorAlignmentManager>();
        anchorVisual = FindFirstObjectByType<AnchorVisual>();
        TryInitializeDevices();
        InputDevices.deviceConnected += OnDeviceConnected;
    }

    void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
    }

    void Update()
    {
        // Reinitialize if devices have become invalid
        if (!rightHand.isValid || !leftHand.isValid)
        {
            TryInitializeDevices();
        }

        // --- RIGHT HAND: Place Anchor ---
        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTrigger))
        {
            if (rightTrigger && !rightTriggerLastFrame && anchorPlacer.getAnchorTransforms().Count < 3)
            {
                anchorPlacer.PlaceAnchor(rightControllerTransform.position, rightControllerTransform.rotation);
            }
            rightTriggerLastFrame = rightTrigger;
        }

        // --- RIGHT HAND: Align Environment ---    
        if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton))
        {
            if (primaryButton && !rightPrimaryButtonLastFrame)
            {
                Debug.Log("Primary button pressed");

                List<Transform> anchors = new List<Transform>();
                foreach (Transform anchor in anchorPlacer.getAnchorTransforms())
                {
                    anchors.Add(anchor);
                }
                    anchorAlignmentManager.AlignEnvironment(anchors);
            }
            rightPrimaryButtonLastFrame = primaryButton;
        }


        // --- LEFT HAND: Delete Anchor ---
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrigger))
        {
            if (leftTrigger && !leftTriggerLastFrame)
            {
                anchorPlacer.DeleteLastAnchor();
            }
            leftTriggerLastFrame = leftTrigger;
        }

        if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimaryButton))
        {
            if (leftPrimaryButton && !leftPrimaryButtonLastFrame)
            {
                if (anchorsVisible)
                {
                    anchorVisual.HideVisuals();
                    anchorsVisible = false;
                }
                else
                {
                    anchorVisual.ShowVisuals();
                    anchorsVisible = true;
                }
            }
            leftPrimaryButtonLastFrame = leftPrimaryButton;
        }
    }

    void TryInitializeDevices()
    {
        List<InputDevice> devices = new List<InputDevice>();

        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
            rightHand = devices[0];

        devices.Clear();

        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
        if (devices.Count > 0)
            leftHand = devices[0];
    }

    void OnDeviceConnected(InputDevice device)
    {
        if (!rightHand.isValid && device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            rightHand = device;

        if (!leftHand.isValid && device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            leftHand = device;
    }

} 
