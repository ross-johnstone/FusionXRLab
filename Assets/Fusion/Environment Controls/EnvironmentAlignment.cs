//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class EnvironmentAlignment : MonoBehaviour
//{
//    private List<OVRSpatialAnchor> anchors = new List<OVRSpatialAnchor>();
//    private Transform[] anchorTransforms;
//    private GameObject[] sceneryObjects;
//    public Transform rootTransform;

//    private int previousAnchorCount = 0;

//    public bool allowAutoAlign = true;


//    void Start()
//    {

//        anchors = getAllOvrSpatialAnchors();
//        sceneryObjects = getSceneryObjects();

//        if (anchors != null)
//        {
//            logAllAnchorPositions();
//            if (anchors.Count == 3)
//            {
//                AlignSceneToAnchors();
//            }
//        }
//        else
//        {
//            Debug.Log("No anchors found.");
//        }

//    }

//    private bool hasAligned = false;

//    void Update()
//    {
//        anchors = getAllOvrSpatialAnchors();

//        // Log message only when anchor count changes
//        if (anchors.Count != previousAnchorCount)
//        {
//            previousAnchorCount = anchors.Count;

//            if (anchors.Count < 3)
//            {
//                Debug.Log("Three anchors must be defined in the scene for environment alignment.");
//                hasAligned = false; // reset if anchors change
//            }
//            else
//            {
//                Debug.Log("Three anchors detected.");
//            }
//        }

//        // Align only once when 3 anchors are available
//        if (allowAutoAlign && anchors.Count == 3 && !hasAligned)
//        {
//            anchorTransforms = getAllAnchorTransforms();

//            if (anchorTransforms != null)
//            {
//                AlignSceneToAnchors();
//                hasAligned = true;
//            }
//        }
//    }


//    void AlignSceneToAnchors()
//    {
//        if (anchorTransforms.Length < 3)
//        {
//            Debug.LogWarning("Need exactly 3 anchors to perform full alignment.");
//            return;
//        }

//        Transform a = anchorTransforms[0];
//        Transform b = anchorTransforms[1];
//        Transform c = anchorTransforms[2];

//        // Use anchor A as the origin
//        Vector3 origin = a.position;

//        // Compute plane normal (represents the physical floor's up direction)
//        Vector3 ab = b.position - a.position;
//        Vector3 ac = c.position - a.position;
//        Vector3 planeNormal = Vector3.Cross(ab, ac).normalized;

//        // OPTIONAL: Constrain normal to be as close to Vector3.up as possible
//        if (Vector3.Dot(planeNormal, Vector3.up) < 0)
//        {
//            planeNormal = -planeNormal; // Flip if upside-down
//        }

//        // Compute flat forward and right vectors (projected onto the floor plane)
//        Vector3 flatForward = Vector3.ProjectOnPlane((c.position - a.position), planeNormal).normalized;
//        Vector3 flatRight = Vector3.Cross(planeNormal, flatForward).normalized;

//        // Build rotation with real-world up (planeNormal)
//        Quaternion targetRotation = Quaternion.LookRotation(flatForward, planeNormal);

//        // Set root position using the plane’s origin (preserve current height)
//        Vector3 targetPosition = new Vector3(origin.x, origin.y, origin.z);

//        // Apply transform
//        rootTransform.SetPositionAndRotation(targetPosition, targetRotation);

//        Debug.Log("Scene aligned with gravity-based floor normal.");
//    }




//    public List<OVRSpatialAnchor> getAllOvrSpatialAnchors()
//    {
//        List<OVRSpatialAnchor> anchorList = FindObjectsByType<OVRSpatialAnchor>(FindObjectsSortMode.None).ToList();
//        return anchorList;
//    }

//    public Transform[] getAllAnchorTransforms()
//    {
//        List<OVRSpatialAnchor> anchors = getAllOvrSpatialAnchors();
//        Transform[] anchorTransforms = new Transform[anchors.Count];
//        for (int i = 0; i < anchors.Count; i++)
//        {
//            anchorTransforms[i] = anchors[i].transform;
//        }
//        return anchorTransforms;
//    }

//    private GameObject[] getSceneryObjects()
//    {
//        GameObject[] sceneryObjects = GameObject.FindGameObjectsWithTag("SceneObject");
//        return sceneryObjects;
//    }

//    private void logAllAnchorPositions()
//    {
//        List<OVRSpatialAnchor> anchors = getAllOvrSpatialAnchors();
//        foreach (OVRSpatialAnchor anchor in anchors)
//        {
//            Debug.Log("Anchor Position: " + anchor.transform.position);
//        }
//    }


//}
