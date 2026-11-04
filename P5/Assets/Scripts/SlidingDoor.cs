using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] private Vector3 slideAxis = Vector3.right; // in the door's local space
    [SerializeField] private float maxDistance = 1.0f;          // how far it can slide open (metres)

    private Transform hand;
    private Vector3 worldAxis;
    private float handStart;       // hand position along the axis when grabbed
    private float offsetAtGrab;    // door offset when grabbed
    private float currentOffset;   // 0 = closed, maxDistance = fully open

    // Hook to the handle's Select Entered
    public void BeginGrab(SelectEnterEventArgs args)
    {
        hand = args.interactorObject.transform;
        worldAxis = transform.TransformDirection(slideAxis).normalized;
        handStart = Vector3.Dot(hand.position, worldAxis);
        offsetAtGrab = currentOffset;
    }

    // Hook to the handle's Select Exited
    public void EndGrab(SelectExitEventArgs args)
    {
        hand = null;
    }

    private void Update()
    {
        if (hand == null) return;

        float delta = Vector3.Dot(hand.position, worldAxis) - handStart;

        float min = Mathf.Min(0f, maxDistance);
        float max = Mathf.Max(0f, maxDistance);
        float target = Mathf.Clamp(offsetAtGrab + delta, min, max);

        transform.position += worldAxis * (target - currentOffset);
        currentOffset = target;
    }
}