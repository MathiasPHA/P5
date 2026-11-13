using UnityEngine;

// Put on a trigger collider (3D or 2D) covering an indoor area.
public class RainIndoorZone : MonoBehaviour
{
    [SerializeField] private RainAmbience rain;
    [SerializeField] private string playerTag = "Player";

    private int playerCollidersInside; // handles players with several colliders

    private void Awake()
    {
        if (rain == null) rain = FindFirstObjectByType<RainAmbience>();
    }

    private void Reset()
    {
        if (TryGetComponent(out Collider c)) c.isTrigger = true;
        if (TryGetComponent(out Collider2D c2)) c2.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => Enter(other.CompareTag(playerTag));
    private void OnTriggerExit(Collider other) => Exit(other.CompareTag(playerTag));
    private void OnTriggerEnter2D(Collider2D other) => Enter(other.CompareTag(playerTag));
    private void OnTriggerExit2D(Collider2D other) => Exit(other.CompareTag(playerTag));

    private void Enter(bool isPlayer)
    {
        if (!isPlayer || rain == null) return;
        if (playerCollidersInside++ == 0) rain.EnterIndoor();
    }

    private void Exit(bool isPlayer)
    {
        if (!isPlayer || rain == null || playerCollidersInside == 0) return;
        if (--playerCollidersInside == 0) rain.ExitIndoor();
    }

    private void OnDisable()
    {
        if (playerCollidersInside > 0 && rain != null) rain.ExitIndoor();
        playerCollidersInside = 0;
    }
}
