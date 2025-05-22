using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Ubiq.Logging;
using Ubiq.Avatars;

/// <summary>
/// Manages administrative controls for VR scene manipulation and controller input handling.
/// Provides functionality for scene rotation, anchor placement, and controller state management.
/// </summary>
public class AdminControls : MonoBehaviour
{
    #region Inspector Fields
    [Header("Scene References")]
    public Transform rootTransform;
    public Transform xrOrigin;
    public Transform cameraOffset;

    [Header("Control Settings")]
    public bool adminControlsEnabled = true;
    public float maxRotationSpeed = 90f;
    public float exponentialFactor = 2f;
    #endregion

    #region Private Fields
    private ExperimentLogEmitter events;
    private InputDevice leftHand;
    private InputDevice rightHand;
    private Quaternion savedRotation;
    private bool forceEnabled = false;
    private bool isInitialized = false;
    private float controllerCheckInterval = 0.1f;
    private float nextControllerCheck = 0f;
    private readonly WaitForSeconds initializationDelay = new WaitForSeconds(1f);
    #endregion

    #region Unity Lifecycle Methods
    void Awake()
    {
        events = new ExperimentLogEmitter(this);
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
    }

    void Start()
    {
        StartCoroutine(InitializeWithDelay());
    }

    void Update()
    {
        if (!isInitialized) return;

        HandleControllerInput();
        CheckControllerState();
        HandleSceneRotation();
    }

