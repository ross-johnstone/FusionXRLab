using System;
using UnityEngine;
using UnityEngine.XR;
using Unity.XR.Oculus;
using System.Collections.Generic;

public class BoundaryAligner : MonoBehaviour
{
    public Transform rootTransform; // Your VR scene root

    void Start()
    {
        AlignSceneToBoundary();

        InvokeRepeating("TryAlignSceneToPlayArea", 20f, 20f);
    }

    void Awake()
    {
        if (rootTransform == null)
        {
            rootTransform = transform; // Default to the GameObject this script is attached to
        }

        Debug.Log("BoundaryAligner Awake called. Root Transform: " + rootTransform.name);
    }

    void AlignSceneToBoundary()
    {
        OVRBoundary boundary = new OVRBoundary();

        try
        {
            Vector3[] boundaryPoints = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            Debug.Log("Boundary points count: " + boundaryPoints.Length);

            // GetBoundaryDimensions(Boundary.BoundaryType.PlayArea, out Vector3 dimensions);

            Debug.LogWarning("Not enough boundary points to align the scene.");



            if (boundaryPoints == null || boundaryPoints.Length < 3)
            {
                Debug.LogWarning("Not enough boundary points to align the scene.");
                return;
            }

            // Compute the center point of the boundary
            Vector3 center = Vector3.zero;
            foreach (var point in boundaryPoints)
            {
                center += point;
            }
            center /= boundaryPoints.Length;

            // Align scene's root to this position (on floor)
            rootTransform.position = new Vector3(center.x, 0f, center.z);

            // Optional: orient the scene to face the direction the user was facing when they set the boundary
            rootTransform.rotation = Quaternion.identity;

            Debug.Log("Scene aligned to Guardian boundary center.");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("An exception occurred while getting the boundary geometry: " + ex.Message);
        }
        
    }

    public void TryAlignSceneToPlayArea()
    {
        if (OVRManager.boundary == null)
        {
            Debug.LogWarning("OVRManager.boundary is not initialized.");
            return;
        }

        var boundaryPoints = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);

        if (boundaryPoints.Length < 3)
        {
            Debug.LogWarning("Boundary geometry unavailable or not enough points. Make sure Guardian is set up.");
            return;
        }

        // Compute the XZ center of boundary
        Vector3 center = Vector3.zero;
        foreach (var pt in boundaryPoints)
        {
            center += new Vector3(pt.x, 0, pt.z);
        }
        center /= boundaryPoints.Length;

        // Move scene root to center of boundary (XZ), keep current height
        rootTransform.position = new Vector3(center.x, rootTransform.position.y, center.z);
        Debug.Log("Scene aligned to Oculus Guardian boundary.");
    }
}
