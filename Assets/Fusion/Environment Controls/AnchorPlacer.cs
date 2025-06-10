using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class AnchorPlacer : MonoBehaviour
{
    public GameObject anchorPrefab;

    private Stack<GameObject> anchorStack = new Stack<GameObject>();

    

    public void PlaceAnchor(Vector3 position, Quaternion rotation)
    {
        anchorStack.Push(Instantiate(anchorPrefab, position, rotation));
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
