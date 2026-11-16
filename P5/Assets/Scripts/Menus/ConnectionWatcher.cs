using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Put on the NetworkManager GameObject in the Boot scene (it survives scene loads).
// Warns when the other player leaves and handles going back to the start screen.
//   PC (host):   shows a warning and waits for the VR player to reconnect.
//   VR (client): the host is gone, so it counts down and returns to the start screen.
public class ConnectionWatcher : MonoBehaviour
{
    [Tooltip("Scene to return to. Launcher routes each player to their own start screen")]
    [SerializeField] private string menuScene = "Launcher";
    [Tooltip("VR: seconds before returning to the start screen when the PC player leaves")]
    [SerializeField] private float vrReturnDelay = 8f;
    [SerializeField] private Color warningColor = new Color(1f, 0.35f, 0.3f);
    [Tooltip("Optional – leave empty for the default TextMeshPro font")]
    [SerializeField] private TMP_FontAsset font;

    private NetworkManager nm;
    private bool partnerConnected;   // host: is the VR player in?
    private bool connectedToHost;    // client: are we in a game?
    private bool returning;

    private GameObject warningCanvas;
    private TMP_Text titleText, messageText;

    void Start()
    {
        nm = NetworkManager.Singleton;
        nm.OnClientConnectedCallback += OnConnected;
        nm.OnClientDisconnectCallback += OnDisconnected;
        nm.OnTransportFailure += OnTransportFailure;
    }

    void OnDestroy()
    {
        if (nm == null) return;
        nm.OnClientConnectedCallback -= OnConnected;
        nm.OnClientDisconnectCallback -= OnDisconnected;
        nm.OnTransportFailure -= OnTransportFailure;
    }

    // ───────────────────────── Connection events ─────────────────────────

    private void OnConnected(ulong id)
    {
        if (nm.IsServer)
        {
            if (id == nm.LocalClientId) return;
            partnerConnected = true;
            HideWarning();
        }
        else if (id == nm.LocalClientId)
        {
            connectedToHost = true;
        }
    }

    private void OnDisconnected(ulong id)
    {
        if (returning) return;

        if (nm.IsServer)
        {
            if (id == nm.LocalClientId || !partnerConnected) return;
            partnerConnected = false;
            ShowWarning("VR PLAYER DISCONNECTED",
                        $"Waiting for them to reconnect...\nHost: {Bootstrap.HostInfo}",
                        withButton: true);
        }
        else
        {
            HostLost();
        }
    }

    private void OnTransportFailure()
    {
        if (!nm.IsServer) HostLost();
    }

    // Only reacts once actually in a game; failed connects in Boot are Bootstrap's job
    private void HostLost()
    {
        if (!connectedToHost || returning) return;
        connectedToHost = false;
        StartCoroutine(VrCountdown());
    }

    private IEnumerator VrCountdown()
    {
        for (float t = vrReturnDelay; t > 0f; t -= Time.unscaledDeltaTime)
        {
            ShowWarning("PC PLAYER DISCONNECTED",
                        $"Returning to start screen in {Mathf.CeilToInt(t)}...",
                        withButton: false);
            yield return null;
        }
        ReturnToMenu();
    }

    // ───────────────────────── Back to start screen ─────────────────────────

    public void ReturnToMenu()
    {
        if (returning) return;
        returning = true;
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        HideWarning();

        if (nm.IsListening) nm.Shutdown();
        float started = Time.unscaledTime;
        yield return new WaitUntil(() => !nm.ShutdownInProgress || Time.unscaledTime - started > 3f);

        // Remove everything that would be duplicated when Boot runs again
        var shared = FindAnyObjectByType<SharedState>();
        if (shared != null) Destroy(shared.gameObject);

        SceneManager.LoadScene(menuScene);
        Destroy(nm.gameObject);   // also removes this script, LanDiscovery and the overlay
    }

    // ───────────────────────── Warning UI ─────────────────────────

    private void ShowWarning(string title, string message, bool withButton)
    {
        if (warningCanvas == null) BuildWarning(withButton);
        titleText.text = title;
        messageText.text = message;
    }

    private void HideWarning()
    {
        if (warningCanvas != null) Destroy(warningCanvas);
        warningCanvas = null;
    }

    private void BuildWarning(bool withButton)
    {
        warningCanvas = new GameObject("DisconnectWarning", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = warningCanvas.GetComponent<Canvas>();
        var cam = Camera.main;

        if (!nm.IsServer && cam != null)
        {
            // VR: floating panel in front of the player's view
            canvas.renderMode = RenderMode.WorldSpace;
            warningCanvas.transform.SetParent(cam.transform, false);
            warningCanvas.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            warningCanvas.transform.localScale = Vector3.one * 0.001f;
            ((RectTransform)warningCanvas.transform).sizeDelta = new Vector2(900f, 360f);
        }
        else
        {
            // PC: on top of everything
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = warningCanvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            warningCanvas.transform.SetParent(transform, false);   // survives scene loads
        }

        var panel = CreateImage("Panel", warningCanvas.transform, new Color(0f, 0f, 0f, 0.88f));
        panel.rectTransform.sizeDelta = new Vector2(900f, withButton ? 360f : 300f);
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = warningColor;
        outline.effectDistance = new Vector2(3f, -3f);

        titleText = CreateText("Title", panel.transform, 54f, warningColor, FontStyles.Bold);
        Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f), 80f);

        messageText = CreateText("Message", panel.transform, 32f, Color.white, FontStyles.Normal);
        Place(messageText.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, withButton ? 10f : -20f), 110f);

        if (!withButton) return;

        var buttonImage = CreateImage("BackButton", panel.transform, new Color(0.15f, 0.15f, 0.15f, 1f));
        var buttonRt = buttonImage.rectTransform;
        buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0f);
        buttonRt.sizeDelta = new Vector2(420f, 70f);
        buttonRt.anchoredPosition = new Vector2(0f, 60f);

        var button = buttonImage.gameObject.AddComponent<Button>();
        button.onClick.AddListener(ReturnToMenu);

        var label = CreateText("Label", buttonRt, 30f, Color.white, FontStyles.Bold);
        Place(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, 0f);
        label.text = "BACK TO START SCREEN";
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        var img = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.color = color;
        return img;
    }

    private TMP_Text CreateText(string name, Transform parent, float size, Color color, FontStyles style)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    // Stretch horizontally between the anchors, with a fixed height (0 = stretch fully)
    private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, float height)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = height == 0f ? Vector2.zero : new Vector2(-40f, height);
    }
}