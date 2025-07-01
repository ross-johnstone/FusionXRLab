using System.Collections.Generic;
using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Linq;

public class TestScript : MonoBehaviour
{
    public List<Transform> anchors = new List<Transform>();
    public Transform xrRig; // Facilitator XR Rig, assign in inspector

    private const int MaxAnchors = 3;
    private string anchorVisualName = "NetworkedAnchorVisual";
    private List<GameObject> trackedAnchorVisuals = new List<GameObject>();

    // Call this from the editor to align the scene view to the anchors
    public void RelocateRootAnchors(List<Transform> anchors)
    {
        if (anchors == null || anchors.Count < 3)
        {
            Debug.LogWarning("At least 3 anchors are required.");
            return;
        }

        // Example: Move the SceneView camera to look at the centroid of the first three anchors
#if UNITY_EDITOR
        Vector3 centroid = (anchors[0].position + anchors[1].position + anchors[2].position) / 3f;
        SceneView.lastActiveSceneView.pivot = centroid;
        SceneView.lastActiveSceneView.Repaint();
#endif
    }

    // Editor utility to find and assign the first three anchors in the scene
#if UNITY_EDITOR
    [ContextMenu("Find First 3 Anchors And Relocate")]
    private void FindAndRelocateAnchors()
    {
        anchors.Clear();
        var allAnchors = FindObjectsOfType<Transform>();
        int count = 0;
        foreach (var t in allAnchors)
        {
            if (t != this.transform && t.gameObject != null && t.gameObject.activeInHierarchy)
            {
                anchors.Add(t);
                count++;
                if (count == 3) break;
            }
        }
        RelocateRootAnchors(anchors);
    }
#endif

    void Update()
    {
        // Track newly spawned NetworkedAnchorVisuals (by name)
        var allAnchorVisuals = GameObject.FindGameObjectsWithTag("AnchorVisual");
        foreach (var go in allAnchorVisuals)
        {
            if (!trackedAnchorVisuals.Contains(go) && anchors.Count < MaxAnchors)
            {
                trackedAnchorVisuals.Add(go);
                anchors.Add(go.transform);
            }
        }

        // If not using tag, fallback to name search
        if (anchors.Count < MaxAnchors)
        {
            var allObjects = FindObjectsOfType<GameObject>();
            foreach (var go in allObjects)
            {
                if (go.name.Contains(anchorVisualName) && !trackedAnchorVisuals.Contains(go) && anchors.Count < MaxAnchors)
                {
                    trackedAnchorVisuals.Add(go);
                    anchors.Add(go.transform);
                }
            }
        }

        // Listen for 'R' key using the new Input System
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (anchors.Count >= 2 && AnchorAlignmentManager.Instance != null)
            {
                AnchorAlignmentManager.Instance.RelocateRootAnchors(anchors.AsEnumerable().Reverse().ToList());
#if UNITY_EDITOR
                // Optionally, move the SceneView to match VR users
                Vector3 centroid = Vector3.zero;
                foreach (var t in anchors) centroid += t.position;
                centroid /= anchors.Count;
                SceneView.lastActiveSceneView.pivot = centroid;
                SceneView.lastActiveSceneView.Repaint();
#endif
                // Move facilitator xrRig to centroid of anchors
                if (xrRig != null)
                {
                    Vector3 xrRigCentroid = Vector3.zero;
                    foreach (var t in anchors) xrRigCentroid += t.position;
                    xrRigCentroid /= anchors.Count;
                    xrRig.position = xrRigCentroid;
                }
            }
            else
            {
                Debug.LogWarning("Need at least 2 anchors and AnchorAlignmentManager.Instance to align.");
            }
        }
    }
}
