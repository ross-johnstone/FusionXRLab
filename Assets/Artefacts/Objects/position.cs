using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class AutoResetIfThrown : MonoBehaviour
{
    public float maxDistance = 3f; // How far it can go before resetting
    public float checkInterval = 1f; // How often to check
    public float resetDelay = 0.5f; // Optional delay before reset

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grab;
    private bool isBeingHeld = false;

    private void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrab);
            grab.selectExited.AddListener(OnRelease);
        }

        InvokeRepeating(nameof(CheckDistance), checkInterval, checkInterval);
    }

    private void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnGrab);
            grab.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isBeingHeld = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isBeingHeld = false;
    }

    private void CheckDistance()
    {
        if (isBeingHeld) return;

        float distance = Vector3.Distance(transform.position, initialPosition);
        if (distance > maxDistance)
        {
            Invoke(nameof(ResetObject), resetDelay);
        }
    }

    private void ResetObject()
    {
        transform.SetPositionAndRotation(initialPosition, initialRotation);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}


