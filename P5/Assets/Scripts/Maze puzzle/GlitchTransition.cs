using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Short glitch effect for UI: jitter, flicker and torn horizontal slices.
// The callback passed to Play() runs at the midpoint, hidden by the glitch.
[RequireComponent(typeof(RectTransform))]
public class GlitchTransition : MonoBehaviour
{
    [Tooltip("What shakes and flickers. Leave empty to use this object")]
    [SerializeField] private RectTransform target;
    [SerializeField] private float duration = 0.35f;
    [Tooltip("How often the glitch pattern changes (seconds)")]
    [SerializeField] private float stepTime = 0.03f;
    [Tooltip("How far the screen jumps sideways at the peak (canvas pixels)")]
    [SerializeField] private float jitter = 40f;
    [SerializeField, Range(0, 30)] private int sliceCount = 14;
    [SerializeField] private Color[] sliceColors =
    {
        new Color(0.3f, 0.95f, 0.35f),
        new Color(0.9f, 1f, 0.9f),
        new Color(0f, 0f, 0f)
    };
    [Tooltip("Optional glitch sound")]
    [SerializeField] private AudioSource glitchSound;

    private readonly List<RectTransform> slices = new List<RectTransform>();
    private readonly List<Image> sliceImages = new List<Image>();
    private RectTransform overlay;
    private CanvasGroup group;
    private Vector2 restPos;
    private Coroutine running;

    public bool IsPlaying => running != null;

    void Awake()
    {
        if (target == null) target = (RectTransform)transform;
        if (!target.TryGetComponent(out group)) group = target.gameObject.AddComponent<CanvasGroup>();
        restPos = target.anchoredPosition;
        BuildOverlay();
    }

    void OnDisable()
    {
        if (running == null) return;
        StopCoroutine(running);
        running = null;
        ResetVisuals();
    }

    public void Play(Action onMidpoint)
    {
        if (running != null)
        {
            StopCoroutine(running);
            ResetVisuals();
        }
        running = StartCoroutine(Run(onMidpoint));
    }

    private IEnumerator Run(Action onMidpoint)
    {
        overlay.gameObject.SetActive(true);
        overlay.SetAsLastSibling();
        if (glitchSound != null) glitchSound.Play();

        float t = 0f, nextStep = 0f;
        bool swapped = false;

        while (t < duration)
        {
            float progress = t / duration;

            if (!swapped && progress >= 0.5f)
            {
                swapped = true;
                onMidpoint?.Invoke();
                group.alpha = 0f;          // one blank frame hides the rebuild
                nextStep = t + stepTime;
            }
            else if (t >= nextStep)
            {
                Step(progress);
                nextStep += stepTime;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!swapped) onMidpoint?.Invoke();
        ResetVisuals();
        running = null;
    }

    // One frame of glitch; strongest in the middle of the transition
    private void Step(float progress)
    {
        float intensity = 1f - Mathf.Abs(progress * 2f - 1f);

        target.anchoredPosition = restPos + new Vector2(UnityEngine.Random.Range(-1f, 1f) * jitter * intensity, 0f);
        group.alpha = UnityEngine.Random.Range(1f - 0.6f * intensity, 1f);

        float height = overlay.rect.height;
        for (int i = 0; i < slices.Count; i++)
        {
            bool on = UnityEngine.Random.value < 0.25f + 0.65f * intensity;
            slices[i].gameObject.SetActive(on);
            if (!on) continue;

            float y = UnityEngine.Random.value;
            slices[i].anchorMin = new Vector2(0f, y);
            slices[i].anchorMax = new Vector2(1f, y);
            slices[i].sizeDelta = new Vector2(0f, UnityEngine.Random.Range(0.005f, 0.06f) * height);
            slices[i].anchoredPosition = new Vector2(UnityEngine.Random.Range(-2f, 2f) * jitter * intensity, 0f);

            var c = sliceColors[UnityEngine.Random.Range(0, sliceColors.Length)];
            c.a = UnityEngine.Random.Range(0.35f, 0.9f);
            sliceImages[i].color = c;
        }
    }

    private void ResetVisuals()
    {
        target.anchoredPosition = restPos;
        group.alpha = 1f;
        overlay.gameObject.SetActive(false);
    }

    private void BuildOverlay()
    {
        overlay = new GameObject("GlitchOverlay", typeof(RectTransform)).GetComponent<RectTransform>();
        overlay.gameObject.layer = target.gameObject.layer;
        overlay.SetParent(target, false);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;

        for (int i = 0; i < sliceCount; i++)
        {
            var go = new GameObject($"Slice_{i}", typeof(RectTransform), typeof(Image));
            go.layer = target.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(overlay, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;

            slices.Add(rt);
            sliceImages.Add(img);
        }

        overlay.gameObject.SetActive(false);
    }
}