using UnityEngine;
using Ubiq.Spawning;
using UnityEngine.InputSystem;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public static Vector3 PendingPosition;
    public static Quaternion PendingRotation;

    [Header("Generic Object Prefabs (used by SpawnA/SpawnB)")]
    public GameObject objectPrefabA;
    public GameObject objectPrefabB;

    [Header("Table Prefab")]
    public GameObject tablePrefab;

    [Header("Paper Prefabs (used by SpawnPaper1/2/3)")]
    public GameObject paperPrefab1;
    public GameObject paperPrefab2;
    public GameObject paperPrefab3;

    private NetworkSpawnManager spawnManager;

    private GameObject currentSpawnedObject;
    private GameObject currentTableObject;

    private enum SpawnType { None, A, B, Paper1, Paper2, Paper3, Table }
    private SpawnType currentType = SpawnType.None;

    void Start()
    {
        spawnManager = NetworkSpawnManager.Find(this);
    }

    void Update()
    {
        // existing keyboard debug controls
        if (Keyboard.current != null)
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
            else if (Keyboard.current.vKey.wasPressedThisFrame)
            {
                DespawnTableObject();
                SpawnTableObject();
            }
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
        PendingRotation = transform.rotation; // safe to always set

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
            currentType = SpawnType.None;
        }
    }

    private void SpawnTableObject()
    {
        if (tablePrefab == null || spawnManager == null)
        {
            Debug.LogError("Table prefab or spawn manager is not set.");
            return;
        }

        PendingPosition = transform.position;
        PendingRotation = transform.rotation;

        currentTableObject = spawnManager.SpawnWithPeerScope(tablePrefab);

        var grabbable = currentTableObject.GetComponent<NetworkedGrabbableWithVelocity>();
        if (grabbable != null)
        {
            grabbable.SetOwner(true);
            StartCoroutine(ForceNetworkUpdateNextFrame(grabbable));
        }
    }

    private void DespawnTableObject()
    {
        if (currentTableObject != null && spawnManager != null)
        {
            spawnManager.Despawn(currentTableObject);
            currentTableObject = null;
        }
    }

    // Existing API
    public void SpawnA()
    {
        DespawnCurrentObject();
        SpawnObject(objectPrefabA, SpawnType.A);
    }

    public void SpawnB()
    {
        DespawnCurrentObject();
        SpawnObject(objectPrefabB, SpawnType.B);
    }

    public void SpawnTable()
    {
        DespawnTableObject();
        SpawnTableObject();
    }

    // NEW: Papers API
    public void SpawnPaper1()
    {
        DespawnCurrentObject();
        SpawnObject(paperPrefab1, SpawnType.Paper1);
    }

    public void SpawnPaper2()
    {
        DespawnCurrentObject();
        SpawnObject(paperPrefab2, SpawnType.Paper2);
    }

    public void SpawnPaper3()
    {
        DespawnCurrentObject();
        SpawnObject(paperPrefab3, SpawnType.Paper3);
    }

    public void DespawnAll()
    {
        DespawnCurrentObject();
        DespawnTableObject();
    }

    private IEnumerator ForceNetworkUpdateNextFrame(NetworkedGrabbableWithVelocity grabbable)
    {
        yield return null; // Wait one frame so Start() runs
        grabbable.ForceNetworkUpdate();
    }
}