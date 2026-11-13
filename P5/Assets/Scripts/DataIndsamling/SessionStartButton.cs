using UnityEngine;
using UnityEngine.UI;

// Put this on the main menu's Start button (PC side).
// - Keeps the button greyed out until the VR headset is connected.
// - Add StartSession() to the button's OnClick() list to start the session clock.
[RequireComponent(typeof(Button))]
public class SessionStartButton : MonoBehaviour
{
    [Tooltip("Optional: shown while waiting for the VR player, hidden once connected")]
    [SerializeField] GameObject waitingHint;

    Button button;

    void Awake() => button = GetComponent<Button>();

    void Update()
    {
        bool connected = SessionLogger.VrConnected;
        button.interactable = connected;
        if (waitingHint != null) waitingHint.SetActive(!connected);
    }

    // Hook this up in the Button's OnClick() list
    public void StartSession()
    {
        if (SessionLogger.Instance != null) SessionLogger.Instance.StartSession();
        else Debug.LogWarning("[SessionStartButton] No SessionLogger: session is not being timed");
    }
}
