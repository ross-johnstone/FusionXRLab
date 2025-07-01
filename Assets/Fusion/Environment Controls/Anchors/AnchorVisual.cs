using UnityEngine;

public class AnchorVisual : MonoBehaviour
{
    private AnchorPlacer anchorPlacer;
    private AnchorAlignmentManager anchorAlignmentManager;

    void Start()
    {
        anchorPlacer = FindFirstObjectByType<AnchorPlacer>();
        anchorAlignmentManager = FindFirstObjectByType<AnchorAlignmentManager>();
    }

    public void HideVisuals()
    {
        if (anchorPlacer == null)
        {
            anchorPlacer = FindFirstObjectByType<AnchorPlacer>();
        }

        var anchors = anchorPlacer.getAnchorTransforms();
        foreach (var anchor in anchors)
        {
            if (anchor != null)
            {
                // Hide all visual components
                var renderers = anchor.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = false;
                }

                var canvases = anchor.GetComponentsInChildren<Canvas>();
                foreach (var canvas in canvases)
                {
                    canvas.enabled = false;
                }
            }
        }

        // Hide root reference objects
        if (anchorAlignmentManager != null)
        {
            var rootPosition = anchorAlignmentManager.transform.Find("rootPosition");
            var rootDirection = anchorAlignmentManager.transform.Find("rootDirection");
            var rootAngle = anchorAlignmentManager.transform.Find("rootAngle");

            if (rootPosition != null)
            {
                var renderer = rootPosition.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = false;
            }

            if (rootDirection != null)
            {
                var renderer = rootDirection.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = false;
            }

            if (rootAngle != null)
            {
                var renderer = rootAngle.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = false;
            }
        }
    }

    public void ShowVisuals()
    {
        var anchors = anchorPlacer.getAnchorTransforms();
        foreach (var anchor in anchors)
        {
            if (anchor != null)
            {
                // Show all visual components
                var renderers = anchor.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    renderer.enabled = true;
                }
            }

            var canvases = anchor.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.enabled = true;
            }
        }

        // Show root reference objects
        if (anchorAlignmentManager != null)
        {
            var rootPosition = anchorAlignmentManager.transform.Find("rootPosition");
            var rootDirection = anchorAlignmentManager.transform.Find("rootDirection");
            var rootAngle = anchorAlignmentManager.transform.Find("rootAngle");

            if (rootPosition != null)
            {
                var renderer = rootPosition.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = true;
            }

            if (rootDirection != null)
            {
                var renderer = rootDirection.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = true;
            }

            if (rootAngle != null)
            {
                var renderer = rootAngle.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = true;
            }
        }
    }
} 