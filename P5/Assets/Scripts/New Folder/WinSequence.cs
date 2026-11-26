using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Put on the SharedState prefab. SharedState lives in DontDestroyOnLoad on both the PC
// and the VR side, so the ending plays for both players no matter which scene they are in.
//
// When the game is won (SharedState.OnGameWon):
//   victory sound -> wait -> fade to black -> load the outro scene -> fade back in.
//   PC (host):   black overlay on top of the screen.
//   VR (client): black panel that stays right in front of the headset camera.
//
// This object persists between games, so `playing` is reset when the sequence finishes
// and when a new game starts (SharedState.OnGameStarted). Otherwise the second win is ignored.
[RequireComponent(typeof(SharedState))]
public class WinSequence : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] AudioClip victorySound;
    [SerializeField, Range(0f, 1f)] float volume = 1f;

    [Header("Timing (seconds)")]
    [Tooltip("From the bottle being grabbed until the fade starts")]
    [SerializeField] float delayBeforeFade = 5f;
    [SerializeField] float fadeOutDuration = 1.5f;
    [Tooltip("Fade from black into the outro scene. 0 = cut straight in")]
    [SerializeField] float fadeInDuration = 1f;

    [Header("Outro scene (must be in the Build Profile scene list)")]
    [SerializeField] string pcOutroScene = "Outro";
    [SerializeField] string vrOutroScene = "Outro";

    SharedState state;
    AudioSource source;
    bool playing;
    Coroutine running;

    GameObject fadeRoot;
    Image fadeImage;
    bool followCamera; // VR: keep the black panel in front of the headset

    void Awake() => state = GetComponent<SharedState>();

    void OnEnable()
    {
        state.OnGameWon += Play;
        state.OnGameStarted += ResetForNextGame;
    }

    void OnDisable()
    {
        state.OnGameWon -= Play;
        state.OnGameStarted -= ResetForNextGame;
    }

    void OnDestroy()
    {
        if (fadeRoot != null) Destroy(fadeRoot);
    }

    void Play()
    {
        if (playing) return;
        playing = true;
        running = StartCoroutine(Run());
    }

    // New game started: make sure the next win can play, even if the last sequence was cut off
    void ResetForNextGame()
    {
        if (running != null) StopCoroutine(running);
        running = null;
        CleanUpFade();
        playing = false;
    }

    IEnumerator Run()
    {
        // Same rule as ConnectionWatcher: the host is the PC, the client is the headset
        var nm = NetworkManager.Singleton;
        bool isPC = nm != null && nm.IsServer;
        string scene = isPC ? pcOutroScene : vrOutroScene;

        if (victorySound != null)
        {
            // 2D, and on this object, so it keeps playing through the scene change.
            // Reused between games instead of adding a new AudioSource every win.
            if (source == null) source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 0f;
            source.PlayOneShot(victorySound, volume);
        }

        yield return new WaitForSecondsRealtime(delayBeforeFade);

        BuildFade(isPC);
        yield return Fade(0f, 1f, fadeOutDuration);

        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError($"[WinSequence] Scene '{scene}' can't be loaded: not in the build or misspelled.");
        }
        else
        {
            var load = SceneManager.LoadSceneAsync(scene);
            while (!load.isDone) yield return null;
        }

        yield return null; // one frame for the new scene's camera to start
        yield return Fade(1f, 0f, fadeInDuration);

        CleanUpFade();
        running = null;
        playing = false; // allow the next win
    }

    // ───────────────────────── Fade ─────────────────────────

    IEnumerator Fade(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    void SetAlpha(float a)
    {
        if (fadeImage != null) fadeImage.color = new Color(0f, 0f, 0f, a);
    }

    void CleanUpFade()
    {
        if (fadeRoot != null) Destroy(fadeRoot);
        fadeRoot = null;
        fadeImage = null;
        followCamera = false;
    }

    void BuildFade(bool isPC)
    {
        CleanUpFade(); // never stack two fade canvases

        fadeRoot = new GameObject("WinFade", typeof(Canvas));
        DontDestroyOnLoad(fadeRoot); // stays black while the scenes swap

        var canvas = fadeRoot.GetComponent<Canvas>();
        if (isPC)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // just under ConnectionWatcher's warning (1000)
        }
        else
        {
            // Screen-space overlays are not drawn in the headset, so use a
            // 10 x 10 m world-space panel that LateUpdate keeps in front of the camera
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 999;
            ((RectTransform)fadeRoot.transform).sizeDelta = new Vector2(100f, 100f);
            fadeRoot.transform.localScale = Vector3.one * 0.1f;
            followCamera = true;
            FollowCamera();
        }

        fadeImage = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var rt = fadeImage.rectTransform;
        rt.SetParent(fadeRoot.transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        fadeImage.raycastTarget = false;
        SetAlpha(0f);
    }

    void LateUpdate()
    {
        if (followCamera && fadeRoot != null) FollowCamera();
    }

    // Camera.main changes when the outro scene loads; the panel simply follows the new one
    void FollowCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var c = cam.transform;
        fadeRoot.transform.SetPositionAndRotation(
            c.position + c.forward * (cam.nearClipPlane + 0.05f), c.rotation);
    }
}