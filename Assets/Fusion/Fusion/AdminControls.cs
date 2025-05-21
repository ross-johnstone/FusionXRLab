using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using Ubiq.Logging;
using Ubiq.Avatars;

public class AdminControls : MonoBehaviour
{
    public Transform rootTransform; // The object that contains your whole scene
    ExperimentLogEmitter events; // Changed to interface type
    public bool adminControlsEnabled = true;
    BoundaryAligner aligner;
    float exponentialFactor = 2f; 
    public float maxRotationSpeed = 90f;
    Coroutine reenableCoroutine;
    private Quaternion savedRotation;
    public Transform xrOrigin; // Reference to the XR Origin transform
    public Transform cameraOffset; // Reference to the Camera Offset transform under XR Origin

    InputDevice leftHand;
    InputDevice rightHand;

    private float controllerCheckInterval = 0.1f; // Check more frequently
    private float nextControllerCheck = 0f;
    private bool forceEnabled = false;
    private bool isInitialized = false;
    private WaitForSeconds initializationDelay = new WaitForSeconds(1f);

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

    IEnumerator InitializeWithDelay()
    {
        Debug.Log("Starting delayed initialization...");
        
        // Wait for XR to initialize
        yield return initializationDelay;

        // Find XR Origin if not set
        if (xrOrigin == null)
        {
            GameObject xrOriginObj = GameObject.Find("XR Origin Hands (XR Rig)");
            
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj.transform;
                Transform foundOffset = xrOrigin.Find("Camera Offset");
                if (foundOffset == null) foundOffset = xrOrigin.Find("CameraOffset");
                cameraOffset = foundOffset;
                
                Debug.Log($"Found XR Origin: {xrOriginObj.name}");
                if (cameraOffset != null)
                {
                    Debug.Log("Found Camera Offset");
                }
                else
                {
                    Debug.LogWarning("Camera Offset not found under XR Origin");
                }
            }
            else
            {
                Debug.LogError("Could not find XR Origin in scene. Please assign it manually in the inspector.");
            }
        }

        // Wait additional time for controllers to fully initialize
        yield return new WaitForSeconds(1f);

