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

    public void StartGame()
    {
        if (starting) return;   // ignore double presses
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
        SceneManager.LoadScene(bootScene);
    }
}