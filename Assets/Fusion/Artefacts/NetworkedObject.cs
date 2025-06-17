using UnityEngine;
using Ubiq.Messaging;

public class NetworkedObject : MonoBehaviour
{
    private NetworkContext context;
    public NetworkId Id { get; private set; }

    [Tooltip("Set to true if this instance should control the object")]
    public bool isOwner = false;

    private void Start()
    {
        context = NetworkScene.Register(this);
        Id = NetworkId.Unique();
    }

    private void Update()
    {
        // Send transform only if we're the owner and context is valid
        if (isOwner && context.Id.Valid)
        {
            var msg = new TransformMessage(transform.position, transform.rotation);
            context.SendJson(msg);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        if (!isOwner) // Only apply remote messages if not owner
        {
            var msg = message.FromJson<TransformMessage>();
            transform.position = msg.position;
            transform.rotation = msg.rotation;
        }
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
}
