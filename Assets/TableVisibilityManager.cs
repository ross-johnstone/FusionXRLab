using UnityEngine;

public class TableVisibilityManager : MonoBehaviour
{
    public enum TableState
    {
        None,
        TableOnly,
        TableWithObjects1To10,
        TableWithObjects11To20
    }

    [Header("State Control")]
    public TableState currentState;

    [Header("Scene Objects")]
    public GameObject table;
    public GameObject[] objects1to10;
    public GameObject[] objects11to20;

    public void ApplyState()
    {
        // Turn everything off by default
        SetActiveVisuals(table, false);
        SetActiveVisuals(objects1to10, false);
        SetActiveVisuals(objects11to20, false);

        switch (currentState)
        {
            case TableState.TableOnly:
                SetActiveVisuals(table, true);
                break;

            case TableState.TableWithObjects1To10:
                SetActiveVisuals(table, true);
                SetActiveVisuals(objects1to10, true);
                break;

            case TableState.TableWithObjects11To20:
                SetActiveVisuals(table, true);
                SetActiveVisuals(objects11to20, true);
                break;

            case TableState.None:
            default:
                break;
        }
    }

    void SetActiveVisuals(GameObject obj, bool active)
    {
        if (obj == null) return;

        foreach (var renderer in obj.GetComponentsInChildren<MeshRenderer>())
            renderer.enabled = active;

        foreach (var collider in obj.GetComponentsInChildren<Collider>())
            collider.enabled = active;
    }

    void SetActiveVisuals(GameObject[] objs, bool active)
    {
        if (objs == null) return;

        foreach (var obj in objs)
        {
            if (obj == null) continue;
            SetActiveVisuals(obj, active);
        }
    }
}
