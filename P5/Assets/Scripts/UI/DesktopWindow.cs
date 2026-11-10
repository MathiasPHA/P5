using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopWindow : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] RectTransform window;

    [Header("Open/Close Animation")]
    [SerializeField] float openDuration = 0.2f;
    [SerializeField] float closeDuration = 0.12f;
    [SerializeField, Range(0.5f, 1f)] float hiddenScale = 0.92f;

    Canvas canvas;
    CanvasGroup group;
    Coroutine animRoutine;

    public bool IsOpen { get; private set; }

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();

        // CanvasGroup on the window handles fade + input blocking
        group = window.GetComponent<CanvasGroup>();
        if (group == null) group = window.gameObject.AddComponent<CanvasGroup>();

        IsOpen = gameObject.activeSelf;
    }

    public void Open()
    {
        bool wasHidden = !gameObject.activeSelf;
        gameObject.SetActive(true); // triggers Awake the first time if it started disabled
        Focus();

        if (IsOpen && animRoutine == null) return; // already fully open
        IsOpen = true;

        // Start from the hidden pose if it was fully closed (not mid-close)
        if (wasHidden)
        {
            group.alpha = 0f;
            window.localScale = Vector3.one * hiddenScale;
        }

        PlayAnimation(1f, 1f, openDuration, EaseOutCubic, deactivateAtEnd: false);
    }

    public void Close()
    {
        if (!IsOpen || !gameObject.activeInHierarchy) return;
        IsOpen = false;
        PlayAnimation(0f, hiddenScale, closeDuration, EaseInCubic, deactivateAtEnd: true);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Focus() => window.SetAsLastSibling(); // bring to front

    public void OnPointerDown(PointerEventData e) => Focus();

    // Called from TitleBarDrag
    public void Move(Vector2 screenDelta)
    {
        // divide by scaleFactor so movement matches the 1920x1080 reference space
        window.anchoredPosition += screenDelta / canvas.scaleFactor;
    }

    // ---------- Animation ----------

    void PlayAnimation(float targetAlpha, float targetScale, float duration,
                       System.Func<float, float> ease, bool deactivateAtEnd)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(Animate(targetAlpha, targetScale, duration, ease, deactivateAtEnd));
    }

    IEnumerator Animate(float a1, float s1, float duration,
                        System.Func<float, float> ease, bool deactivateAtEnd)
    {
        float a0 = group.alpha;
        float s0 = window.localScale.x;

        // Opening: clickable right away. Closing: stop catching clicks immediately.
        group.interactable = !deactivateAtEnd;
        group.blocksRaycasts = !deactivateAtEnd;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = ease(t / duration);
            group.alpha = Mathf.Lerp(a0, a1, k);
            window.localScale = Vector3.one * Mathf.Lerp(s0, s1, k);
            yield return null;
        }

        group.alpha = a1;
        window.localScale = Vector3.one * s1;
        animRoutine = null;

        if (deactivateAtEnd)
        {
            window.localScale = Vector3.one; // reset so the editor/prefab isn't left shrunk
            gameObject.SetActive(false);
        }
    }

    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    static float EaseInCubic(float x) => x * x * x;
}