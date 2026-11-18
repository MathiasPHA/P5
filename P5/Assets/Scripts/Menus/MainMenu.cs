using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Start screen actions. Hook Start/Quit buttons (UI or VR) to these methods.
// Each copy's "Next Scene" is set in the Inspector (e.g. the PC game scene on the PC,
// VRCutscene on the VR side). Can also be used in later scenes to move both players on together.
// StartGame() on either side moves BOTH: the button only tells the server, and every
// MainMenu loads its scene when SharedState.OnGameStarted fires (sent as an RPC to both sides).
public class MainMenu : MonoBehaviour
{
    [Tooltip("The scene this side loads when the game starts")]
    [UnityEngine.Serialization.FormerlySerializedAs("bootScene")] // keeps the value already set in the Inspector
    [SerializeField] private string nextScene = "Boot";
    [Tooltip("Small pause before loading, e.g. so a VR button press animation can finish")]
    [SerializeField] private float startDelay = 0.3f;

    private bool starting;
    private SharedState subscribedTo;

    private void Update()
    {
        // SharedState is network-spawned and may appear after this menu is enabled,
        // so subscribe as soon as it exists (and re-subscribe if it's ever replaced).
        var state = SharedState.Instance;
        if (state != null && state != subscribedTo)
        {
            Unsubscribe();
            state.OnGameStarted += StartGameRemote;
            subscribedTo = state;
            Debug.Log($"[MainMenu] Listening for game start (next scene '{nextScene}')");
        }
    }

    private void OnDisable() => Unsubscribe();

    private void Unsubscribe()
    {
        if (subscribedTo != null) subscribedTo.OnGameStarted -= StartGameRemote;
        subscribedTo = null;
    }

    // Button hook. Only tells the server; the actual load happens in
    // StartGameRemote, which runs on BOTH players when OnGameStarted fires.
    public void StartGame()
    {
        if (starting) return;
        if (SharedState.Instance == null)
        {
            Debug.LogWarning("[MainMenu] SharedState not spawned yet, can't start.");
            return;
        }
        Debug.Log("[MainMenu] Start pressed, telling the server");
        SharedState.Instance.SetGameStartedRpc(true);
    }

    // Runs on every peer via SharedState.OnGameStarted
    private void StartGameRemote()
    {
        if (starting) return;   // ignore double triggers
        starting = true;
        Debug.Log($"[MainMenu] Game started, loading '{nextScene}'");
        StartCoroutine(LoadNext());
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadNext()
    {
        if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

        if (!Application.CanStreamedLevelBeLoaded(nextScene))
        {
            Debug.LogError($"[MainMenu] Scene '{nextScene}' can't be loaded: not in the build or misspelled.");
            starting = false;
            yield break;
        }

        SceneManager.LoadScene(nextScene);
    }
}