    void OnDestroy()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }
    #endregion

    #region Initialization Methods
    private IEnumerator InitializeWithDelay()
    {
        Debug.Log("Starting delayed initialization...");
        yield return initializationDelay;

        FindXRComponents();
        yield return new WaitForSeconds(1f);
        StartCoroutine(InitialControllerDetection());
    }

    private void FindXRComponents()
    {
        if (xrOrigin == null)
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin Hands (XR Rig)");
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj.transform;
                cameraOffset = xrOrigin.Find("Camera Offset") ?? xrOrigin.Find("CameraOffset");
                Debug.Log($"Found XR Origin: {xrOriginObj.name}");
                if (cameraOffset == null)
                {
                    Debug.LogWarning("Camera Offset not found under XR Origin");
                }
            }
            else
            {
                Debug.LogError("Could not find XR Origin in scene. Please assign it manually in the inspector.");
            }
        }
    }
    #endregion

    #region Controller Management
    private IEnumerator InitialControllerDetection()
    {
        int maxAttempts = 5;
        int currentAttempt = 0;
        bool controllersReady = false;

        while (currentAttempt < maxAttempts && !controllersReady)
        {
            RefreshControllers();
            controllersReady = ValidateControllerFeatures();
            
            if (!controllersReady)
            {
                currentAttempt++;
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (controllersReady)
        {
            Debug.Log("Controllers successfully initialized");
        }
        else
        {
            Debug.LogWarning("Could not fully initialize controllers. Some features may not work until controllers are reconnected.");
        }

        StartCoroutine(PeriodicControllerValidation());
        isInitialized = true;
        Debug.Log("Initialization complete. System ready.");
    }

    private bool ValidateControllerFeatures()
    {
        if (!leftHand.isValid) return false;

        bool hasThumbstick = leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out _);
        bool hasButtons = leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out _) &&
                         leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out _);

        return hasThumbstick && hasButtons;
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

        UpdateControllerReference(leftHandDevices, ref leftHand, "Left");
        UpdateControllerReference(rightHandDevices, ref rightHand, "Right");
    }

    private void UpdateControllerReference(List<InputDevice> devices, ref InputDevice currentDevice, string side)
    {
        foreach (var device in devices)
        {
            if (device.isValid && device != currentDevice)
            {
                currentDevice = device;
                Debug.Log($"Found {side.ToLower()} controller: {currentDevice.name}");
                break;
            }
        }
    }

    private IEnumerator PeriodicControllerValidation()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);

            if ((adminControlsEnabled || forceEnabled) && leftHand.isValid)
            {
                bool hasThumbstick = leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out _);
                if (!hasThumbstick)
                {
                    Debug.LogWarning("Left controller thumbstick not responding - attempting to refresh connection");
                    RefreshControllers();
                }
            }
        }
    }
    #endregion

    #region Input Handling
    private void HandleControllerInput()
    {
        if (!rightHand.isValid || !leftHand.isValid) return;

        bool rightAPressed = GetButtonState(rightHand, CommonUsages.primaryButton);
        bool rightBPressed = GetButtonState(rightHand, CommonUsages.secondaryButton);
        bool leftAPressed = GetButtonState(leftHand, CommonUsages.primaryButton);
        bool leftBPressed = GetButtonState(leftHand, CommonUsages.secondaryButton);

        if (rightAPressed && rightBPressed && leftAPressed && leftBPressed)
        {
            EnableControls();
        }
        else if (rightAPressed || leftAPressed)
        {
            SaveCurrentRotation();
        }
        else if (rightBPressed || leftBPressed)
        {
            RestoreSavedRotation();
        }

        HandleAnchorPlacement();
    }

    private bool GetButtonState(InputDevice device, InputFeatureUsage<bool> button)
    {
        return device.TryGetFeatureValue(button, out bool pressed) && pressed;
    }

    private void HandleAnchorPlacement()
    {
        if (rightHand.isValid && GetButtonState(rightHand, CommonUsages.triggerButton) && 
            SpatialAnchorManager.Instance != null)
        {
            Vector3 controllerPosition = GetControllerPosition(rightHand);
            Quaternion controllerRotation = GetControllerRotation(rightHand);
            Vector3 forward = controllerRotation * Vector3.forward;
            Vector3 anchorPosition = controllerPosition + (forward * 2f);
            
            SpatialAnchorManager.Instance.PlaceAnchor(anchorPosition);
        }
    }

    private Vector3 GetControllerPosition(InputDevice device)
    {
        device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position);
        return position;
    }

    private Quaternion GetControllerRotation(InputDevice device)
    {
        device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation);
        return rotation;
    }

    private void HandleSceneRotation()
    {
        if (!adminControlsEnabled && !forceEnabled) return;
        if (!leftHand.isValid) return;

        if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftThumbstickValue) && 
            leftThumbstickValue != Vector2.zero)
        {
            float horizontalInput = leftThumbstickValue.x;
            float scaledInput = Mathf.Sign(horizontalInput) * Mathf.Pow(Mathf.Abs(horizontalInput), exponentialFactor);
            float rotationAmount = scaledInput * Time.deltaTime * maxRotationSpeed;
            
            if (Mathf.Abs(rotationAmount) > 0.01f)
            {
                RotateSceneAroundPointThumbstick(rootTransform.position, rotationAmount);
            }
        }

        if (rightHand.isValid && GetButtonState(rightHand, CommonUsages.gripButton))
        {
            DisableControls();
        }
    }

    private void CheckControllerState()
    {
        if (Time.time > nextControllerCheck)
        {
            nextControllerCheck = Time.time + controllerCheckInterval;
            
            if ((!leftHand.isValid || !rightHand.isValid) && 
                FindAnyObjectByType<Facilitator>() == null)
            {
                Debug.Log("Periodic check: Controllers not valid, attempting to detect...");
                RefreshControllers();
            }
        }
    }
    #endregion

    #region Scene Manipulation
    private void RotateSceneAroundPointThumbstick(Vector3 pivot, float rotationAmount)
    {
        if (!ValidateSceneComponents()) return;

        Vector3 originalXROriginPos = xrOrigin.position;
        Vector3 originalCameraOffsetPos = cameraOffset != null ? cameraOffset.position : xrOrigin.position;
        Quaternion originalXROriginRot = xrOrigin.rotation;
        
        rootTransform.RotateAround(pivot, Vector3.up, rotationAmount);
        
        xrOrigin.position = originalXROriginPos;
        xrOrigin.rotation = originalXROriginRot;
        
        if (cameraOffset != null)
        {
            cameraOffset.position = originalCameraOffsetPos;
        }

        LogRotation(rotationAmount);
    }

    private bool ValidateSceneComponents()
    {
        if (rootTransform == null)
        {
            Debug.LogError("Root transform is not assigned!");
            return false;
        }

        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin not found! Please assign it in the inspector or ensure it exists in the scene.");
            return false;
        }

        if (cameraOffset == null)
        {
            Debug.LogWarning("Camera Offset not found! Scene rotation might not work correctly.");
        }

        return true;
    }

    private void LogRotation(float rotationAmount)
    {
        Debug.Log($"Scene rotated {rotationAmount:F2} degrees around pivot point.");
        events?.Log($"Scene rotated {rotationAmount:F2} degrees around pivot point.");
    }

    private void SaveCurrentRotation()
    {
        if (rootTransform == null) return;
        
        savedRotation = rootTransform.rotation;
        Debug.Log("Rotation saved: " + savedRotation.eulerAngles);
        
        UpdateAvatarInNetworkedScene();
    }

    private void UpdateAvatarInNetworkedScene()
    {
        var avatarManager = AvatarManager.Find(this);
        if (avatarManager != null)
        {
            var currentPrefab = avatarManager.avatarPrefab;
            avatarManager.avatarPrefab = null;
            avatarManager.avatarPrefab = currentPrefab;
            Debug.Log("Avatar respawned in networked scene");
        }
        else
        {
            Debug.LogWarning("AvatarManager not found in scene");
        }
    }

    private void RestoreSavedRotation()
    {
        if (rootTransform == null) return;
        
        rootTransform.rotation = savedRotation;
        Debug.Log("Rotation restored to: " + savedRotation.eulerAngles);
    }
    #endregion

    #region Control State Management
    private void EnableControls()
    {
        forceEnabled = true;
        adminControlsEnabled = true;
        Debug.Log("Controls force-enabled via all buttons pressed.");
    }

    private void DisableControls()
    {
        adminControlsEnabled = false;
        forceEnabled = false;
        Debug.Log("Controls disabled via right grip button");
    }
    #endregion

    #region Device Event Handlers
    private void OnDeviceConnected(InputDevice device)
    {
        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            leftHand = device;
            Debug.Log($"Left controller connected: {leftHand.name}");
            StartCoroutine(WaitForControllerFeatures(device, true));
        }

        if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right) &&
            device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
        {
            rightHand = device;
            Debug.Log($"Right controller connected: {rightHand.name}");
            StartCoroutine(WaitForControllerFeatures(device, false));
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

    private IEnumerator WaitForControllerFeatures(InputDevice device, bool isLeft)
    {
        int attempts = 0;
        bool featuresReady = false;
        
        while (attempts < 5 && !featuresReady && device.isValid)
        {
            featuresReady = isLeft ? 
                device.TryGetFeatureValue(CommonUsages.primary2DAxis, out _) :
                device.TryGetFeatureValue(CommonUsages.primaryButton, out _);
            
            if (!featuresReady)
            {
                yield return new WaitForSeconds(0.2f);
                attempts++;
            }
        }
        
        if (featuresReady)
        {
            Debug.Log($"{(isLeft ? "Left" : "Right")} controller features initialized");
        }
    }
    #endregion
}

