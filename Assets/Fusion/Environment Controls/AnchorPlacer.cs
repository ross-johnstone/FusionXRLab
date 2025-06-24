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
        // Spawn the prefab *via* NetworkSpawnManager, not Instantiate
        var anchor = networkSpawnManager.SpawnWithPeerScope(anchorPrefab);

        if (anchor != null)
        {
            anchor.transform.SetPositionAndRotation(position, rotation);
            anchorStack.Push(anchor);
        }
        else
        {
            Debug.LogWarning("[AnchorPlacer] Failed to spawn networked anchor.");
        }

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
            Destroy(last);
        }
    }
}
