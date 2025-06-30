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

    private float ownershipCooldown = 0f;
    private const float OwnershipCooldownDuration = 0.2f; // 200ms cooldown

    private void Awake()
    {
        // Set position/rotation from spawner static variables if set
        if (Spawner.PendingPosition != Vector3.zero)
        {
            transform.position = Spawner.PendingPosition;
            //transform.rotation = Spawner.PendingRotation; //TEST
            Spawner.PendingPosition = Vector3.zero;
        }
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false; // Always start as non-kinematic
        grab.enabled = true;

        // Minimal setup for dynamic attach in XRITK 3.0.7
        grab.useDynamicAttach = true;

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

        StartCoroutine(EnableGrabAfterDelay());
    }

    private System.Collections.IEnumerator EnableGrabAfterDelay()
    {
        grab.enabled = false;
        yield return new WaitForSeconds(0.2f); // Wait a short time
        grab.enabled = true;
        //Debug.Log($"[{name}] Grab enabled after delay");
    }

    private void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void Update()
    {
        if (ownershipCooldown > 0f)
        {
            ownershipCooldown -= Time.deltaTime;
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isOwner = true;
        rb.isKinematic = true;
        ownershipCooldown = OwnershipCooldownDuration;
        StartCoroutine(ForceNetworkUpdateNextFrame());
    }

    private System.Collections.IEnumerator ForceNetworkUpdateNextFrame()
    {
        yield return null; // Wait one frame so XR system moves the object
        ForceNetworkUpdate();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isOwner = true;
        rb.isKinematic = false; // Enable physics
        ForceNetworkUpdate();
    }

    private void FixedUpdate()
    {
        if (isOwner)
        {
            //Debug.Log($"[{name}] {localPeerId} sending update (owner)");
            SendMessage();
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
        //Debug.Log($"[{name}] Received message from ownerId={msg.ownerId}, localPeerId={localPeerId}, isOwner={isOwner}");
        var worldPose = Transforms.ToWorld(msg.pose, context.Scene.transform);

        bool wasOwner = isOwner;
        // Only allow ownership change if not in cooldown
        if (ownershipCooldown <= 0f)
        {
            isOwner = (msg.ownerId == localPeerId);
            if (wasOwner != isOwner)
            {
                //Debug.Log($"[{name}] Ownership changed: now isOwner={isOwner} (msg.ownerId={msg.ownerId}, localPeerId={localPeerId})");
                // If we lost ownership and are still grabbing, force release
                if (!isOwner && wasOwner && grab.isSelected && grab.firstInteractorSelecting != null)
                {
                    //Debug.Log($"[{name}] Forcing local release due to ownership transfer");
                    grab.interactionManager.SelectExit(grab.firstInteractorSelecting, grab);
                }
            }
        }
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
        //Debug.Log($"[{name}] SetOwner({owner}) called by local peer {localPeerId}");
    }

    public void ForceNetworkUpdate()
    {
        SendMessage();
    }
}
