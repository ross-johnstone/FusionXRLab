using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using Ubiq.Logging;

public class SpatialAnchorManager : MonoBehaviour
{
    public static SpatialAnchorManager Instance { get; private set; }
    public Transform anchorPrefab; // Assign a visual representation of the anchor in the inspector
    private Transform currentAnchor;
    private ExperimentLogEmitter events;
    private Vector3 anchorPosition;
    private bool anchorPlaced = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            events = new ExperimentLogEmitter(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaceAnchor(Vector3 position)
    {
        if (currentAnchor != null)
        {
            Destroy(currentAnchor.gameObject);
        }

        if (anchorPrefab != null)
        {
            currentAnchor = Instantiate(anchorPrefab, position, Quaternion.identity);
            anchorPosition = position;
            anchorPlaced = true;
            events.Log($"Spatial anchor placed at position: {position}");
        }
        else
        {
            Debug.LogError("Anchor prefab not assigned!");
        }
    }

    public Vector3 GetAnchorPosition()
    {
        return anchorPosition;
    }

    public bool IsAnchorPlaced()
    {
        return anchorPlaced;
    }

    public void ClearAnchor()
    {
        if (currentAnchor != null)
        {
            Destroy(currentAnchor.gameObject);
            currentAnchor = null;
        }
        anchorPlaced = false;
        events.Log("Spatial anchor cleared");
    }
} 