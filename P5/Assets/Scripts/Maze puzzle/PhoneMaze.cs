using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PhoneMaze : MonoBehaviour
{
    public enum ViewMode { VRPhone, PCMap }

    [Header("Mode")]
    [SerializeField] private ViewMode mode = ViewMode.VRPhone;
    [Tooltip("Can be left empty if a DesktopSwitcher assigns it")]
    [SerializeField] private MazeLayout mazeLayout;
    [SerializeField] private bool resetOnWallHit = true;
    [SerializeField] private bool showPlayerOnPC = false;

    [Header("Testing")]
    [Tooltip("Arrow keys / WASD to move, Enter / Space to confirm (VR Phone mode only)")]
    [SerializeField] private bool keyboardInput = true;

    [Header("References")]
    [SerializeField] private RectTransform gridArea;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons (VR phone only) – wired and styled by this script, leave OnClick empty")]
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button enterButton;

    [Header("Board")]
    [SerializeField] private bool showGridLines = true;

    [Header("Colours")]
    [SerializeField] private Color boardColor = new Color(0.02f, 0.13f, 0.04f);
    [SerializeField] private Color traceColor = new Color(0.3f, 0.95f, 0.35f);
    [SerializeField] private Color tileColor = new Color(0.3f, 0.95f, 0.35f, 0.5f);
    [SerializeField] private Color wallColor = new Color(0.55f, 0.75f, 1f);
    [SerializeField] private Color playerColor = new Color(0.35f, 1f, 1f);
    [SerializeField] private Color exitColor = new Color(1f, 0.6f, 0.15f);
    [SerializeField] private Color hitColor = new Color(1f, 0.15f, 0.15f);

    [Header("Feel")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField, Range(0.05f, 0.3f)] private float wallThickness = 0.14f;
    [SerializeField] private float hitDuration = 0.6f;

    [Header("Events")]
    public UnityEvent onSolved;
    public UnityEvent onWallHit;

    private int cols, rows;
    private Vector2Int player;
    private bool solved;
    private float cell, hitTimer;

    private RectTransform playerRt, shipRt, playerGlowRt, exitRingRt;
    private Image shipImg, exitGlow;
    private readonly List<Image> borderBars = new List<Image>();
    private Vector2 visualPos, bump;

    private Sprite boardSprite, tileSprite, blockSprite, barSprite, glowSprite, arrowSprite, ringSprite, enterSprite;

    public Vector2Int PlayerCell => player;
    public MazeLayout Layout => mazeLayout;
    private bool HasPlayer => playerRt != null;
    private bool Ready => mazeLayout != null && exitRingRt != null;

    // ───────────────────────── Lifecycle ─────────────────────────

    void Awake()
    {
        GenerateSprites();
        if (mode == ViewMode.VRPhone) SetupButtons();
        if (statusText != null) statusText.color = traceColor;
        if (mazeLayout != null) Rebuild();
    }

    void OnEnable()
    {
        if (Ready) Restart();
    }

    void Update()
    {
        if (!Ready) return;

        if (mode == ViewMode.VRPhone && keyboardInput) HandleKeyboard();

        float t = Time.time;
        float dt = Time.deltaTime;

        exitRingRt.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
        SetAlpha(exitGlow, 0.45f + 0.25f * Mathf.Sin(t * 4f));

        if (!HasPlayer) return;

        visualPos = Vector2.MoveTowards(visualPos, CellPos(player), moveSpeed * cell * dt);
        bump = Vector2.Lerp(bump, Vector2.zero, dt * 15f);
        playerRt.anchoredPosition = visualPos + bump;

        float pulse = solved ? 1.5f + 0.35f * Mathf.Sin(t * 10f) : 1f + 0.08f * Mathf.Sin(t * 5f);
        playerGlowRt.localScale = Vector3.one * pulse;

        if (hitTimer > 0f)
        {
            hitTimer -= dt;
            bool on = Mathf.PingPong(hitTimer * 10f, 1f) > 0.5f;
            shipImg.color = on ? hitColor : playerColor;
            foreach (var bar in borderBars) bar.color = on ? hitColor : wallColor;

            if (hitTimer <= 0f)
            {
                shipImg.color = playerColor;
                foreach (var bar in borderBars) bar.color = wallColor;
                if (resetOnWallHit) Restart();
                else SetStatus("FIND THE EXIT");
            }
        }
    }

    // ───────────────────────── Public API ─────────────────────────

    public void SetLayout(MazeLayout layout)
    {
        if (layout == null) return;
        mazeLayout = layout;
        Rebuild();
        Restart();
    }

    public void Up()    => Move(new Vector2Int(0, -1));
    public void Down()  => Move(new Vector2Int(0, 1));
    public void Left()  => Move(new Vector2Int(-1, 0));
    public void Right() => Move(new Vector2Int(1, 0));

    public void Enter()
    {
        if (!Ready || mode != ViewMode.VRPhone || hitTimer > 0f) return;
        if (solved) { Restart(); return; }
        if (player != mazeLayout.exit) { SetStatus("NOT AT EXIT"); return; }

        solved = true;
        SetStatus("ACCESS GRANTED");
        onSolved.Invoke();
    }

    public void Restart()
    {
        if (!Ready) return;

        solved = false;
        hitTimer = 0f;
        player = mazeLayout.start;
        SetStatus(mode == ViewMode.PCMap ? "GUIDE THE AGENT" : "FIND THE EXIT");
        foreach (var bar in borderBars) bar.color = wallColor;

        if (!HasPlayer) return;
        visualPos = CellPos(player);
        bump = Vector2.zero;
        playerRt.anchoredPosition = visualPos;
        shipRt.localRotation = Quaternion.identity;
        shipImg.color = playerColor;
    }

    // For mirroring the VR player's position onto the PC map later (networking)
    public void SetPlayerCell(Vector2Int c) => player = c;

    // ───────────────────────── Input & movement ─────────────────────────

    private void HandleKeyboard()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k == null) return;

        if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Up();
        else if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Down();
        else if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Left();
        else if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Right();
        else if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Enter();
