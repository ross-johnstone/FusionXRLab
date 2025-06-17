using UnityEngine;
using Ubiq.Messaging;

public class NetworkedObject : MonoBehaviour
{
    private NetworkContext context;
    public NetworkId Id { get; private set; }

    private bool isOwner = false;

    // Use a session-unique ID instead of PeerUuid (not accessible in this Ubiq version)
    private static string localPeerId = System.Guid.NewGuid().ToString();

    private void Start()
    {
        context = NetworkScene.Register(this);
        Id = NetworkId.Unique();
    }

    private void Update()
    {
        if (isOwner)
        {
            var msg = new TransformMessage(transform.position, transform.rotation, localPeerId);
            context.SendJson(msg);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<TransformMessage>();

        if (msg.ownerId != localPeerId)
        {
            isOwner = false;
            transform.position = msg.position;
            transform.rotation = msg.rotation;
        }
    }

    public void TakeOwnership()
    {
        isOwner = true;
    }

    [System.Serializable]
    private struct TransformMessage
    {
        public Vector3 position;
        public Quaternion rotation;
        public string ownerId;

        public TransformMessage(Vector3 pos, Quaternion rot, string owner)
        {
            position = pos;
            rotation = rot;
            ownerId = owner;
        }
    }
}
