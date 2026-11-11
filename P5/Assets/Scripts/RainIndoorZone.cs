using UnityEngine;

// Put on a GameObject with a collider (3D or 2D) covering an indoor area.
// No Rigidbody needed: checks the player's position against the collider each frame.
public class RainIndoorZone : MonoBehaviour
{
    [SerializeField] private RainAmbience rain;
    [SerializeField, Tooltip("Leave empty to find the object tagged Player.")]
    private Transform player;
    [SerializeField] private string playerTag = "Player";

    private Collider zone3D;
    private Collider2D zone2D;
    private bool inside;

    private void Awake()
    {
        if (rain == null) rain = FindFirstObjectByType<RainAmbience>();
        zone3D = GetComponent<Collider>();
        zone2D = GetComponent<Collider2D>();
    }

    private void Reset()
    {
        if (TryGetComponent(out Collider c)) c.isTrigger = true;
        if (TryGetComponent(out Collider2D c2)) c2.isTrigger = true;
    }

    private void Update()
    {
        if (rain == null) return;

        if (player == null)
        {
            var found = GameObject.FindWithTag(playerTag);
            if (found == null) return;
            player = found.transform;
        }

        bool now = Contains(player.position);
        if (now == inside) return;

        inside = now;
        if (inside) rain.EnterIndoor();
        else rain.ExitIndoor();
    }

    private bool Contains(Vector3 point)
    {
        if (zone3D != null && zone3D.enabled)
            return (zone3D.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
        if (zone2D != null && zone2D.enabled)
            return zone2D.OverlapPoint(point);
        return false;
    }

    private void OnDisable()
    {
        if (inside && rain != null) rain.ExitIndoor();
        inside = false;
    }
}