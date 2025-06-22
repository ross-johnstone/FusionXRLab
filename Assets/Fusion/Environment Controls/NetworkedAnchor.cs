using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

public class NetworkedAnchor : MonoBehaviour, INetworkSpawnable
{
    public NetworkId NetworkId { get; set; }
    Vector3 anchorPosition;
    NetworkContext context;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        context = NetworkScene.Register(this);
    }

    // Update is called once per frame
    void Update()
    {
        if (anchorPosition != transform.position)
        {
            anchorPosition = transform.position;
            context.SendJson(new Message()
            {
                position = transform.position,
            });
        }
    }

    private struct Message
    {
        public Vector3 position;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        // Parse the message
        var m = message.FromJson<Message>();

        // Use the message to update the Component
        transform.localPosition = m.position;

        // Make sure the logic in Update doesn't trigger as a result of this message
        anchorPosition = transform.localPosition;
    }

}
