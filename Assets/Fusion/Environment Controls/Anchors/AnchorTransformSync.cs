using System;
using UnityEngine;
using Ubiq.Messaging;

public class AnchorTransformSync : MonoBehaviour
{
    private NetworkContext context;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool isAuthoritative = false;

    [Serializable]
    public struct TransformData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    void Start()
    {
        context = NetworkScene.Register(this);
        lastPosition = transform.position;
        lastRotation = transform.rotation;
        // Determine authority (e.g., only creator sends updates)
        // For now, assume the local peer is authoritative if they spawned the object
        // You may want to set isAuthoritative from outside based on your logic
        isAuthoritative = true;
    }

    void Update()
    {
        if (isAuthoritative)
        {
            // Broadcast transform if changed
            if (transform.position != lastPosition || transform.rotation != lastRotation)
            {
                var data = new TransformData
                {
                    position = transform.position,
                    rotation = transform.rotation
                };
                string json = JsonUtility.ToJson(data);
                context.Send(json);
                lastPosition = transform.position;
                lastRotation = transform.rotation;
            }
        }
    }

    // This is the only required ProcessMessage for Ubiq
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        if (!isAuthoritative)
        {
            string json = message.ToString();
            var data = JsonUtility.FromJson<TransformData>(json);
            transform.SetPositionAndRotation(data.position, data.rotation);
            lastPosition = data.position;
            lastRotation = data.rotation;
        }
    }

    // Optionally, allow external scripts to set authority
    public void SetAuthority(bool authority)
    {
        isAuthoritative = authority;
    }
} 