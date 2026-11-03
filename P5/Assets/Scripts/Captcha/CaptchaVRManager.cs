using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CaptchaVRManager : MonoBehaviour
{
    [SerializeField] int CaptchaSet;
    [SerializeField] Sprite[] CaptchaSprite;
    [SerializeField] Image CaptchaImg;

    IEnumerator Start()
    {
        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);

        SharedState.Instance.CaptchaSet.OnValueChanged += OnCaptchaChanged;
        OnCaptchaChanged(0, SharedState.Instance.CaptchaSet.Value); // handle the current value
    }

    void OnDestroy()
    {
        // Required: otherwise SharedState calls into destroyed objects after a scene switch
        if (SharedState.Instance != null)
            SharedState.Instance.CaptchaSet.OnValueChanged -= OnCaptchaChanged;
    }

    void OnCaptchaChanged(int oldInt, int newInt)
    {
        CaptchaSet = SharedState.Instance.CaptchaSet.Value;
        CaptchaImg.sprite = CaptchaSprite[CaptchaSet];
    }
}
