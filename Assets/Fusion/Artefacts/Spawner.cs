using UnityEngine;
using Ubiq.Spawning;
using UnityEngine.InputSystem;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public static Vector3 PendingPosition;
    public static Quaternion PendingRotation;

    public GameObject objectPrefab;
    private NetworkSpawnManager spawnManager;
    private GameObject currentSpawnedObject;

    void Start()
    {
        spawnManager = NetworkSpawnManager.Find(this);
    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            DespawnCurrentObject();
            SpawnObject();
        }
    }

    private void SpawnObject()
    {
        if (objectPrefab == null || spawnManager == null)
        {
            Debug.LogError("Prefab or spawn manager is not set.");
            return;
        }

        PendingPosition = transform.position;
        //PendingRotation = transform.rotation; //TEST
        currentSpawnedObject = spawnManager.SpawnWithPeerScope(objectPrefab);
        //Debug.Log("Spawned object at " + PendingPosition);

        // Set initial ownership if the component exists
        var grabbable = currentSpawnedObject.GetComponent<NetworkedGrabbableWithVelocity>();
        if (grabbable != null)
        {
            grabbable.SetOwner(true);
            StartCoroutine(ForceNetworkUpdateNextFrame(grabbable));
        }
    }

    private void DespawnCurrentObject()
    {
        if (currentSpawnedObject != null && spawnManager != null)
        {
            spawnManager.Despawn(currentSpawnedObject);
            currentSpawnedObject = null;
        }
    }

    private System.Collections.IEnumerator ForceNetworkUpdateNextFrame(NetworkedGrabbableWithVelocity grabbable)
    {
        yield return null; // Wait one frame so Start() runs
        grabbable.ForceNetworkUpdate();
    }
}
