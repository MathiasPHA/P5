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

    private static readonly Vector2Int DirUp    = new Vector2Int(0, -1);
    private static readonly Vector2Int DirDown  = new Vector2Int(0, 1);
    private static readonly Vector2Int DirLeft  = new Vector2Int(-1, 0);
    private static readonly Vector2Int DirRight = new Vector2Int(1, 0);

    [Header("Mode")]
    [SerializeField] private ViewMode mode = ViewMode.VRPhone;
    [Tooltip("Can be left empty if a DesktopSwitcher assigns it")]
    [SerializeField] private MazeLayout mazeLayout;
    [SerializeField] private bool resetOnWallHit = true;
    [Tooltip("PC Map only: show the agent's position")]
    [SerializeField] private bool showPlayerOnPC = false;
    [Tooltip("PC Map only: show the start tile and exit marker")]
    [SerializeField] private bool showStartAndExitOnPC = true;
    [Tooltip("VR Phone only: arrow keys / WASD to move, Enter / Space to confirm")]
    [SerializeField] private bool keyboardInput = true;

    [Header("References")]
    [SerializeField] private RectTransform gridArea;
    [Tooltip("Row of symbols from the Maze Layout")]
    [SerializeField] private RectTransform symbolArea;
    [SerializeField] private float symbolSpacing = 30f;
    [Tooltip("Optional status messages")]
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
    [SerializeField] private Color wallColor = new Color(0.7f, 0.4f, 1f);
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
    private float cell, hitTimer;
    private bool built, solved;
    private Vector2Int player;
    private Vector2 visualPos, bump;

    private Sprite boardSprite;
    private RectTransform playerRt, shipRt, playerGlowRt, exitRingRt;
    private Image shipImg, exitGlow;
    private readonly List<Image> borderBars = new List<Image>();
    private readonly List<Image> symbolImages = new List<Image>();

    public Vector2Int PlayerCell => player;
    private bool Ready => built && mazeLayout != null;
    private bool IsPhone => mode == ViewMode.VRPhone;
    private bool HasPlayer => playerRt != null;
    private bool CanMove => Ready && IsPhone && HasPlayer && !solved && hitTimer <= 0f;

    // ───────────────────────── Lifecycle ─────────────────────────

    void Awake()
    {
        boardSprite = MazeGraphics.Board(512, boardColor, traceColor, showGridLines);
        if (IsPhone) SetupButtons();
        if (statusText != null) statusText.color = traceColor;
        if (mazeLayout != null) Rebuild();
    }

    void OnEnable() => Restart();

    void Update()
    {
        if (!Ready) return;
        if (IsPhone && keyboardInput) HandleKeyboard();

        AnimateExit();
        if (!HasPlayer) return;

        AnimatePlayer();
        UpdateHitFlash();
    }

    // ───────────────────────── Public API ─────────────────────────

    public void SetLayout(MazeLayout layout)
    {
        if (layout == null) return;
        mazeLayout = layout;
        Rebuild();
        Restart();
    }

    public void Up()    => Move(DirUp);
    public void Down()  => Move(DirDown);
    public void Left()  => Move(DirLeft);
    public void Right() => Move(DirRight);

    public void Enter()
    {
        if (!Ready || !IsPhone || hitTimer > 0f) return;
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
        SetStatus(IsPhone ? "FIND THE EXIT" : "GUIDE THE AGENT");
        SetAlert(false);

        if (!HasPlayer) return;
        visualPos = CellPos(player);
        bump = Vector2.zero;
        playerRt.anchoredPosition = visualPos;
        shipRt.localRotation = Quaternion.identity;
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
        if (!CanMove) return;

        Vector2 screenDir = new Vector2(dir.x, -dir.y);
        shipRt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-screenDir.x, screenDir.y) * Mathf.Rad2Deg);

        // Edge of the grid: just bump, no penalty
        if (!mazeLayout.InBounds(player + dir))
        {
            bump = screenDir * cell * 0.18f;
            return;
        }

        // Hidden wall: flash red, then reset
        if (mazeLayout.IsBlocked(player, dir))
        {
            bump = screenDir * cell * 0.35f;
            hitTimer = hitDuration;
            SetStatus(resetOnWallHit ? "CONNECTION LOST" : "BLOCKED");
            onWallHit.Invoke();
            return;
        }

        player += dir;
        SetStatus(player == mazeLayout.exit ? "PRESS ENTER" : "FIND THE EXIT");
    }

    // ───────────────────────── Animation ─────────────────────────

    private void AnimateExit()
    {
        if (exitRingRt == null) return;

        exitRingRt.localRotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
        var c = exitGlow.color;
        c.a = 0.45f + 0.25f * Mathf.Sin(Time.time * 4f);
        exitGlow.color = c;
    }

    private void AnimatePlayer()
    {
        float dt = Time.deltaTime;
        visualPos = Vector2.MoveTowards(visualPos, CellPos(player), moveSpeed * cell * dt);
        bump = Vector2.Lerp(bump, Vector2.zero, dt * 15f);
        playerRt.anchoredPosition = visualPos + bump;

        float t = Time.time;
        float pulse = solved ? 1.5f + 0.35f * Mathf.Sin(t * 10f) : 1f + 0.08f * Mathf.Sin(t * 5f);
        playerGlowRt.localScale = Vector3.one * pulse;
    }

    private void UpdateHitFlash()
    {
        if (hitTimer <= 0f) return;

        hitTimer -= Time.deltaTime;
        SetAlert(Mathf.PingPong(hitTimer * 10f, 1f) > 0.5f);
        if (hitTimer > 0f) return;

        SetAlert(false);
        if (resetOnWallHit) Restart();
        else SetStatus("FIND THE EXIT");
    }

    private void SetAlert(bool on)
    {
        if (shipImg != null) shipImg.color = on ? hitColor : playerColor;
        foreach (var b in borderBars) b.color = on ? hitColor : wallColor;
        foreach (var s in symbolImages) s.color = on ? hitColor : traceColor;
    }

    // ───────────────────────── Building ─────────────────────────

    private void Rebuild()
    {
        mazeLayout.EnsureSize();
        cols = mazeLayout.width;
        rows = mazeLayout.height;
        cell = Mathf.Min(gridArea.rect.width / cols, gridArea.rect.height / rows);

        Clear(gridArea);
        borderBars.Clear();
        playerRt = shipRt = playerGlowRt = exitRingRt = null;
        shipImg = exitGlow = null;

        float th = cell * wallThickness;
        BuildTiles(th);
        if (IsPhone || showStartAndExitOnPC) BuildMarkers(th);
        if (!IsPhone) BuildWalls(th);
        BuildBorder(th);
        if (IsPhone || showPlayerOnPC) BuildPlayer();
        BuildSymbols();

        built = true;
    }

    private void BuildTiles(float th)
    {
        CreateImage("Board", gridArea, boardSprite, Color.white, new Vector2(cols, rows) * cell, Vector2.zero);

        Vector2 size = Vector2.one * (cell - th * 1.2f);
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < cols; x++)
            CreateImage("Tile", gridArea, MazeGraphics.Tile, tileColor, size, CellPos(new Vector2Int(x, y)));
    }

    private void BuildMarkers(float th)
    {
        var startColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.35f);
        CreateImage("Start", gridArea, MazeGraphics.Tile, startColor, Vector2.one * (cell - th * 1.2f), CellPos(mazeLayout.start));

        Vector2 exitPos = CellPos(mazeLayout.exit);
        exitGlow = CreateImage("ExitGlow", gridArea, MazeGraphics.Glow, exitColor, Vector2.one * cell * 1.4f, exitPos);
        exitRingRt = CreateImage("ExitRing", gridArea, MazeGraphics.Ring, exitColor, Vector2.one * cell * 0.75f, exitPos).rectTransform;
    }

    private void BuildWalls(float th)
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

    private void BuildBorder(float th)
    {
        float w = cols * cell, h = rows * cell;
        borderBars.Add(CreateBar(new Vector2(0f,  h / 2f), new Vector2(w + th, th), th));
        borderBars.Add(CreateBar(new Vector2(0f, -h / 2f), new Vector2(w + th, th), th));
        borderBars.Add(CreateBar(new Vector2(-w / 2f, 0f), new Vector2(th, h + th), th));
        borderBars.Add(CreateBar(new Vector2( w / 2f, 0f), new Vector2(th, h + th), th));
    }

    private void BuildPlayer()
    {
        playerRt = new GameObject("Player", typeof(RectTransform)).GetComponent<RectTransform>();
        playerRt.SetParent(gridArea, false);
        playerRt.sizeDelta = Vector2.one * cell;

        var glowColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0.6f);
        playerGlowRt = CreateImage("Glow", playerRt, MazeGraphics.Glow, glowColor, Vector2.one * cell * 1.3f, Vector2.zero).rectTransform;
        shipImg = CreateImage("Ship", playerRt, MazeGraphics.Arrow, playerColor, Vector2.one * cell * 0.5f, Vector2.zero);
        shipRt = shipImg.rectTransform;
    }

    private void BuildSymbols()
    {
        symbolImages.Clear();
        if (symbolArea == null) return;
        Clear(symbolArea);

        var symbols = mazeLayout.symbols;
        if (symbols == null || symbols.Length == 0) return;

        int n = symbols.Length;
        float size = Mathf.Min(symbolArea.rect.height, (symbolArea.rect.width - symbolSpacing * (n - 1)) / n);
        float firstX = -(n * size + (n - 1) * symbolSpacing) / 2f + size / 2f;

        for (int i = 0; i < n; i++)
        {
            var img = CreateImage($"Symbol_{i + 1}", symbolArea, symbols[i], traceColor,
                                  Vector2.one * size, new Vector2(firstX + i * (size + symbolSpacing), 0f));
            img.preserveAspect = true;
            img.enabled = symbols[i] != null;
            symbolImages.Add(img);
        }
    }

    private Image CreateBar(Vector2 pos, Vector2 size, float thickness)
    {
        var img = CreateImage("Wall", gridArea, MazeGraphics.Bar, wallColor, size, pos);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 24f / (thickness * 0.5f);
        return img;
    }

    // ───────────────────────── Buttons ─────────────────────────

    private void SetupButtons()
    {
        SetupButton(upButton,    Up,    MazeGraphics.Arrow, 0f);
        SetupButton(downButton,  Down,  MazeGraphics.Arrow, 180f);
        SetupButton(leftButton,  Left,  MazeGraphics.Arrow, 90f);
        SetupButton(rightButton, Right, MazeGraphics.Arrow, -90f);
        SetupButton(enterButton, Enter, MazeGraphics.EnterIcon, 0f);
    }

    private void SetupButton(Button button, UnityAction action, Sprite icon, float angle)
    {
        if (button == null) return;

        // Deselect after click so Enter/Space don't re-trigger the last clicked button
        button.onClick.AddListener(() =>
        {
            action();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        });

        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true)) label.gameObject.SetActive(false);
        foreach (var label in button.GetComponentsInChildren<Text>(true)) label.gameObject.SetActive(false);

        if (button.TryGetComponent(out Image bg))
        {
            bg.sprite = MazeGraphics.Block;
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
        var iconImg = CreateImage("Icon", button.transform, icon, new Color(0.08f, 0.04f, 0.15f), Vector2.one * s, Vector2.zero);
        iconImg.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ───────────────────────── Helpers ─────────────────────────

    private Vector2 CellPos(Vector2Int p) =>
        new Vector2((p.x + 0.5f - cols / 2f) * cell, (rows / 2f - p.y - 0.5f) * cell);

    private void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
    }

    private static void Clear(Transform parent)
    {
        foreach (Transform child in parent) Destroy(child.gameObject);
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
}