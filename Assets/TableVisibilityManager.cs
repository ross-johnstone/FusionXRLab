using UnityEngine;

public class TableVisibilityManager : MonoBehaviour
{
    public enum TableState
    {
        None,
        TableOnly,
        TableWithObjects1To10,
        TableWithObjects11To20,
        TableTokyo,
        TableParis,
        TableNewYork
    }

    [Header("State Control")]
    public TableState currentState;

    [Header("Scene Objects")]
    public GameObject table;
    public GameObject[] objects1to10;
    public GameObject[] objects11to20;

    [Header("Location Objects")]
    public GameObject[] tokyoObjects;
    public GameObject[] parisObjects;
    public GameObject[] newYorkObjects;

    private void Start()
    {
        ApplyState();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyState();
    }
#endif

    public void ApplyState()
    {
        // Turn everything off by default
        SetActiveVisuals(table, false);
        SetActiveVisuals(objects1to10, false);
        SetActiveVisuals(objects11to20, false);
        SetActiveVisuals(tokyoObjects, false);
        SetActiveVisuals(parisObjects, false);
        SetActiveVisuals(newYorkObjects, false);

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

            case TableState.TableTokyo:
                SetActiveVisuals(table, true);
                SetActiveVisuals(tokyoObjects, true);
                break;

            case TableState.TableParis:
                SetActiveVisuals(table, true);
                SetActiveVisuals(parisObjects, true);
                break;

            case TableState.TableNewYork:
                SetActiveVisuals(table, true);
                SetActiveVisuals(newYorkObjects, true);
                break;

            case TableState.None:
            default:
                break;
        }
    }

    private void SetActiveVisuals(GameObject obj, bool active)
    {
        if (obj == null) return;

        // Enable/disable all renderers
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
        {
            renderer.enabled = active;
        }

        // Enable/disable all colliders
        foreach (var collider in obj.GetComponentsInChildren<Collider>(true))
        {
            collider.enabled = active;
        }
    }

    private void SetActiveVisuals(GameObject[] objs, bool active)
    {
        if (objs == null) return;

        foreach (var obj in objs)
        {
            if (obj == null) continue;
            SetActiveVisuals(obj, active);
        }
    }
}
