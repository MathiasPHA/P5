using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adds a CCTV recording overlay (blinking REC dot, camera name, timestamp)
// to the top-left corner of the UI element it is placed on.
[RequireComponent(typeof(RectTransform))]
public class TaskBar : MonoBehaviour
{
    [SerializeField] private string timeFormat = "dd/MM/yyyy  HH:mm:ss";
    [SerializeField] private float fontSize = 28f;
    [Tooltip("Distance from the top-left corner")]
    [SerializeField] private Color textColor = new Color(1f, 1f, 1f, 0.9f);
    [Tooltip("Optional – leave empty for the default TextMeshPro font")]
    [SerializeField] private TMP_FontAsset font;

    private static Sprite circleSprite;
    private Image recDot;
    private TMP_Text info;
    private int lastSecond = -1;

    void Awake() => Build();

    void Update()
    {

        var now = DateTime.Now;
        if (now.Second == lastSecond) return;

        lastSecond = now.Second;
        info.text = $"\n{now.ToString(timeFormat)}";
    }

    private void Build()
    {
        var root = new GameObject("CCTV Overlay", typeof(RectTransform)).GetComponent<RectTransform>();
        root.gameObject.layer = gameObject.layer;
        root.SetParent(transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        // REC / camera name / timestamp
        info = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var infoRt = info.rectTransform;
        infoRt.SetParent(root, false);
        infoRt.anchorMin = infoRt.anchorMax = infoRt.pivot = new Vector2(0f, 1f);
        infoRt.sizeDelta = new Vector2(600f, fontSize * 4f);

        if (font != null) info.font = font;
        info.fontSize = fontSize;
        info.color = textColor;
        info.alignment = TextAlignmentOptions.TopLeft;
        info.textWrappingMode = TextWrappingModes.NoWrap;
        info.raycastTarget = false;
    }

}