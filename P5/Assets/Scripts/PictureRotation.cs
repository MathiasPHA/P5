using UnityEngine;

public class PictureRotation : MonoBehaviour
{
    [SerializeField] private Transform hingePoint;
    [SerializeField] private Vector3 hingeAxis = Vector3.up;
    [SerializeField] private float openAngle = -100f;
    [SerializeField] private float openingSpeed = 60f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pictureRotationSound;

    public bool rotatePicture;

    private float currentAngle;

    public void rotate()
    {
        rotatePicture = true;
        audioSource.PlayOneShot(pictureRotationSound);
    }
    

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
