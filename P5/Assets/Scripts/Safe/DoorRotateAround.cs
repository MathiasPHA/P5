using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
// SCRIPTET ER LAVET HURTIGT MED HJÆLP FRA CLAUDE. GENEVALUER / GENSKRIV DET
public class DoorRotateAround : MonoBehaviour
{
    [SerializeField] private Transform hingePoint;
    [SerializeField] private float openAngle = 110f;  // max opening. Use a negative value to flip direction

    private bool unlocked;
    private Transform hand; // the hand currently holding the handle
    private Vector3 startDir; // hand direction from hinge when grabbed
    private float angleAtGrab; // door angle when grabbed
    private float currentAngle;

    public void Unlock()
    {
        unlocked = true;
    }

    public void BeginGrab(SelectEnterEventArgs args)
    {
        if (!unlocked) return;
        hand = args.interactorObject.transform;
        startDir = FlatDirFromHinge(hand.position);
        angleAtGrab = currentAngle;
    }

    public void EndGrab(SelectExitEventArgs args)
    {
        hand = null;
    }

    private void Update()
    {
        if (hand == null) return;

        float delta = Vector3.SignedAngle(startDir, FlatDirFromHinge(hand.position), Vector3.up);

        float min = Mathf.Min(0f, openAngle);
        float max = Mathf.Max(0f, openAngle);
        float target = Mathf.Clamp(angleAtGrab + delta, min, max);

        transform.RotateAround(hingePoint.position, Vector3.up, target - currentAngle);
        currentAngle = target;
    }

    private Vector3 FlatDirFromHinge(Vector3 worldPos)
    {
        Vector3 dir = worldPos - hingePoint.position;
        dir.y = 0f;
        return dir.normalized;
    }
}