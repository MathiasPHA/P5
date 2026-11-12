using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Adds a CCTV recording overlay (blinking REC dot, camera name, timestamp)
// to the top-left corner of the UI element it is placed on.
[RequireComponent(typeof(RectTransform))]
public class CCTVOverlay : MonoBehaviour
{
    [SerializeField] private string cameraName = "CAM 03";
    [SerializeField] private string timeFormat = "dd/MM/yyyy  HH:mm:ss";
    [SerializeField] private float fontSize = 28f;
    [Tooltip("Distance from the top-left corner")]
    [SerializeField] private Vector2 padding = new Vector2(24f, 20f);
    [SerializeField] private Color textColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color recColor = new Color(1f, 0.2f, 0.2f);
    [Tooltip("Seconds the REC dot stays on / off")]
    [SerializeField] private float blinkInterval = 0.6f;
    [Tooltip("Optional – leave empty for the default TextMeshPro font")]
    [SerializeField] private TMP_FontAsset font;

    private static Sprite circleSprite;
    private Image recDot;
    private TMP_Text info;
    private int lastSecond = -1;

    void Awake() => Build();

    void Update()
    {
        recDot.enabled = Mathf.Repeat(Time.unscaledTime, blinkInterval * 2f) < blinkInterval;

        var now = DateTime.Now;
        if (now.Second == lastSecond) return;

        lastSecond = now.Second;
        info.text = $"REC\n{cameraName}\n{now.ToString(timeFormat)}";
    }

    private void Build()
    {
        var root = new GameObject("CCTV Overlay", typeof(RectTransform)).GetComponent<RectTransform>();
        root.gameObject.layer = gameObject.layer;
        root.SetParent(transform, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;

        // Blinking red dot, centred on the first text line
        float dotSize = fontSize * 0.6f;
        recDot = new GameObject("RecDot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var dotRt = recDot.rectTransform;
        dotRt.SetParent(root, false);
        dotRt.anchorMin = dotRt.anchorMax = new Vector2(0f, 1f);
        dotRt.pivot = new Vector2(0f, 0.5f);
        dotRt.sizeDelta = Vector2.one * dotSize;
        dotRt.anchoredPosition = new Vector2(padding.x, -(padding.y + fontSize * 0.62f));
        recDot.sprite = GetCircleSprite();
        recDot.color = recColor;
        recDot.raycastTarget = false;

        // REC / camera name / timestamp
        info = new GameObject("Info", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var infoRt = info.rectTransform;
        infoRt.SetParent(root, false);
        infoRt.anchorMin = infoRt.anchorMax = infoRt.pivot = new Vector2(0f, 1f);
        infoRt.anchoredPosition = new Vector2(padding.x + dotSize + 12f, -padding.y);
        infoRt.sizeDelta = new Vector2(600f, fontSize * 4f);

        if (font != null) info.font = font;
        info.fontSize = fontSize;
        info.color = textColor;
        info.alignment = TextAlignmentOptions.TopLeft;
        info.textWrappingMode = TextWrappingModes.NoWrap;
        info.raycastTarget = false;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 64;
        var px = new Color[size * size];
        var center = new Vector2(size / 2f, size / 2f);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(size / 2f - 1f - dist + 0.5f));
        }

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        tex.SetPixels(px);
        tex.Apply();

        circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }
}