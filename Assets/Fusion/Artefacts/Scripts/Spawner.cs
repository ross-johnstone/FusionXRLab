using UnityEngine;
using Ubiq.Spawning;
using UnityEngine.InputSystem;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public static Vector3 PendingPosition;
    public static Quaternion PendingRotation;

    public GameObject objectPrefabA;
    public GameObject objectPrefabB;
    private NetworkSpawnManager spawnManager;
    private GameObject currentSpawnedObject;
    private enum SpawnType { None, A, B }
    private SpawnType currentType = SpawnType.None;

    void Start()
    {
        spawnManager = NetworkSpawnManager.Find(this);
    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            DespawnCurrentObject();
            SpawnObject(objectPrefabA, SpawnType.A);
        }
        else if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            DespawnCurrentObject();
            SpawnObject(objectPrefabB, SpawnType.B);
        }
    }

    private void SpawnObject(GameObject prefab, SpawnType type)
    {
        if (prefab == null || spawnManager == null)
        {
            Debug.LogError("Prefab or spawn manager is not set.");
            return;
        }

        PendingPosition = transform.position;
        currentSpawnedObject = spawnManager.SpawnWithPeerScope(prefab);
        currentType = type;

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
