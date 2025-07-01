using System.Collections.Generic;
using Ubiq.Spawning;
using UnityEngine;

public class AnchorPlacer : MonoBehaviour
{
    public NetworkSpawnManager networkSpawnManager;
    public GameObject anchorPrefab;
    public GameObject previewAnchorPrefab;

    private GameObject previewAnchor;
    private Stack<GameObject> anchorStack = new Stack<GameObject>();
    private bool isPreviewActive = false;
    private Vector3 pendingAnchorPosition;
    private Quaternion pendingAnchorRotation;

    void Start()
    {
        // Create preview object but keep it inactive
        previewAnchor = Instantiate(previewAnchorPrefab);
        previewAnchor.SetActive(false);

        // Make preview semi-transparent
        var renderers = previewAnchor.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.materials)
            {
                var color = material.color;
                color.a = 0.5f;
                material.color = color;
            }
        }

        // Subscribe to OnSpawned event for room-scoped anchors
        networkSpawnManager.OnSpawned.AddListener(OnAnchorSpawned);
    }

    public void ShowPreview(Vector3 position, Quaternion rotation)
    {
        if (!isPreviewActive)
        {
            previewAnchor.SetActive(true);
            isPreviewActive = true;
        }

        previewAnchor.transform.SetPositionAndRotation(position, rotation);
    }

    public void HidePreview()
    {
        if (isPreviewActive)
        {
            previewAnchor.SetActive(false);
            isPreviewActive = false;
        }
    }

    public void PlaceAnchor(Vector3 position, Quaternion rotation)
    {
        // Store the desired transform for the next anchor
        pendingAnchorPosition = position;
        pendingAnchorRotation = rotation;

        // Request a room-scoped spawn
        networkSpawnManager.SpawnWithRoomScope(anchorPrefab);

        HidePreview();
    }

    public List<Transform> getAnchorTransforms()
    {
        var list = new List<Transform>();
        foreach (var anchor in anchorStack)
        {
            list.Add(anchor.transform);
        }
        return list;
    }

    public List<GameObject> getAnchors()
    {
        return new List<GameObject>(anchorStack);
    }

    public void DeleteLastAnchor()
    {
        if (anchorStack.Count > 0)
        {
            var last = anchorStack.Pop();
            var networkedAnchor = last.GetComponent<NetworkedAnchor>();
            if (networkedAnchor != null)
            {
                //networkedAnchor.DestroyNetworkedAnchor();
                networkSpawnManager.Despawn(last);
            }
            else
            {
                Debug.LogWarning("[AnchorPlacer] Last anchor does not have a NetworkedAnchor component.");
            }
            Destroy(last);
        }
    }

    private void OnAnchorSpawned(GameObject obj, Ubiq.Rooms.IRoom room, Ubiq.Rooms.IPeer peer, Ubiq.Spawning.NetworkSpawnOrigin origin)
    {
        // Only handle room-scoped spawns and only for objects with the AnchorVisual tag
        if (room != null && obj.CompareTag("AnchorVisual"))
        {
            obj.transform.SetPositionAndRotation(pendingAnchorPosition, pendingAnchorRotation);
            anchorStack.Push(obj);
        }
    }
}
