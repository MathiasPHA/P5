using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// Put on the milk bottle in the safe (TheFinalDropOfMilk, VR scene).
// The first time the bottle is grabbed, the game is won for both players:
// SharedState tells the PC and the VR side, and WinSequence plays the ending on each.
// `won` resets on its own when the scene reloads (the bottle is a new instance then).
[RequireComponent(typeof(XRGrabInteractable))]
public class MilkBottleWin : MonoBehaviour
{
    [Tooltip("Optional: the bottle can't be grabbed until this safe is unlocked " +
             "(stops a grab through the closed safe door)")]
    [SerializeField] SafeLock safeLock;

    XRGrabInteractable grab;
    bool won;

    void Awake() => grab = GetComponent<XRGrabInteractable>();

    void OnEnable() => grab.selectEntered.AddListener(OnGrabbed);
    void OnDisable() => grab.selectEntered.RemoveListener(OnGrabbed);

    void Start()
    {
        if (safeLock == null) return;
        grab.enabled = false;
        safeLock.onUnlocked.AddListener(AllowGrab);
    }

    void OnDestroy()
    {
        if (safeLock != null) safeLock.onUnlocked.RemoveListener(AllowGrab);
    }

    void AllowGrab() => grab.enabled = true;

    void OnGrabbed(SelectEnterEventArgs args)
    {
        if (won) return;

        if (SharedState.Instance == null)
        {
            Debug.LogWarning("[MilkBottleWin] SharedState missing, the win was not sent (scene started without Boot?)");
            return;
        }

        won = true;
        SharedState.Instance.WinGameRpc();
    }
}