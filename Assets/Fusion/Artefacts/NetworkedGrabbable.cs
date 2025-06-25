using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.Geometry;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

#if XRI_3_0_7_OR_NEWER
using UnityEngine.XR.Interaction.Toolkit;
#endif

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class NetworkedGrabbableWithVelocity : MonoBehaviour, INetworkSpawnable
{
    public NetworkId NetworkId { get; set; }

    private static string localPeerId = System.Guid.NewGuid().ToString();
    private NetworkContext context;
    private bool isOwner;

    private XRGrabInteractable grab;
    private Rigidbody rb;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 targetVelocity;

    private void Awake()
    {
        // Set position/rotation from spawner static variables if set
        if (Spawner.PendingPosition != Vector3.zero)
        {
            transform.position = Spawner.PendingPosition;
            transform.rotation = Spawner.PendingRotation;
            Spawner.PendingPosition = Vector3.zero;
        }
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        targetPosition = transform.position;
        targetRotation = transform.rotation;
        targetVelocity = Vector3.zero;
    }

    private void Start()
    {
        context = NetworkScene.Register(this);

        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);

        // XR handles tracking nicely
        grab.trackPosition = true;
        grab.trackRotation = true;

        rb.isKinematic = false;
    }

    private void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isOwner = true;
        rb.isKinematic = true; // Freeze physics; follow hand
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isOwner = true;
        rb.isKinematic = false; // Enable physics
    }

    private void FixedUpdate()
    {
        if (isOwner)
        {
            // Broadcast current state
            SendMessage();

            // Record for next snapshot
            targetVelocity = rb.linearVelocity;
        }
        else
        {
            // Simulate remote physics
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, 0.2f);
            transform.position = Vector3.Lerp(transform.position, targetPosition, 0.2f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.2f);
        }
    }

    private struct Message
    {
        public Pose pose;
        public Vector3 velocity;
        public string ownerId;
    }

    private void SendMessage()
    {
        var message = new Message
        {
            pose = Transforms.ToLocal(transform, context.Scene.transform),
            velocity = rb.linearVelocity,
            ownerId = localPeerId
        };
        context.SendJson(message);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage msgSrc)
    {
        var msg = msgSrc.FromJson<Message>();
        var worldPose = Transforms.ToWorld(msg.pose, context.Scene.transform);

        // Ownership logic
        isOwner = (msg.ownerId == localPeerId);

        // Immediately set position, rotation, and velocity
        transform.position = worldPose.position;
        transform.rotation = worldPose.rotation;
        rb.linearVelocity = msg.velocity;

        // Also update targets for interpolation
        targetPosition = worldPose.position;
        targetRotation = worldPose.rotation;
        targetVelocity = msg.velocity;
    }

    public void SetOwner(bool owner)
    {
        isOwner = owner;
        rb.isKinematic = owner;
    }

    public void ForceNetworkUpdate()
    {
        SendMessage();
    }
}