#else
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Up();
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Down();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) Left();
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Right();
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space)) Enter();
#endif
    }

    private void Move(Vector2Int dir)
    {
        if (!Ready || mode != ViewMode.VRPhone || solved || hitTimer > 0f || !HasPlayer) return;

        Vector2 screenDir = new Vector2(dir.x, -dir.y);
        shipRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-screenDir.x, screenDir.y) * Mathf.Rad2Deg);

        Vector2Int target = player + dir;

        if (!mazeLayout.InBounds(target))
        {
            bump = screenDir * cell * 0.18f;
            return;
        }

        if (mazeLayout.IsBlocked(player, dir))
        {
            bump = screenDir * cell * 0.35f;
            hitTimer = hitDuration;
            SetStatus(resetOnWallHit ? "CONNECTION LOST" : "BLOCKED");
            onWallHit.Invoke();
            return;
        }

        player = target;
        SetStatus(player == mazeLayout.exit ? "PRESS ENTER" : "FIND THE EXIT");
    }

    // ───────────────────────── Building ─────────────────────────

    private void Rebuild()
    {
        mazeLayout.EnsureSize();
        cols = mazeLayout.width;
        rows = mazeLayout.height;
        BuildBoard();
    }

    private void BuildBoard()
    {
        var layoutGroup = gridArea.GetComponent<GridLayoutGroup>();
        if (layoutGroup != null) { layoutGroup.enabled = false; Destroy(layoutGroup); }
        foreach (Transform child in gridArea) Destroy(child.gameObject);

        playerRt = shipRt = playerGlowRt = exitRingRt = null;
        shipImg = exitGlow = null;
        borderBars.Clear();

        // Fit the maze inside GridArea, keeping cells square
        cell = Mathf.Min(gridArea.rect.width / cols, gridArea.rect.height / rows);
        float th = cell * wallThickness;
        Vector2 boardSize = new Vector2(cols * cell, rows * cell);

        CreateImage("Board", gridArea, boardSprite, Color.white, boardSize, Vector2.zero);

        Vector2 tileSize = Vector2.one * (cell - th * 1.2f);
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < cols; x++)
            CreateImage("Tile", gridArea, tileSprite, tileColor, tileSize, CellPos(new Vector2Int(x, y)));

        var startColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.35f);
        CreateImage("Start", gridArea, tileSprite, startColor, tileSize, CellPos(mazeLayout.start));

        exitGlow = CreateImage("ExitGlow", gridArea, glowSprite, exitColor, Vector2.one * cell * 1.4f, CellPos(mazeLayout.exit));
        exitRingRt = CreateImage("ExitRing", gridArea, ringSprite, exitColor, Vector2.one * cell * 0.75f, CellPos(mazeLayout.exit)).rectTransform;

        if (mode == ViewMode.PCMap)
        {
            for (int y = 0; y < rows; y++)
            for (int x = 0; x < cols; x++)
            {
                Vector2 c = CellPos(new Vector2Int(x, y));
                int i = y * cols + x;

                if (x < cols - 1 && mazeLayout.wallRight[i])
                    CreateBar(c + new Vector2(cell / 2f, 0f), new Vector2(th, cell + th), th);

                if (y < rows - 1 && mazeLayout.wallBottom[i])
                    CreateBar(c + new Vector2(0f, -cell / 2f), new Vector2(cell + th, th), th);
            }
        }

        borderBars.Add(CreateBar(new Vector2(0f, boardSize.y / 2f), new Vector2(boardSize.x + th, th), th));
        borderBars.Add(CreateBar(new Vector2(0f, -boardSize.y / 2f), new Vector2(boardSize.x + th, th), th));
        borderBars.Add(CreateBar(new Vector2(-boardSize.x / 2f, 0f), new Vector2(th, boardSize.y + th), th));
        borderBars.Add(CreateBar(new Vector2(boardSize.x / 2f, 0f), new Vector2(th, boardSize.y + th), th));

        if (mode == ViewMode.PCMap && !showPlayerOnPC) return;

        playerRt = new GameObject("Player", typeof(RectTransform)).GetComponent<RectTransform>();
        playerRt.SetParent(gridArea, false);
        playerRt.sizeDelta = Vector2.one * cell;

        var glowColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.6f);
        playerGlowRt = CreateImage("Glow", playerRt, glowSprite, glowColor, Vector2.one * cell * 1.3f, Vector2.zero).rectTransform;
        shipImg = CreateImage("Ship", playerRt, arrowSprite, playerColor, Vector2.one * cell * 0.5f, Vector2.zero);
        shipRt = shipImg.rectTransform;
    }

    private Image CreateBar(Vector2 pos, Vector2 size, float thickness)
    {
        var img = CreateImage("Wall", gridArea, barSprite, wallColor, size, pos);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 24f / (thickness * 0.5f);
        return img;
    }

    private void SetupButtons()
    {
        SetupButton(upButton, Up, arrowSprite, 0f);
        SetupButton(downButton, Down, arrowSprite, 180f);
        SetupButton(leftButton, Left, arrowSprite, 90f);
        SetupButton(rightButton, Right, arrowSprite, -90f);
        SetupButton(enterButton, Enter, enterSprite, 0f);
    }

    private void SetupButton(Button button, UnityAction action, Sprite icon, float angle)
    {
        if (button == null) return;

        button.onClick.AddListener(() =>
        {
            action();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        });

        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        foreach (var label in button.GetComponentsInChildren<Text>(true)) label.gameObject.SetActive(false);

        var bg = button.GetComponent<Image>();
        if (bg != null)
        {
            bg.sprite = blockSprite;
            bg.type = Image.Type.Simple;
            bg.color = Color.white;
            button.targetGraphic = bg;
        }

        var colors = button.colors;
        colors.normalColor = wallColor;
        colors.highlightedColor = Color.Lerp(wallColor, Color.white, 0.35f);
        colors.selectedColor = wallColor;
        colors.pressedColor = traceColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        var rt = (RectTransform)button.transform;
        float s = Mathf.Min(rt.rect.width, rt.rect.height) * 0.45f;
        var iconImg = CreateImage("Icon", button.transform, icon, new Color(0.05f, 0.1f, 0.2f), Vector2.one * s, Vector2.zero);
        iconImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ───────────────────────── Helpers ─────────────────────────

    private Vector2 CellPos(Vector2Int p) =>
        new Vector2((p.x + 0.5f - cols / 2f) * cell, (rows / 2f - p.y - 0.5f) * cell);

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private static void SetAlpha(Image img, float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // ───────────────────────── Generated graphics ─────────────────────────

    private void GenerateSprites()
    {
        boardSprite = MakeBoard(512);

        tileSprite = Procedural(64, p =>
        {
            float d = RoundRect(p, 32f, 30f, 6f);
            if (d > 0.5f) return Color.clear;
            float a = d > -3f ? Mathf.Clamp01(0.5f - d) : 0.15f;
            return new Color(1f, 1f, 1f, a);
        });

        blockSprite = Procedural(128, p =>
        {
            float d = RoundRect(p, 64f, 60f, 14f);
            if (d > 0.5f) return Color.clear;

            float v;
            if (d > -3f) v = 0.12f;
            else if (d > -8f) v = 1f;
            else
            {
                float d2 = RoundRect(p, 64f, 42f, 8f);
                if (d2 > 0f) v = 0.72f;
                else if (d2 > -3f) v = 0.95f;
                else v = ((int)p.x % 14 < 4) ? 0.45f : 0.6f;
            }
            return new Color(v, v, v, Mathf.Clamp01(0.5f - d));
        });

        barSprite = Procedural(64, p =>
        {
            float d = RoundRect(p, 32f, 31f, 12f);
            if (d > 0.5f) return Color.clear;
            float v = d > -3f ? 0.12f : d > -8f ? 1f : 0.7f;
            return new Color(v, v, v, Mathf.Clamp01(0.5f - d));
        }, new Vector4(24f, 24f, 24f, 24f));

        glowSprite = Procedural(64, p =>
        {
            float a = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(32f, 32f)) / 32f);
            return new Color(1f, 1f, 1f, a * a);
        });

        arrowSprite = Procedural(64, p =>
        {
            Vector2 a = new Vector2(32f, 56f), b = new Vector2(10f, 12f), c = new Vector2(54f, 12f);
            float d = Mathf.Min(Edge(a, b, p), Mathf.Min(Edge(b, c, p), Edge(c, a, p)));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(d + 0.5f));
        });

        ringSprite = Procedural(64, p =>
        {
            Vector2 v = p - new Vector2(32f, 32f);
            float ang = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (Mathf.Repeat(ang + 10f, 90f) < 20f) return Color.clear;
            float d = Mathf.Abs(v.magnitude - 22f) - 4f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
        });

        enterSprite = Procedural(64, p =>
        {
            float r = Vector2.Distance(p, new Vector2(32f, 32f));
            float a = Mathf.Max(Mathf.Clamp01(12.5f - r), Mathf.Clamp01(0.5f - (Mathf.Abs(r - 22f) - 3f)));
            return new Color(1f, 1f, 1f, a);
        });
    }

    private Sprite MakeBoard(int s)
    {
        var px = new Color[s * s];
        Color fine = Color.Lerp(boardColor, traceColor, 0.08f);
        Color major = Color.Lerp(boardColor, traceColor, 0.16f);

        for (int y = 0; y < s; y++)
        for (int x = 0; x < s; x++)
        {
            Color c = boardColor;
            if (showGridLines)
            {
                if (x % 16 == 0 || y % 16 == 0) c = fine;
                if (x % 64 == 0 || y % 64 == 0) c = major;
            }
            px[y * s + x] = c;
        }

        var tex = NewTexture(s);
        tex.SetPixels(px);
        tex.Apply();
        return ToSprite(tex, Vector4.zero);
    }

    private static Sprite Procedural(int size, System.Func<Vector2, Color> f, Vector4 border = default)
    {
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            px[y * size + x] = f(new Vector2(x + 0.5f, y + 0.5f));

        var tex = NewTexture(size);
        tex.SetPixels(px);
        tex.Apply();
        return ToSprite(tex, border);
    }

    private static Texture2D NewTexture(int size) =>
        new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

    private static Sprite ToSprite(Texture2D tex, Vector4 border) =>
        Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                      100f, 0, SpriteMeshType.FullRect, border);

    private static float RoundRect(Vector2 p, float center, float half, float radius)
    {
        Vector2 q = new Vector2(Mathf.Abs(p.x - center), Mathf.Abs(p.y - center)) - Vector2.one * (half - radius);
        return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude
               + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
    }

    private static float Edge(Vector2 a, Vector2 b, Vector2 p)
    {
        Vector2 e = b - a;
        return (e.x * (p.y - a.y) - e.y * (p.x - a.x)) / e.magnitude;
    }
}