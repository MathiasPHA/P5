using UnityEngine;

/// Put this on the milk object (the one using the Custom/Liquid material).
/// It watches how the object moves and rotates and feeds a decaying
/// sloshing motion into the shader.
[RequireComponent(typeof(Renderer))]
public class LiquidWobble : MonoBehaviour
{
    [Tooltip("Maximum tilt of the liquid surface (slope). 0.5 is roughly 27 degrees.")]
    [SerializeField] private float maxWobble = 0.5f;

    [Tooltip("How strongly movement kicks the liquid.")]
    [SerializeField] private float sensitivity = 0.1f;

    [Tooltip("Sloshes per second.")]
    [SerializeField] private float wobbleSpeed = 1.5f;

    [Tooltip("How fast the liquid settles. Higher = calms down sooner.")]
    [SerializeField] private float recovery = 1.5f;

    private static readonly int WobbleXId = Shader.PropertyToID("_WobbleX");
    private static readonly int WobbleZId = Shader.PropertyToID("_WobbleZ");

    private Material material;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float amountX;
    private float amountZ;
    private float time;

    private void Awake()
    {
        material = GetComponent<Renderer>().material;
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        time += dt;

        // Let the slosh die down over time.
        amountX = Mathf.Lerp(amountX, 0f, dt * recovery);
        amountZ = Mathf.Lerp(amountZ, 0f, dt * recovery);

        // Swing back and forth.
        float wave = Mathf.Sin(2f * Mathf.PI * wobbleSpeed * time);
        material.SetFloat(WobbleXId, amountX * wave);
        material.SetFloat(WobbleZId, amountZ * wave);

        // How fast did we move and rotate this frame?
        Vector3 velocity = (transform.position - lastPosition) / dt;

        Quaternion delta = transform.rotation * Quaternion.Inverse(lastRotation);
        delta.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / dt);

        // Movement and rotation both kick the liquid.
        amountX = Mathf.Clamp(
            amountX + (velocity.x + angularVelocity.z * 0.2f) * sensitivity,
            -maxWobble, maxWobble);
        amountZ = Mathf.Clamp(
            amountZ + (velocity.z + angularVelocity.x * 0.2f) * sensitivity,
            -maxWobble, maxWobble);

        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }
}
