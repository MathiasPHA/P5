using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using System.Linq;
using Unity.Multiplayer.PlayMode;
#endif

// First scene in the build. Sends each player to their own start screen.
public class Launcher : MonoBehaviour
{
    [SerializeField] private string pcMenuScene = "MainMenu_PC";
    [SerializeField] private string vrMenuScene = "MainMenu_VR";

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

    void Start()
    {
        string scene = IsVR ? vrMenuScene : pcMenuScene;

        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"[Launcher] Scene '{scene}' can't be loaded: not in the build or misspelled.");
            return;
        }
        SceneManager.LoadScene(scene);
    }
}