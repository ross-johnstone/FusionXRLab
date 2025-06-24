using Ubiq.Spawning;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestScript : MonoBehaviour
{
    [SerializeField] private GameObject anchorPrefab;
    [SerializeField] private NetworkSpawnManager networkSpawnManager;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current?.leftButton != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            SpawnNetworkedAnchor();
        }
    }

    private void SpawnNetworkedAnchor()
    {
        if (anchorPrefab == null || networkSpawnManager == null)
        {
            Debug.LogWarning("AnchorPrefab or NetworkSpawnManager not assigned.");
            return;
        }

        if (mainCamera == null)
        {
            Debug.LogWarning("Main camera not found.");
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 spawnPosition = hit.point;
            Quaternion spawnRotation = Quaternion.identity;

            GameObject instance = networkSpawnManager.SpawnWithPeerScope(anchorPrefab);
            instance.transform.position = spawnPosition;
            instance.transform.rotation = spawnRotation;   
        }
    }
}
