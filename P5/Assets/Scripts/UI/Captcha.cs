using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CaptchaSet
{
    public Sprite[] images = new Sprite[3];   // left, middle, right
    public int correctIndex;                  // 0, 1 or 2
}

public class Captcha : MonoBehaviour
{
    // Order must match CaptchaVRManager.CaptchaSprite:
    // CaptchaSprite[i] is the picture the VR player describes for sets[i]
    public CaptchaSet[] sets;

    [SerializeField] Image[] imageSlots;       // the 3 image buttons
    [SerializeField] AddressBar addressBar;    // on Window_Hack3rNet

    [Header("PC switch")]
    [SerializeField] GameObject currentPC;     // HackerPC
    [SerializeField] GameObject nextPC;        // BigMilkPC

    int currentSet = 0;
    DesktopWindow window;

    void Awake() => window = GetComponent<DesktopWindow>();

    // Runs every time the captcha window is opened (if Close() disables the GameObject)
    void OnEnable() => ShowSet();

    void ShowSet()
    {
        CaptchaSet set = sets[currentSet];

        for (int i = 0; i < imageSlots.Length; i++)
            imageSlots[i].sprite = set.images[i];
    }

    public void SelectImage(int index)
    {
        Debug.Log($"[Captcha] Picked {index}, set {currentSet}, correct is {sets[currentSet].correctIndex}");

        if (index == sets[currentSet].correctIndex)
        {
            // correct branch
            if (SharedState.Instance != null)
            {
                SharedState.Instance.SetCaptchaSolvedRpc(true);
                SharedState.Instance.ReportPuzzleEventRpc(PuzzleId.Captcha, PuzzleEvent.Solved);
            }

            nextPC.SetActive(true);
            currentPC.SetActive(false);
        }
        else
        {
            // wrong branch, first line inside the else
            if (SharedState.Instance != null)
                SharedState.Instance.ReportPuzzleEventRpc(PuzzleId.Captcha, PuzzleEvent.Fail);

            // Next set, wraps from the last back to the first
            currentSet = (currentSet + 1) % sets.Length;
            ShowSet(); // redraw now, don't rely on OnEnable

            if (SharedState.Instance != null)
                SharedState.Instance.SetCaptchaRpc(currentSet);
            else
                Debug.LogWarning("[Captcha] SharedState missing, VR won't get the new set");

            window.Close();
            addressBar.Retry("Wrong image. Enter the address again.");
        }
    }
}