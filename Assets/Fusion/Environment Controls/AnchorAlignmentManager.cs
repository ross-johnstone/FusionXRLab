using UnityEngine;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR;

public class AnchorAlignmentManager : MonoBehaviour
{
    public static AnchorAlignmentManager Instance { get; private set; }
    
    [SerializeField] private Transform environmentRoot; // Root transform of the virtual environment
    private ComponentLogEmitter events;
    private bool isAligned = false;

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

    public void AlignEnvironment(List<Transform> anchors)
    {
        if (anchors.Count != 3 || environmentRoot == null) return;

        // Get the three anchor positions
        Vector3 anchor1 = anchors[0].position;
        Vector3 anchor2 = anchors[1].position;
        Vector3 anchor3 = anchors[2].position;

        // Calculate the forward direction (from anchor1 to anchor2)
        Vector3 forward = (anchor2 - anchor1).normalized;
        
        // Calculate the right direction using anchor3
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        
        // Recalculate up to ensure orthogonality
        Vector3 up = Vector3.Cross(right, forward).normalized;

        // Create the rotation matrix
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        // Apply the transformation
        environmentRoot.position = anchor1;
        environmentRoot.rotation = rotation;

        isAligned = true;
        events.Log("Environment alignment completed");

        // Hide the anchor visuals
        //SpatialAnchorManager.Instance.HideAnchorVisuals();
        Debug.Log("Environment aligned");
    }

    public bool IsAligned()
    {
        return isAligned;
    }

    public void ResetAlignment()
    {
        isAligned = false;
        events.Log("Alignment reset");
    }
} 