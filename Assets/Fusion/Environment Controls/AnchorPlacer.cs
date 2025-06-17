using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class AnchorPlacer : MonoBehaviour
{
    public GameObject anchorPrefab;
    private GameObject previewAnchor;
    private Stack<GameObject> anchorStack = new Stack<GameObject>();
    private bool isPreviewActive = false;

    void Start()
    {
        // Create preview object but keep it inactive
        previewAnchor = Instantiate(anchorPrefab);
        previewAnchor.SetActive(false);
        
        // Make preview semi-transparent
        var renderers = previewAnchor.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            var materials = renderer.materials;
            foreach (var material in materials)
            {
                Color color = material.color;
                color.a = 0.5f;
                material.color = color;
            }
            renderer.materials = materials;
        }
    }

    public void ShowPreview(Vector3 position, Quaternion rotation)
    {
        if (!isPreviewActive)
        {
            previewAnchor.SetActive(true);
            isPreviewActive = true;
        }
        previewAnchor.transform.position = position;
        previewAnchor.transform.rotation = rotation;
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
        anchorStack.Push(Instantiate(anchorPrefab, position, rotation));
        HidePreview();
    }

    public List<Transform> getAnchorTransforms()
    {
        List<Transform> anchors = new List<Transform>();
        foreach (GameObject anchor in anchorStack)
        {
            anchors.Add(anchor.transform);
        }
        return anchors;
    }

    public List<GameObject> getAnchors()
    {
        return new List<GameObject>(anchorStack);
    }

    public void DeleteLastAnchor()
    {
        if (anchorStack.Count > 0)
        {
            Destroy(anchorStack.Pop());
        }
    }
}
