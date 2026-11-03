using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class DesktopSwitcher : MonoBehaviour
{
    [Header("Mazes – one per desktop")]
    [SerializeField] private MazeLayout[] desktops;
    [SerializeField] private int startIndex = 0;

    [Header("Targets")]
    [SerializeField] private PhoneMaze pcMap;
    [Tooltip("Optional: also switch the VR phone to the same maze")]
    [SerializeField] private PhoneMaze phone;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text desktopLabel;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    [Header("Input")]
    [Tooltip("Q / E = previous / next,  1–9 = jump to desktop")]
    [SerializeField] private bool keyboardInput = true;

    public int Current { get; private set; }

    void Start()
    {
        if (previousButton != null) previousButton.onClick.AddListener(Previous);
        if (nextButton != null) nextButton.onClick.AddListener(Next);
        Show(startIndex);
    }

    void Update()
    {
        if (!keyboardInput || desktops == null || desktops.Length == 0) return;

#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;

        if (k.qKey.wasPressedThisFrame) Previous();
        else if (k.eKey.wasPressedThisFrame) Next();
        else
        {
            for (int i = 0; i < Mathf.Min(9, desktops.Length); i++)
                if (k[Key.Digit1 + i].wasPressedThisFrame) { Show(i); break; }
        }
#else
        if (Input.GetKeyDown(KeyCode.Q)) Previous();
        else if (Input.GetKeyDown(KeyCode.E)) Next();
        else
        {
            for (int i = 0; i < Mathf.Min(9, desktops.Length); i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) { Show(i); break; }
        }
#endif
    }

    public void Next() => Show(Current + 1);
    public void Previous() => Show(Current - 1);

    public void Show(int index)
    {
        if (desktops == null || desktops.Length == 0) return;

        int n = desktops.Length;
        Current = ((index % n) + n) % n;

        if (pcMap != null) pcMap.SetLayout(desktops[Current]);
        if (phone != null) phone.SetLayout(desktops[Current]);

        if (desktopLabel != null) desktopLabel.text = $"DESKTOP {Current + 1} / {n}";
    }
}