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
    private bool rightSecondaryButtonLastFrame = false;
    private bool leftPrimaryButtonLastFrame = false;
    private bool anchorsVisible = true;
    private ComponentLogEmitter events;
    public static SpatialAnchorManager Instance { get; private set; }

    void Start()
    {
        events = new ComponentLogEmitter(this);
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
                // Show preview when trigger is first pressed
                anchorPlacer.ShowPreview(rightControllerTransform.position, rightControllerTransform.rotation);
            }
            else if (rightTrigger && anchorPlacer.getAnchorTransforms().Count < 3)
            {
                // Update preview position while trigger is held
                anchorPlacer.ShowPreview(rightControllerTransform.position, rightControllerTransform.rotation);
            }
            else if (!rightTrigger && rightTriggerLastFrame)
            {
                // Place anchor and hide preview when trigger is released
                if (anchorPlacer.getAnchorTransforms().Count < 3)
                {
                    anchorPlacer.PlaceAnchor(rightControllerTransform.position, rightControllerTransform.rotation);
                    events.Log("[SpatialAnchorManager] Anchor placed");
                }
                anchorPlacer.HidePreview();
            }
            rightTriggerLastFrame = rightTrigger;
        }

        // --- RIGHT HAND: Align Environment ---    
        if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton))
        {
            if (primaryButton && !rightPrimaryButtonLastFrame)
            {
                Debug.Log("[SpatialAnchorManager] Primary button pressed");
                StartRepeatedAlignment();
                events.Log("[SpatialAnchorManager] Environment alignment started");
            }
            rightPrimaryButtonLastFrame = primaryButton;
        }

        // --- RIGHT HAND: Reset Environment ---
        if (rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButton))
        {
            if (secondaryButton && !rightSecondaryButtonLastFrame)
            {
                anchorAlignmentManager.ResetEnvironment();
                StopRepeatedAlignment();
                events.Log("[SpatialAnchorManager] Environment reset");
            }
            rightSecondaryButtonLastFrame = secondaryButton;
        }

        // --- LEFT HAND: Delete Anchor ---
        if (leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrigger))
        {
            if (leftTrigger && !leftTriggerLastFrame)
            {
                anchorPlacer.DeleteLastAnchor();
                StopRepeatedAlignment();
                events.Log("[SpatialAnchorManager] Anchor deleted");
            }
            leftTriggerLastFrame = leftTrigger;
        }

        // --- LEFT HAND: Toggle Anchors Visibility ---
        if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimaryButton))
        {
            if (leftPrimaryButton && !leftPrimaryButtonLastFrame)
            {
                if (anchorsVisible)
                {
                    anchorVisual.HideVisuals();
                    anchorsVisible = false;
                    events.Log("[SpatialAnchorManager] Anchors hidden");
                }
                else
                {
                    anchorVisual.ShowVisuals();
                    anchorsVisible = true;
                    events.Log("[SpatialAnchorManager] Anchors shown");
                }
            }
            leftPrimaryButtonLastFrame = leftPrimaryButton;
        }
    }

    private void StartRepeatedAlignment()
    {
        // Cancel any existing alignment first
        StopRepeatedAlignment();
        // Start new alignment
        InvokeRepeating("PerformAlignment", 0.5f, 0.5f);
    }

    private void StopRepeatedAlignment()
    {
        CancelInvoke("PerformAlignment");
    }

    private void PerformAlignment()
    {
        List<Transform> anchors = anchorPlacer.getAnchorTransforms();
        if (anchors.Count > 0)
        {
            anchorAlignmentManager.RelocateRootAnchors(anchors);
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
