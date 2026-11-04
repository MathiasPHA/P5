using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x – delete this line on XRI 2.x

[RequireComponent(typeof(XRGrabInteractable))]
public class PhysicsOn : MonoBehaviour
{
    [SerializeField] Rigidbody rb;

    XRGrabInteractable grab;
    bool activated;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void OnEnable() => grab.selectExited.AddListener(OnReleased);
    void OnDisable() => grab.selectExited.RemoveListener(OnReleased);

    void OnReleased(SelectExitEventArgs args)
    {
        if (activated) return;          // only the first time
        activated = true;
        grab.selectExited.RemoveListener(OnReleased);
        StartPhysics();
    }

    public void StartPhysics()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }
}