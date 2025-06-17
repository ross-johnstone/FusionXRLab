using UnityEngine;
using Ubiq.Messaging;

public class NetworkedObject : MonoBehaviour
{
    private NetworkContext context;
    public NetworkId Id { get; private set; }

    public bool isOwner = false;

    private Rigidbody rb;

    private void Start()
    {
        context = NetworkScene.Register(this);
        Id = NetworkId.Unique();

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (isOwner && context.Id.Valid)
        {
            var msg = new TransformMessage(transform.position, transform.rotation);
            context.SendJson(msg);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var json = message.ToString();

        if (json.Contains("position")) // crude but functional
        {
            var msg = JsonUtility.FromJson<TransformMessage>(json);
            if (!isOwner)
            {
                transform.position = msg.position;
                transform.rotation = msg.rotation;
            }
        }
        else if (json.Contains("isOwner"))
        {
            var msg = JsonUtility.FromJson<OwnershipMessage>(json);
            isOwner = msg.isOwner;

            if (rb != null)
            {
                rb.useGravity = !isOwner;
                rb.isKinematic = isOwner;
            }
        }
    }

    public void SetOwner(bool owner)
    {
        isOwner = owner;

        if (rb != null)
        {
            rb.useGravity = !owner;
            rb.isKinematic = owner;
        }

        var msg = new OwnershipMessage { isOwner = owner };
        context.SendJson(msg);
    }

    [System.Serializable]
    private struct TransformMessage
    {
        public Vector3 position;
        public Quaternion rotation;

        public TransformMessage(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }

    [System.Serializable]
    private struct OwnershipMessage
    {
        public bool isOwner;
    }
}
