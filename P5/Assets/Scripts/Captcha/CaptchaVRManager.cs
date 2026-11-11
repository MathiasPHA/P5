using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CaptchaVRManager : MonoBehaviour
{
    // Order must match Captcha.sets on the PC
    [SerializeField] Sprite[] CaptchaSprite;
    [SerializeField] Image CaptchaImg;
    [SerializeField] GameObject solvedObject;   // shown when the PC picks correctly (optional)

    bool subscribed;

    IEnumerator Start()
    {
        if (solvedObject != null) solvedObject.SetActive(false);

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

    void OnCaptchaChanged(int oldIndex, int newIndex)
    {
        if (newIndex < 0 || newIndex >= CaptchaSprite.Length)
        {
            Debug.LogError($"Captcha index {newIndex} has no sprite. CaptchaSprite has {CaptchaSprite.Length} entries.");
            return;
        }
        CaptchaImg.sprite = CaptchaSprite[newIndex];
    }

    void OnSolvedChanged(bool oldValue, bool solved)
    {
        CaptchaImg.gameObject.SetActive(!solved);
        if (solvedObject != null) solvedObject.SetActive(solved);
    }
}