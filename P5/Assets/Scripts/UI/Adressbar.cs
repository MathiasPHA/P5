using System.Collections;
using TMPro;
using UnityEngine;

public class AddressBar : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    [SerializeField] string correctAddress = "192.168.0.1";
    [SerializeField] DesktopWindow captchaWindow;   // Window_Captcha
    [SerializeField] TMP_Text feedbackText;         // optional

    [Header("Wrong-address feedback")]
    [Tooltip("What shakes. Empty = the input field. Could also be the whole window.")]
    [SerializeField] RectTransform shakeTarget;
    [SerializeField] float shakeDuration = 0.35f;
    [SerializeField] float shakeDistance = 12f;     // pixels, side to side
    [SerializeField] float shakeSpeed = 50f;        // how fast it wobbles
    [Tooltip("Optional error sound")]
    [SerializeField] AudioClip wrongSound;
    [SerializeField, Range(0f, 1f)] float wrongVolume = 1f;

    AudioSource audioSource;
    Coroutine shakeRoutine;
    Vector2 shakeRestPos;

    void Awake()
    {
        if (shakeTarget == null) shakeTarget = input.GetComponent<RectTransform>();

        if (wrongSound != null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D, UI sound
        }
    }

    void OnEnable() => input.onSubmit.AddListener(Submit);

    void OnDisable()
    {
        input.onSubmit.RemoveListener(Submit);
        StopShake(); // don't leave the field offset if the window closes mid-shake
    }

    public void OnEnterButton() => Submit(input.text);

    void Submit(string text)
    {
        if (text.Trim() == correctAddress)
        {
            if (feedbackText) feedbackText.text = "";
            captchaWindow.Open();   // shows it and brings it to the front
        }
        else
        {
            // Wrong address counts as a captcha fail (same as a wrong image in Captcha.cs).
            // An empty submit (stray Enter) is not counted.
            if (text.Trim().Length > 0 && SharedState.Instance != null)
                SharedState.Instance.ReportPuzzleEventRpc(PuzzleId.Captcha, PuzzleEvent.Fail);

            if (feedbackText) feedbackText.text = "Connection failed";
            PlayWrongFeedback();
            input.text = "";
            input.ActivateInputField();
        }
    }

    public void Retry(string message)
    {
        GetComponentInParent<DesktopWindow>().Open();  // in case the window was closed
        if (feedbackText) feedbackText.text = message;
        input.text = "";
        input.ActivateInputField();             // cursor ready in the field
    }

    // ---------- Feedback ----------

    void PlayWrongFeedback()
    {
        if (audioSource != null) audioSource.PlayOneShot(wrongSound, wrongVolume);

        StopShake(); // restart cleanly if they spam Enter
        shakeRoutine = StartCoroutine(Shake());
    }

    IEnumerator Shake()
    {
        shakeRestPos = shakeTarget.anchoredPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float fade = 1f - t / shakeDuration;  // dies out towards the end
            float x = Mathf.Sin(t * shakeSpeed) * shakeDistance * fade;
            shakeTarget.anchoredPosition = shakeRestPos + new Vector2(x, 0f);
            yield return null;
        }
        shakeTarget.anchoredPosition = shakeRestPos;
        shakeRoutine = null;
    }

    void StopShake()
    {
        if (shakeRoutine == null) return;
        StopCoroutine(shakeRoutine);
        shakeTarget.anchoredPosition = shakeRestPos;
        shakeRoutine = null;
    }
}