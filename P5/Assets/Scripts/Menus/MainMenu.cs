using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Start screen actions. Hook Start/Quit buttons (UI or VR) to these methods.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string bootScene = "Boot";
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
        }
    }

    private void OnDisable() => Unsubscribe();

    private void Unsubscribe()
    {
        if (subscribedTo != null) subscribedTo.OnGameStarted -= StartGameRemote;
        subscribedTo = null;
    }

    // Button hook. Only tells the server; the actual load happens in
    // StartGameRemote, which runs on BOTH players when GameStarted flips to true.
    public void StartGame()
    {
        if (starting) return;
        if (SharedState.Instance == null)
        {
            Debug.LogWarning("[MainMenu] SharedState not spawned yet, can't start.");
            return;
        }
        SharedState.Instance.SetGameStartedRpc(true);
    }

    // Runs on every peer via SharedState.OnGameStarted
    private void StartGameRemote()
    {
        if (starting) return;   // ignore double triggers
        starting = true;
        StartCoroutine(LoadBoot());
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator LoadBoot()
    {
        if (startDelay > 0f) yield return new WaitForSecondsRealtime(startDelay);

        if (!Application.CanStreamedLevelBeLoaded(bootScene))
        {
            Debug.LogError($"[MainMenu] Scene '{bootScene}' can't be loaded: not in the build or misspelled.");
            starting = false;
            yield break;
        }

        // Reset so a later return to the menu can start again (false->true must happen to fire the event)
        SharedState.Instance?.SetGameStartedRpc(false);
        SceneManager.LoadScene(bootScene);
    }
}