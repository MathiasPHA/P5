using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CaptchaVRManager : MonoBehaviour
{
    // Order must match Captcha.sets on the PC
    [SerializeField] Sprite[] CaptchaSprite;
    [SerializeField] Image CaptchaImg;          // Image Type = Simple, with a ConveyorImage component
    [SerializeField] GameObject solvedObject;   // shown when the PC picks correctly (optional)

    [Header("Billboard roll")]
    [SerializeField] float rollDuration = 0.6f;
    [SerializeField] AnimationCurve rollCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] bool rollUp = true;        // false = new picture comes from the top
    [SerializeField] float gap = 0f;            // space between pictures while rolling (UI units)

    Image current, next;
    RectTransform currentRT, nextRT;
    ConveyorImage currentBelt, nextBelt;

    int shownIndex = -1;
    int pendingIndex = -1;
    Coroutine rollRoutine;
    bool subscribed;

    IEnumerator Start()
    {
        if (solvedObject != null) solvedObject.SetActive(false);
        SetupRollImages();

        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);

        var s = SharedState.Instance;
        s.CaptchaSet.OnValueChanged += OnCaptchaChanged;
        s.CaptchaSolved.OnValueChanged += OnSolvedChanged;
        subscribed = true;

        // Handle the current values (in case they changed before this scene loaded)
        OnCaptchaChanged(0, s.CaptchaSet.Value);
        OnSolvedChanged(false, s.CaptchaSolved.Value);
    }

    void OnDestroy()
    {
        // Required: otherwise SharedState calls into destroyed objects after a scene switch
        if (!subscribed || SharedState.Instance == null) return;
        SharedState.Instance.CaptchaSet.OnValueChanged -= OnCaptchaChanged;
        SharedState.Instance.CaptchaSolved.OnValueChanged -= OnSolvedChanged;
    }

    void SetupRollImages()
    {
        current = CaptchaImg;
        currentRT = current.rectTransform;

        // The belt warp lives on the image; add it if it wasn't set up in the Inspector
        currentBelt = CaptchaImg.GetComponent<ConveyorImage>();
        if (currentBelt == null) currentBelt = CaptchaImg.gameObject.AddComponent<ConveyorImage>();

        // Clone the image (including its ConveyorImage) so we have a second picture on the belt
        next = Instantiate(CaptchaImg, CaptchaImg.transform.parent);
        next.name = CaptchaImg.name + " (Roll)";
        nextRT = next.rectTransform;
        nextBelt = next.GetComponent<ConveyorImage>();
        next.gameObject.SetActive(false);

        var parent = CaptchaImg.transform.parent;
        if (parent != null && parent.TryGetComponent(out RectMask2D mask) && mask.enabled)
            Debug.LogWarning("CaptchaImg's parent has an active RectMask2D. Disable it, or it will clip the rollers.");
    }

    void OnCaptchaChanged(int oldIndex, int newIndex)
    {
        if (newIndex < 0 || newIndex >= CaptchaSprite.Length)
        {
            Debug.LogError($"Captcha index {newIndex} has no sprite. CaptchaSprite has {CaptchaSprite.Length} entries.");
            return;
        }

        // First picture: show instantly, no roll
        if (shownIndex < 0)
        {
            current.sprite = CaptchaSprite[newIndex];
            shownIndex = newIndex;
            return;
        }

        // Already rolling: remember the latest target and roll to it afterwards
        if (rollRoutine != null)
        {
            pendingIndex = newIndex;
            return;
        }

        if (newIndex == shownIndex) return;
        rollRoutine = StartCoroutine(Roll(newIndex));
    }

    IEnumerator Roll(int index)
    {
        float dist = (currentRT.rect.height + gap) * (rollUp ? 1f : -1f);

        next.sprite = CaptchaSprite[index];
        nextBelt.Offset = -dist;                 // starts hidden around the far roller
        next.gameObject.SetActive(true);

        for (float t = 0f; t < rollDuration; t += Time.deltaTime)
        {
            float k = rollCurve.Evaluate(t / rollDuration);
            currentBelt.Offset = dist * k;               // old picture rolls over the edge
            nextBelt.Offset = -dist * (1f - k);          // new picture rolls in from the other edge
            yield return null;
        }

        // Swap roles so "current" is always the visible one
        (current, next) = (next, current);
        (currentRT, nextRT) = (nextRT, currentRT);
        (currentBelt, nextBelt) = (nextBelt, currentBelt);
        currentBelt.Offset = 0f;
        nextBelt.Offset = 0f;
        next.gameObject.SetActive(false);
        shownIndex = index;
        rollRoutine = null;

        // A change arrived mid-roll
        if (pendingIndex >= 0)
        {
            int p = pendingIndex;
            pendingIndex = -1;
            if (p != shownIndex) rollRoutine = StartCoroutine(Roll(p));
        }
    }

    void OnSolvedChanged(bool oldValue, bool solved)
    {
        Debug.Log($"Captcha solved changed: {solved}");
    }
}