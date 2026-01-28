using UnityEngine;
using Ubiq.Messaging;

public class TableSpawner : MonoBehaviour
{

    private NetworkContext context;
    private GameObject table;
    private bool toggleTable;

    private struct toggleTableMessage
    {
        public bool toggleTable;
    }

    void Start()
    {
        toggleTable = false;
        context = NetworkScene.Register(this);

        var vrEnv = GameObject.Find("VR Environment");
        if (!vrEnv)
        {
            Debug.LogError("[TableSpawner] Could not find VR Environment");
            return;
        }

        var tableTransform = vrEnv.transform.Find("Table");
        if (!tableTransform)
        {
            Debug.LogError("[TableSpawner] Could not find Table under VR Environment");
            return;
        }

        table = tableTransform.gameObject;

        Debug.Log("[TableSpawner] Table reference acquired successfully");
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<toggleTableMessage>();
        ApplyTableState(msg.toggleTable);
    }

    public void ToggleTable()
    {
        try 
        {
            toggleTable = !toggleTable;
            
            // Send the new state to all clients
            if (context.Id.Valid)
            {
                var message = new toggleTableMessage
                {
                    toggleTable = toggleTable
                };
                context.SendJson(message);
                Debug.Log($"[TableSpawner] Network: Sent table state to all clients");
            }
            else
            {
                Debug.LogWarning("[TableSpawner] Network: Context invalid - cannot send state");
            }

            table.SetActive(toggleTable);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TableSpawner] Could not enable table: {e.Message}");
        }

        ApplyTableState(toggleTable);
        
    }

    public void ApplyTableState(bool state){
        toggleTable = state;
        table.SetActive(state);
        Debug.Log($"[TableSpawner] Table {(state ? "ON" : "OFF")}");
    }

}
