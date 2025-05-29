using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class RoomAlignmentManager : MonoBehaviour
{
    [SerializeField] private Transform worldContainer; // Parent object containing all world objects
    [SerializeField] private Transform squareMarker; // Helper object for alignment
    [SerializeField] private Transform point1, point2, point3, point4; // Corner points for alignment

    private Vector3[] boundaryPoints;
    private bool isConfigured = false;
    private Vector3 storedPosition;
    private Quaternion storedRotation;
    private bool hasStoredTransform = false;

    void Start()
    {
        // Initialize boundary points array
        boundaryPoints = new Vector3[4];
        
        // Subscribe to OVRManager events
        OVRManager.HMDMounted += OnHMDMounted;
        OVRManager.HMDUnmounted += OnHMDUnmounted;
        
        // Initial alignment
        UpdateRoomAlignment();
    }

    void OnDestroy()
    {
        // Unsubscribe from OVRManager events
        OVRManager.HMDMounted -= OnHMDMounted;
        OVRManager.HMDUnmounted -= OnHMDUnmounted;
    }

    private void OnHMDMounted()
    {
        // Re-align when HMD is mounted (e.g., after sleep/wake)
        UpdateRoomAlignment();
    }

    private void OnHMDUnmounted()
    {
        // Optional: Handle unmounting if needed
    }

    public void UpdateRoomAlignment()
    {
        #if !UNITY_EDITOR
        if (OVRManager.boundary.GetConfigured())
        {
            // Get the play area boundary points
            boundaryPoints = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            
            if (boundaryPoints.Length >= 4)
            {
                CenterRoom();
                isConfigured = true;
            }
        }
        #endif
    }

    private void CenterRoom()
    {
        // Convert boundary points to local space
        point1.transform.localPosition = boundaryPoints[0];
        point2.transform.localPosition = boundaryPoints[1];
        point3.transform.localPosition = boundaryPoints[2];
        point4.transform.localPosition = boundaryPoints[3];

        // Calculate center points of opposite sides
        Vector3 pointA = MidPoint(point1.transform.position, point2.transform.position);
        Vector3 pointB = MidPoint(point3.transform.position, point4.transform.position);

        // Calculate the direction vector between points
        Vector3 between = pointB - pointA;

        // Position the square marker at the center
        squareMarker.transform.position = pointA + (between / 2.0f);
        squareMarker.transform.LookAt(pointB);

        // Apply the alignment to the world container
        worldContainer.transform.position = squareMarker.transform.position;
        worldContainer.transform.rotation = squareMarker.transform.rotation;
    }

    private Vector3 MidPoint(Vector3 a, Vector3 b)
    {
        return new Vector3(
            (a.x + b.x) / 2,
            (a.y + b.y) / 2,
            (a.z + b.z) / 2
        );
    }

    // Call this method when you need to force a re-alignment
    public void ForceRealignment()
    {
        UpdateRoomAlignment();
    }

    public void StoreCurrentTransform()
    {
        if (worldContainer != null)
        {
            storedPosition = worldContainer.position;
            storedRotation = worldContainer.rotation;
            hasStoredTransform = true;
            Debug.Log("[RoomAlignmentManager] Stored room transform");
        }
    }

    public void RestoreStoredTransform()
    {
        if (worldContainer != null && hasStoredTransform)
        {
            worldContainer.position = storedPosition;
            worldContainer.rotation = storedRotation;
            Debug.Log("[RoomAlignmentManager] Restored room transform");
        }
    }

    public bool IsConfigured()
    {
        return isConfigured;
    }
} 