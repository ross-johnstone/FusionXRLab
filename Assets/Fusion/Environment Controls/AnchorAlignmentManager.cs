using UnityEngine;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR;
using System.Linq;

public class AnchorAlignmentManager : MonoBehaviour
{
    public static AnchorAlignmentManager Instance { get; private set; }
    
    [SerializeField] private Transform environmentRoot; // Root transform of the virtual environment
    [SerializeField] private Vector3 environmentRootPosition; // Original position of the virtual environment
    [SerializeField] private Vector3 environmentRootRotation; // Original rotation of the virtual environment
    [SerializeField] private Transform rootPosition;
    [SerializeField] private Transform rootAngle;
    [SerializeField] private Transform rootDirection;
    private ComponentLogEmitter events;
    private bool isAligned = false;
    private bool start = true;

    void Start()
    {
        environmentRootPosition = environmentRoot.position;
        environmentRootRotation = environmentRoot.rotation.eulerAngles;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            events = new ComponentLogEmitter(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsAligned()
    {
        return isAligned;
    }

    public void ResetAlignment()
    {
        isAligned = false;
        events.Log("[AnchorAlignmentManager] Alignment reset");
    }

    public void ResetEnvironment()
    {
        // Store original parent (Anchors object)
        Transform originalParent = rootPosition.parent;

        // Store world positions before parenting
        Vector3 rootPosWorld = rootPosition.position;
        Vector3 rootAngleWorld = rootAngle.position;
        Vector3 rootDirWorld = rootDirection.position;

        // Parent anchor references to environment root
        rootPosition.SetParent(environmentRoot);
        rootAngle.SetParent(environmentRoot);
        rootDirection.SetParent(environmentRoot);

        // Reset environment position and rotation
        environmentRoot.position = environmentRootPosition;
        environmentRoot.rotation = Quaternion.Euler(environmentRootRotation);

        // Restore original parent while maintaining world positions
        rootPosition.SetParent(originalParent);
        rootAngle.SetParent(originalParent);
        rootDirection.SetParent(originalParent);

        // Restore world positions
        rootPosition.position = rootPosWorld;
        rootAngle.position = rootAngleWorld;
        rootDirection.position = rootDirWorld;

        events.Log("[AnchorAlignmentManager] Environment reset");
    }
    public void RelocateRootAnchors(List<Transform> anchors)
    {
        // Get the three anchor positions adapted from aurelien scripts
        // reperePos = rootPosition
        // repereAngle = rootAngle
        // repereDir = rootDirection
        // vecteurRepere = rootVector
        // vecteurAnchor = anchorVector   

        // Validate we have enough anchors
        if (anchors == null || anchors.Count < 2)
        {
            Debug.LogWarning("[AnchorAlignmentManager] Not enough anchors to relocate root. Need at least 2 anchors.");
            return;
        }

        environmentRoot.SetParent(rootPosition);

        // Calculate the new position of the root
        Vector3 newPosition = new Vector3(
            anchors.Average(t => t.position.x),
            anchors.Average(t => t.position.y),
            anchors.Average(t => t.position.z)
        );

        // Keep the y position from current rootPosition to avoid vertical snapping
        rootPosition.position = new Vector3(newPosition.x, rootPosition.position.y, newPosition.z);

        // Calculate the vectors for the root and the anchors
        Vector3 rootVector = rootPosition.position - rootAngle.position;
        Vector3 anchorVector = anchors[1].position - anchors[0].position;

        // Calculate the angle between rootVector and anchorVector
        float newAngle = Vector3.Angle(rootVector, anchorVector);

        // Update the rotation of the rootPosition to align with the anchorVector
        rootPosition.localRotation = Quaternion.FromToRotation(rootVector, anchorVector) * rootPosition.localRotation;

        // Lock rotation only around Y axis
        rootPosition.localEulerAngles = new Vector3(0, rootPosition.localEulerAngles.y, 0);

        // Adjust the initial rotation on the first run
        if (start)
        {
            start = false;

            // Only proceed with angle calculations if we have 3 anchors
            if (anchors.Count >= 3)
            {
                float rAngle = Vector2.SignedAngle(
                    new Vector2(rootAngle.position.x - anchors[0].position.x, rootAngle.position.z - anchors[0].position.z),
                    new Vector2(anchorVector.x, anchorVector.z)
                );

                Debug.Log("[AnchorAlignmentManager] Root angle: " + rAngle);

                float aAngle = Vector2.SignedAngle(
                    new Vector2(anchors[2].position.x - anchors[0].position.x, anchors[2].position.z - rootAngle.position.z),
                    new Vector2(anchorVector.x, anchorVector.z)
                );

                Debug.Log("[AnchorAlignmentManager] Anchor angle: " + aAngle);

                // Invert rootAngle position if necessary
                if ((rAngle > 0 && aAngle < 0) || (rAngle < 0 && aAngle > 0))
                {
                    rootAngle.localPosition = -rootAngle.localPosition;
                    rootVector = rootPosition.position - rootAngle.position;
                    Debug.Log("[AnchorAlignmentManager] Root vector: " + rootVector);
                }

                rootPosition.localRotation = Quaternion.FromToRotation(rootVector, anchorVector) * rootPosition.localRotation;
                rootPosition.localEulerAngles = new Vector3(0, rootPosition.localEulerAngles.y, 0);
            }
        }
    }



} 