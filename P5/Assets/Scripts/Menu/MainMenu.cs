using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using System.Linq;
using Unity.Multiplayer.PlayMode;
#endif

// Start screen for both players. Shows the PC or VR version of the menu
// (same platform check as Bootstrap) and sends the player to the Boot scene.
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string bootScene = "Boot";
    [Tooltip("Desktop menu: camera + overlay canvas")]
    [SerializeField] private GameObject pcMenu;
    [Tooltip("VR menu: XR Origin + world-space canvas")]
    [SerializeField] private GameObject vrMenu;

    public static bool IsVR
    {
        get
        {
            bool vr = Application.platform == RuntimePlatform.Android;
#if UNITY_EDITOR
            // Multiplayer Play Mode player tagged "VR" acts as the Quest
            if (CurrentPlayer.Tags.Contains("VR")) vr = true;
#endif
            return vr;
        }
    }

    void Awake()
    {
        bool vr = IsVR;
        if (pcMenu != null) pcMenu.SetActive(!vr);
        if (vrMenu != null) vrMenu.SetActive(vr);
    }

    // Button hooks
    public void StartGame()
    {
        if (!Application.CanStreamedLevelBeLoaded(bootScene))
        {
            Debug.LogError($"[MainMenu] Scene '{bootScene}' can't be loaded: not in the build or misspelled.");
            return;
        }
        SceneManager.LoadScene(bootScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}