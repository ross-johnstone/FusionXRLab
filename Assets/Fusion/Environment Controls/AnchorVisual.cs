using UnityEngine;

public class AnchorVisual : MonoBehaviour
{
    private AnchorPlacer anchorPlacer;

    void Start()
    {
        anchorPlacer = FindFirstObjectByType<AnchorPlacer>();
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
    }

} 