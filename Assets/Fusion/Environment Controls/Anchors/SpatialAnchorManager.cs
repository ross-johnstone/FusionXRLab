using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;

public class SpatialAnchorManager : MonoBehaviour
{
    // --- Device State ---
    private InputDevice rightHand;
    private InputDevice leftHand;
    public Transform rightControllerTransform;

    // --- Components ---
    private AnchorPlacer anchorPlacer;
    private AnchorAlignmentManager anchorAlignmentManager;
    private AnchorVisual anchorVisual;
    private ComponentLogEmitter events;
    private AvatarAligner avatarAligner;
    private GameObject xrRig;
    private Transform userHead;
    private Transform anchor;       
    private XRRoomAligner xrRoomAligner;


    // --- Input State ---
    private bool rightTriggerLastFrame = false;
    private bool leftTriggerLastFrame = false;
    private bool rightPrimaryButtonLastFrame = false;
    private bool rightSecondaryButtonLastFrame = false;
    private bool leftPrimaryButtonLastFrame = false;
    private bool anchorsVisible = true;


    // --- Singleton ---    
    public static SpatialAnchorManager Instance { get; private set; }

    void Start()
    {
        events = new ComponentLogEmitter(this);
        anchorPlacer = FindFirstObjectByType<AnchorPlacer>();
        anchorAlignmentManager = FindFirstObjectByType<AnchorAlignmentManager>();
        anchorVisual = FindFirstObjectByType<AnchorVisual>();
        xrRoomAligner = FindFirstObjectByType<XRRoomAligner>();
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
        if (!rightHand.isValid)
        {
            rightHand = default;
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            if (devices.Count > 0)
                rightHand = devices[0];
        }
        if (!leftHand.isValid)
        {
            leftHand = default;
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
            if (devices.Count > 0)
                leftHand = devices[0];
        }

        // --- RIGHT HAND: Place Anchor ---
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTrigger))
        {
            if (rightTrigger && !rightTriggerLastFrame && anchorPlacer.getAnchorTransforms().Count < 3)
            {
                anchorPlacer.ShowPreview(rightControllerTransform.position, rightControllerTransform.rotation);
            }
            else if (rightTrigger && anchorPlacer.getAnchorTransforms().Count < 3)
            {
                anchorPlacer.ShowPreview(rightControllerTransform.position, rightControllerTransform.rotation);
            }
            else if (!rightTrigger && rightTriggerLastFrame)
            {
                if (anchorPlacer.getAnchorTransforms().Count < 3)
                {
                    anchorPlacer.PlaceAnchor(rightControllerTransform.position, rightControllerTransform.rotation);
                    events.Log("[SpatialAnchorManager] Anchor placed");
                }
                anchorPlacer.HidePreview();
            }
            rightTriggerLastFrame = rightTrigger;
        }
        else
        {
            rightTriggerLastFrame = false;
        }

        // --- RIGHT HAND: Align Environment ---    
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButton))
        {
            if (primaryButton && !rightPrimaryButtonLastFrame)
            {
                Debug.Log("[SpatialAnchorManager] Primary button pressed");
                StartRepeatedAlignment();
                events.Log("[SpatialAnchorManager] Environment alignment started");
            }
            rightPrimaryButtonLastFrame = primaryButton;
        }
        else
        {
            rightPrimaryButtonLastFrame = false;
        }

        // --- RIGHT HAND: Reset Environment ---
        if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButton))
        {
            if (secondaryButton && !rightSecondaryButtonLastFrame)
            {
                anchorAlignmentManager.ResetEnvironment();
                StopRepeatedAlignment();
                events.Log("[SpatialAnchorManager] Environment reset");
            }
            rightSecondaryButtonLastFrame = secondaryButton;
        }
        else
        {
            rightSecondaryButtonLastFrame = false;
        }

        // --- LEFT HAND: Delete Anchor ---
        if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrigger))
        {
            if (leftTrigger && !leftTriggerLastFrame)
            {
                anchorPlacer.DeleteLastAnchor();
                StopRepeatedAlignment();
                events.Log("[SpatialAnchorManager] Anchor deleted");
            }
            leftTriggerLastFrame = leftTrigger;
        }
        else
        {
            leftTriggerLastFrame = false;
        }

        // --- LEFT HAND: Toggle Anchors Visibility ---
        if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftPrimaryButton))
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
        else
        {
            leftPrimaryButtonLastFrame = false;
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
            xrRoomAligner.AlignXRRig(anchors[0], anchors[1]);
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
