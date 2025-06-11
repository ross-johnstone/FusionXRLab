using UnityEngine;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine.XR;
using System.Linq;

public class AnchorAlignmentManager : MonoBehaviour
{
    public static AnchorAlignmentManager Instance { get; private set; }
    
    [SerializeField] private Transform environmentRoot; // Root transform of the virtual environment
    [SerializeField] private Vector3 environmentRootPosition; // Original position of the virtual environment
    [SerializeField] private Vector3 environmentRootRotation; // Original rotation of the virtual environment
    [SerializeField] private Transform rootPosition;
    [SerializeField] private Transform rootAngle;
    private ComponentLogEmitter events;
    private bool isAligned = false;
    private bool start = true;

    void Start()
    {
        environmentRootPosition = environmentRoot.position;
        environmentRootRotation = environmentRoot.rotation.eulerAngles;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            events = new ComponentLogEmitter(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AlignEnvironment(List<Transform> anchors)
    {
        if (anchors.Count != 3 || environmentRoot == null) return;

        // Get the three anchor positions
        Vector3 anchor1 = anchors[0].position;
        Vector3 anchor2 = anchors[1].position;
        Vector3 anchor3 = anchors[2].position;

        // Calculate the forward direction (from anchor1 to anchor2)
        Vector3 forward = (anchor2 - anchor1).normalized;
        
        // Calculate the right direction using anchor3
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        
        // Recalculate up to ensure orthogonality
        Vector3 up = Vector3.Cross(right, forward).normalized;

        // Create the rotation matrix
        Quaternion rotation = Quaternion.LookRotation(forward, up);

        // Apply the transformation
        environmentRoot.position = anchor1;
        environmentRoot.rotation = rotation;

        isAligned = true;
        events.Log("[AnchorAlignmentManager] Environment alignment completed");

        Debug.Log("[AnchorAlignmentManager] Environment aligned");
    }

    public bool IsAligned()
    {
        return isAligned;
    }

    public void ResetAlignment()
    {
        isAligned = false;
        events.Log("[AnchorAlignmentManager] Alignment reset");
    }

    public void ResetEnvironment()
    {
        environmentRoot.position = environmentRootPosition;
        environmentRoot.rotation = Quaternion.Euler(environmentRootRotation);
        events.Log("[AnchorAlignmentManager] Environment reset");
    }

    public void RelocateRepere(List<Transform> anchors)
    {
        // Si le client et le serveur ne sont pas actifs, ne rien faire
        //if (!networkManager.IsClient &&  !networkManager.IsServer)
        //    return;

        // GetRelocateObjectsList();
        // AssigneGameObj();

        if (anchors.Count <= 0) return; 

        // Calculer la nouvelle position du repère
        Vector3 newPosition = new Vector3(anchors.Average(t => t.position.x), anchors.Average(t => t.position.y), anchors.Average(t => t.position.z));
        rootPosition.position = new Vector3(newPosition.x, rootPosition.position.y, newPosition.z);

        // Calculer les vecteurs pour le repère et les ancres
        Vector3 rootVector = rootPosition.position - rootAngle.position;
        Vector3 anchorVector = anchors[1].position - anchors[0].position;

        // Calculer le nouvel angle entre le repère et les ancres
        //float direction = rootVector - anchorVector;


        //reperePos.Rotate(Vector3.up, newAngle);

        //float AngleSigne = Vector3.Angle(anchorsList[2].position - reperePos.position, rootVector);
        float newAngle = Vector3.Angle(rootVector, anchorVector);

        //Debug.Log("Angle newAngle: " + newAngle);

        // Mettre à jour la rotation du repère
        rootPosition.localRotation = Quaternion.FromToRotation(rootVector, anchorVector) * rootPosition.localRotation;
        rootPosition.localEulerAngles = new Vector3(0, rootPosition.localEulerAngles.y, 0);

        // Ajuster la rotation initiale au démarrage
        if (start)
        {
            start = false;
            float rAngle = Vector2.SignedAngle(new Vector2(rootAngle.position.x - anchors[0].position.x, rootAngle.position.z - anchors[0].position.z), new Vector2(anchorVector.x, anchorVector.z));
            float aAngle = Vector2.SignedAngle(new Vector2(anchors[2].position.x - anchors[0].position.x, anchors[2].position.z - rootAngle.position.z), new Vector2(anchorVector.x, anchorVector.z));

            //Debug.Log("Angle rAngle: " + rAngle);
            //Debug.Log("Angle aAngle: " + aAngle);

            // Inverser la position du repère si nécessaire
            if (rAngle > 0 && aAngle < 0 || rAngle < 0 && aAngle > 0)
            {
                rootAngle.localPosition = -rootAngle.localPosition;
                rootVector = rootPosition.position - rootAngle.position;
            }
            rootPosition.localRotation = Quaternion.FromToRotation(rootVector, anchorVector) * rootPosition.localRotation;
            rootPosition.localEulerAngles = new Vector3(0, rootPosition.localEulerAngles.y, 0);
        }

    }
} 