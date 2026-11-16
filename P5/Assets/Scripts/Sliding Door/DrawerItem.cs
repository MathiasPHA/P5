using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class DrawerItem : MonoBehaviour
{
    [SerializeField] private Transform drawer;          // the moving drawer object
    [SerializeField] private Collider drawerInterior;   // trigger box covering the inside

    private Rigidbody rb;
    private XRGrabInteractable grab;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Stop XRI from restoring the old parent on release — we decide that ourselves
        grab.retainTransformParent = false;
    }

    private void OnEnable()
    {
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void Start()
    {
        // Handles the paper starting inside the drawer
        if (IsInsideDrawer()) AttachToDrawer();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        StopAllCoroutines();
        transform.SetParent(null, true);
        // XRI handles kinematic state while held
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // XRI has just restored the "was kinematic" state from grab time — undo that
        rb.isKinematic = false;
        rb.useGravity = true;

        StartCoroutine(CheckAfterRelease());
    }

    private void OnTriggerStay(Collider other)
    {
        if (other != drawerInterior) return;          // only care about the drawer's interior
        if (grab.isSelected) return;                  // being held
        if (transform.parent == drawer) return;       // already attached

        // Wait until it has actually landed, so it doesn't freeze mid-fall
        if (rb.linearVelocity.sqrMagnitude > 0.0025f) return;   // ~5 cm/s; rb.velocity on 2022 or older

        if (IsInsideDrawer()) AttachToDrawer();
    }


    private IEnumerator CheckAfterRelease()
    {
        // XRI applies throw velocity in the physics step after release.
        // Wait for that, otherwise we'd set velocity on a kinematic body.
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        if (IsInsideDrawer()) AttachToDrawer();
    }

    private bool IsInsideDrawer()
    {
        Vector3 p = rb.worldCenterOfMass;
        // ClosestPoint returns p itself if p is inside the collider
        return (drawerInterior.ClosestPoint(p) - p).sqrMagnitude < 0.0001f;
    }

    private void AttachToDrawer()
    {
        rb.linearVelocity = Vector3.zero;   // use rb.velocity on Unity 2022 or older
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.SetParent(drawer, true);
    }
}