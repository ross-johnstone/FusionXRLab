using UnityEngine;
using Ubiq.Spawning;
using UnityEngine.InputSystem;

public class Spawner : MonoBehaviour
{
    public GameObject objectPrefab;
    private NetworkSpawnManager spawnManager;

    void Start()
    {
        spawnManager = NetworkSpawnManager.Find(this);
    }

    void Update()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
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

        GameObject spawned = spawnManager.SpawnWithPeerScope(objectPrefab);
        spawned.transform.position = transform.position;
        Debug.Log("Spawned object at " + spawned.transform.position);
    }
}