        // Initial controller detection with retries
        StartCoroutine(InitialControllerDetection());
    }

    IEnumerator InitialControllerDetection()
    {
        int maxAttempts = 5;
        int currentAttempt = 0;
        bool controllersReady = false;

        while (currentAttempt < maxAttempts && !controllersReady)
        {
            ForceControllerRefresh();
            
            // Test if controllers are fully functional
            if (leftHand.isValid)
            {
                bool hasThumbstick = leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 thumbstick);
                bool hasButtons = leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out _) &&
                                leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out _);
                
                if (hasThumbstick && hasButtons)
                {
                    Debug.Log("Left controller fully initialized with all required features");
                    controllersReady = true;
                }
                else
                {
                    Debug.Log($"Left controller features not ready (Attempt {currentAttempt + 1}/{maxAttempts}): Thumbstick={hasThumbstick}, Buttons={hasButtons}");
                }
            }

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

        // Start the periodic validation
        StartCoroutine(ValidateControllers());
        isInitialized = true;
        Debug.Log("Initialization complete. System ready.");
    }

    IEnumerator ValidateControllers()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);

            if ((adminControlsEnabled || forceEnabled) && leftHand.isValid)
            {
                // Check thumbstick functionality
                bool hasThumbstick = leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out _);
                if (!hasThumbstick)
                {
                    Debug.LogWarning("Left controller thumbstick not responding - attempting to refresh connection");
                    ForceControllerRefresh();
                }
            }
        }
    }

    private void ForceControllerRefresh()
    {
        var leftHandDevices = new List<InputDevice>();
        var rightHandDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            leftHandDevices);
        
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            rightHandDevices);

        foreach (var device in leftHandDevices)
        {
            if (device.isValid)
            {
                if (device != leftHand) // Only log if it's a new device
                {
                    leftHand = device;
                    Debug.Log($"Found left controller: {leftHand.name}");
                }
                break;
            }
        }

        foreach (var device in rightHandDevices)
        {
            if (device.isValid)
            {
                if (device != rightHand) // Only log if it's a new device
                {
                    rightHand = device;
                    Debug.Log($"Found right controller: {rightHand.name}");
                }
                break;
            }
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        // Check for force-enable command
        if (rightHand.isValid && leftHand.isValid)
        {
            rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool rightAPressed);
            rightHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightBPressed);
            leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool leftAPressed);
            leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool leftBPressed);
            
            // Force enable with all buttons still requires all buttons
            if (rightAPressed && rightBPressed && leftAPressed && leftBPressed)
            {
                forceEnabled = true;
                adminControlsEnabled = true;
                Debug.Log("Controls force-enabled via all buttons pressed.");
            }
            // Save rotation when either A button is pressed
            else if (rightAPressed || leftAPressed)
            {
                SaveCurrentRotation();
                Debug.Log("Scene rotation saved using A button");
            }
            // Restore rotation when either B button is pressed
            else if (rightBPressed || leftBPressed)
            {
                RestoreSavedRotation();
                Debug.Log("Scene rotation restored using B button");
            }
        }

        // Periodic controller check
        if (Time.time > nextControllerCheck)
        {
            nextControllerCheck = Time.time + controllerCheckInterval;
            
            if (!leftHand.isValid || !rightHand.isValid)
            {
                // Only log if there's no Facilitator in the scene
                if (FindAnyObjectByType<Facilitator>() == null)
                {
                    Debug.Log("Periodic check: Controllers not valid, attempting to detect...");
                    ForceControllerRefresh();
                }
            }
        }

        if (adminControlsEnabled || forceEnabled)
        {
            if (leftHand.isValid)
            {
                bool hasThumbstick = leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftThumbstickValue);
                
                if (hasThumbstick && leftThumbstickValue != Vector2.zero)
                {
                    float horizontalInput = leftThumbstickValue.x;
                    float scaledInput = Mathf.Sign(horizontalInput) * Mathf.Pow(Mathf.Abs(horizontalInput), exponentialFactor);
                    float rotationAmount = scaledInput * Time.deltaTime * maxRotationSpeed;
                    
                    // Only log when actually rotating
                    if (Mathf.Abs(rotationAmount) > 0.01f)
                    {
                        Debug.Log($"Rotating scene by {rotationAmount:F2} degrees (Input: {horizontalInput:F2})");
                    }
                    
                    RotateSceneAroundPointThumbstick(rootTransform.position, rotationAmount);
                }
            }

            // Use right grip for disabling controls
            if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool rightGripPressed))
            {
                if (rightGripPressed) 
                {
                    adminControlsEnabled = false;
                    forceEnabled = false;
                    Debug.Log("Controls disabled via right grip button");
                }
            }
        }
    }

    void RotateSceneAroundPointThumbstick(Vector3 pivot, float rotationAmount)
    {
        if (rootTransform == null)
        {
            Debug.LogError("Root transform is not assigned!");
            return;
        }

        // Check if XR Origin is found
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin not found! Please assign it in the inspector or ensure it exists in the scene.");
            return;
        }

        if (cameraOffset == null)
        {
            Debug.LogWarning("Camera Offset not found! Scene rotation might not work correctly.");
            // Continue anyway, just using XR Origin
        }

        // Store the original position of the XR Origin and Camera Offset
        Vector3 originalXROriginPos = xrOrigin.position;
        Vector3 originalCameraOffsetPos = cameraOffset != null ? cameraOffset.position : xrOrigin.position;
        Quaternion originalXROriginRot = xrOrigin.rotation;
        
        // Rotate the scene
        rootTransform.RotateAround(pivot, Vector3.up, rotationAmount);
        Debug.Log($"Scene rotated around {pivot} by {rotationAmount} degrees");
        
        // Counter-rotate the XR Origin to keep it aligned with the real world
        xrOrigin.position = originalXROriginPos;
        xrOrigin.rotation = originalXROriginRot;
        
        // Ensure camera offset maintains its position if it exists
        if (cameraOffset != null)
        {
            cameraOffset.position = originalCameraOffsetPos;
        }

        Debug.Log("Scene rotated " + rotationAmount + " degrees around pivot point.");
        if (events != null)
        {
            events.Log("Scene rotated " + rotationAmount + " degrees around pivot point.");
        }
    }

    void SaveCurrentRotation()
    {
        if (rootTransform == null) return;
        savedRotation = rootTransform.rotation;
        Debug.Log("Rotation saved: " + savedRotation.eulerAngles);
        
        // Update the avatar in the networked scene
        var avatarManager = AvatarManager.Find(this);
        if (avatarManager != null)
        {
            // Store the current prefab
            var currentPrefab = avatarManager.avatarPrefab;
            
            // Temporarily set to null to trigger despawn
            avatarManager.avatarPrefab = null;
            
            // Set back to original prefab to trigger respawn
            avatarManager.avatarPrefab = currentPrefab;
            
            Debug.Log("Avatar respawned in networked scene");
        }
        else
        {
            Debug.LogWarning("AvatarManager not found in scene");
        }
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

    IEnumerator WaitForControllerFeatures(InputDevice device, bool isLeft)
    {
        int attempts = 0;
        bool featuresReady = false;
        
        while (attempts < 5 && !featuresReady && device.isValid)
        {
            if (isLeft)
            {
                featuresReady = device.TryGetFeatureValue(CommonUsages.primary2DAxis, out _);
            }
            else
            {
                featuresReady = device.TryGetFeatureValue(CommonUsages.primaryButton, out _);
            }
            
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

    void OnDeviceDisconnected(InputDevice device)
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
}

