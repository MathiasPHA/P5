using UnityEngine;

public class DoorRotateAround : MonoBehaviour
{
    //HURTIGT CLAUDE SCRPIPT TIL AT ÅBNE OG LUKKE MED EN BOOL. LAV DET FRA BUNDEN PÅ ET SENERE TIDSPUNKT
    [SerializeField] private Transform hingePoint;
    [SerializeField] private float openAngle = 110f;  // where the door stops
    [SerializeField] private float speed = 60f;       // degrees per second

    public bool open;   // tick this to open the door, untick to close it

    private float currentAngle;

    private void Update()
    {
        float target = open ? openAngle : 0f;
        if (Mathf.Approximately(currentAngle, target)) return;

        float next = Mathf.MoveTowards(currentAngle, target, speed * Time.deltaTime);
        transform.RotateAround(hingePoint.position, Vector3.up, next - currentAngle);
        currentAngle = next;
    }
}