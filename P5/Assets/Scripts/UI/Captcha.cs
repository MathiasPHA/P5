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
    public CaptchaSet[] sets;        // your 3 sets

    [SerializeField] Image[] imageSlots;       // the 3 image buttons
    [SerializeField] AddressBar addressBar;    // on Window_Hack3rNet

    [Header("PC switch")]
    [SerializeField] GameObject currentPC;     // HackerPC
    [SerializeField] GameObject nextPC;        // BigMilkPC

    int currentSet = 0;
    DesktopWindow window;

    void Awake() => window = GetComponent<DesktopWindow>();

    // Runs every time the captcha window is opened
    void OnEnable() => ShowSet();

    void ShowSet()
    {
        CaptchaSet set = sets[currentSet];

        for (int i = 0; i < imageSlots.Length; i++)
            imageSlots[i].sprite = set.images[i];
    }

    public void SelectImage(int index)
    {
        if (index == sets[currentSet].correctIndex)
        {
            nextPC.SetActive(true);
            currentPC.SetActive(false);
        }
        else
        {
            // next set, wraps from the last back to the first
            currentSet = (currentSet + 1) % sets.Length;
            SharedState.Instance.SetCaptchaRpc(currentSet);

            window.Close();
            addressBar.Retry("Wrong image. Enter the address again.");
        }
    }
}