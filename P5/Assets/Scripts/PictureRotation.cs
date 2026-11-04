using UnityEngine;

public class PictureRotation : MonoBehaviour
{
    [SerializeField] private Transform hingePoint;
    [SerializeField] private Vector3 hingeAxis = Vector3.up;
    [SerializeField] private float openAngle = -100f;
    [SerializeField] private float openingSpeed = 60f;

    public bool rotatePicture;

    private float currentAngle;

    // Update is called once per frame
    void Update()
    {
        float target = rotatePicture ? openAngle : 0f;
        if (Mathf.Approximately(currentAngle, target)) return;

        float next = Mathf.MoveTowards(currentAngle, target, openingSpeed * Time.deltaTime);
        Vector3 axis = hingePoint.TransformDirection(hingeAxis);
        transform.RotateAround(hingePoint.position, axis, next - currentAngle);
        currentAngle = next;
    }
}
