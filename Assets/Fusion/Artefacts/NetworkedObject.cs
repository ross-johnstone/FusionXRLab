using UnityEngine;
using Ubiq.Messaging;

public class NetworkedObject : MonoBehaviour
{
    private NetworkContext context;
    public NetworkId Id { get; private set; }

    private bool isOwner;

    private void Start()
    {
        // Register this object on the network
        context = NetworkScene.Register(this);
        Id = NetworkId.Unique();
        isOwner = true; // You can implement ownership logic if needed
    }

    private void Update()
    {
        // Only send if we're the owner
        if (isOwner)
        {
            var msg = new TransformMessage(transform.position, transform.rotation);
            context.SendJson(msg);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // Deserialize transform update
        var msg = message.FromJson<TransformMessage>();

        // Apply the received transform
        transform.position = msg.position;
        transform.rotation = msg.rotation;
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