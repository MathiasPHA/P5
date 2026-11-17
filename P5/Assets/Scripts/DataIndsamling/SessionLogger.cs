using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// PC (host) only: records the test data for one play session at a time.
// Added at runtime to the NetworkManager GameObject by Bootstrap, so it survives scene loads.
//
// Writes two CSV files to Documents/EscapeRoomLogs:
//   events_<session>.csv  one line per event, written immediately (survives a crash)
//   sessions.csv          one row per session, appended when the session ends
//
// Flow:
//   1. Setup panel: the facilitator enters pair ID + condition and presses "Ready".
//   2. The clock starts when the main menu's Start button is pressed (see SessionStartButton),
//      which is only possible once the VR player is connected.
//   3. The clock stops and the data is saved the moment the milk bottle is grabbed
//      (see MilkBottleWin / SharedState.OnGameWon). F10 ends a session early.
//   4. The end cutscene plays and the game returns to the main menu by itself.
//      There the summary is shown, with a "New session" button that resets the
//      shared state and goes back to step 1. No app restart needed.
public class SessionLogger : MonoBehaviour
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture; // "12.5", never the Danish "12,5"
    static readonly PuzzleId[] Puzzles = (PuzzleId[])Enum.GetValues(typeof(PuzzleId));
    static readonly string[] Conditions = { "colocated", "remote" };

    class PuzzleStats
    {
        public double startedAt = -1, solvedAt = -1;
        public int fails;
        public bool Solved => solvedAt >= 0;
    }

    enum Phase { Setup, Armed, Running, Done }

    Phase phase = Phase.Setup;
    string pairId = "";
    int conditionIndex;

    string sessionId, folder, eventsPath, summary;
    double t0, lastSolveAt;
    bool subscribed;
    readonly Dictionary<PuzzleId, PuzzleStats> stats = new Dictionary<PuzzleId, PuzzleStats>();

    double Now => Time.realtimeSinceStartupAsDouble - t0;
    string Condition => Conditions[conditionIndex];

    public static SessionLogger Instance { get; private set; }

    void Awake() => Instance = this;

    // Called by SessionStartButton when the main menu's Start button is clicked
    public void StartSession()
    {
        if (phase != Phase.Armed || !subscribed || !VrConnected)
        {
            Debug.LogWarning($"[SessionLogger] StartSession ignored (phase {phase}, VR connected: {VrConnected})");
            return;
        }
        Begin();
    }

    // ConnectedClientsIds includes the host itself
    public static bool VrConnected
    {
        get
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsServer && nm.ConnectedClientsIds.Count > 1;
        }
    }

    // The PC is in its main menu (the scene Boot loaded)
    static bool InMainMenu =>
        !string.IsNullOrEmpty(Bootstrap.MenuScene) && SceneManager.GetActiveScene().name == Bootstrap.MenuScene;

    // Setup panel, or the summary panel once back in the main menu
    bool PanelOpen => phase == Phase.Setup || (phase == Phase.Done && InMainMenu);

    // EventSystems switched off while a panel is showing
    readonly List<EventSystem> blockedEventSystems = new List<EventSystem>();

    // The panels are drawn with OnGUI, which does not stop clicks from reaching
    // the game's own UI underneath. So while one is open, the scene's EventSystem is
    // switched off: no button, slider or keyboard navigation in the game UI responds.
    // (This also stops the main menu's Start button from starting an unlogged game.)
    // Called every frame while a panel is open, so a newly loaded scene's EventSystem is caught too.
    void BlockSceneUI()
    {
        var es = EventSystem.current; // null once it has been switched off
        if (es == null) return;
        es.enabled = false;
        blockedEventSystems.Add(es);
    }

    void UnblockSceneUI()
    {
        foreach (var es in blockedEventSystems)
            if (es != null) es.enabled = true; // null = its scene was unloaded meanwhile
        blockedEventSystems.Clear();
    }

    void Update()
    {
        if (PanelOpen) BlockSceneUI();

        // SharedState may not exist yet on the first frames
        if (!subscribed && SharedState.Instance != null)
        {
            SharedState.Instance.OnPuzzleEvent += OnPuzzleEvent;
            SharedState.Instance.OnGameWon += OnGameWon;
            subscribed = true;
        }

        // Facilitator: end the session early (logged as not completed)
        if (phase == Phase.Running && Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            Finish(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnblockSceneUI();
        if (subscribed && SharedState.Instance != null)
        {
            SharedState.Instance.OnPuzzleEvent -= OnPuzzleEvent;
            SharedState.Instance.OnGameWon -= OnGameWon;
        }
    }

    // Keep the data from a session that was closed before it was finished
    void OnApplicationQuit()
    {
        if (phase == Phase.Running) Finish(false);
    }

    // ---------- Session ----------

    void Begin()
    {
        pairId = Sanitize(pairId);
        sessionId = $"{pairId}_{Condition}_{DateTime.Now.ToString("yyyyMMdd_HHmmss", Inv)}";

        folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EscapeRoomLogs");
        Directory.CreateDirectory(folder);
        eventsPath = Path.Combine(folder, $"events_{sessionId}.csv");

        stats.Clear();
        foreach (var p in Puzzles) stats[p] = new PuzzleStats();

        t0 = Time.realtimeSinceStartupAsDouble;
        lastSolveAt = 0;
        phase = Phase.Running;

        File.WriteAllText(eventsPath, "session_id,pair_id,condition,t_s,puzzle,event,fails_so_far\n");
        WriteEvent(0, "", "SessionStart", 0);
        Debug.Log($"[SessionLogger] Session {sessionId} started. Logging to {folder}");
    }

    // Runs on the host for every event reported through SharedState.ReportPuzzleEventRpc
    void OnPuzzleEvent(PuzzleId puzzle, PuzzleEvent evt)
    {
        if (phase != Phase.Running) return;
        if (!stats.TryGetValue(puzzle, out var s) || s.Solved) return; // ignore anything after the solve

        double t = Now;

        if (evt == PuzzleEvent.Start)
        {
            if (s.startedAt >= 0) return; // only the first start counts
            s.startedAt = t;
        }
        else
        {
            // No explicit start reported: the puzzle began when the previous one was solved
            if (s.startedAt < 0) s.startedAt = lastSolveAt;

            if (evt == PuzzleEvent.Fail) s.fails++;
            else s.solvedAt = t;
        }

        WriteEvent(t, puzzle.ToString(), evt.ToString(), s.fails);

        if (evt == PuzzleEvent.Solved) lastSolveAt = t;
    }

    // The milk bottle was grabbed: this stops the clock, at the moment of the grab
    // (not after the cutscene). The summary waits until the game is back in the main menu.
    void OnGameWon() => Finish(true);

    void Finish(bool completed)
    {
        if (phase != Phase.Running) return;
        phase = Phase.Done;

        double total = Now;
        WriteEvent(total, "", completed ? "SessionEnd" : "SessionAborted", 0);

        // One row per session.
        // fails    = failed tries (wrong codes, wrong picks, wall hits)
        // attempts = fails plus the successful try (codes submitted, images picked)
        var header = new StringBuilder("session_id,pair_id,condition,date,completed,total_s");
        var row = new StringBuilder();
        row.Append(sessionId).Append(',').Append(pairId).Append(',').Append(Condition).Append(',')
           .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm", Inv)).Append(',')
           .Append(completed ? 1 : 0).Append(',').Append(Sec(total));

        // Human-readable version for the summary panel
        var text = new StringBuilder();
        text.Append($"Pair {pairId}, {Condition}: {(completed ? "completed" : "ended early")}\n");
        text.Append($"Total time: {Sec(total)} s\n\n");

        foreach (var p in Puzzles)
        {
            var s = stats[p];
            string n = p.ToString().ToLowerInvariant();
            header.Append($",{n}_solved,{n}_time_s,{n}_fails,{n}_attempts,{n}_solved_at_s");
            row.Append(',').Append(s.Solved ? 1 : 0)
               .Append(',').Append(s.Solved ? Sec(s.solvedAt - s.startedAt) : "")
               .Append(',').Append(s.fails)
               .Append(',').Append(s.fails + (s.Solved ? 1 : 0))
               .Append(',').Append(s.Solved ? Sec(s.solvedAt) : "");

            text.Append($"{p}: {(s.Solved ? Sec(s.solvedAt - s.startedAt) + " s" : "not solved")}, " +
                        $"{s.fails} {(s.fails == 1 ? "fail" : "fails")}\n");
        }

        string path = Path.Combine(folder, "sessions.csv");
        try
        {
            if (!File.Exists(path)) File.WriteAllText(path, header + "\n");
            File.AppendAllText(path, row + "\n");
        }
        catch (IOException)
        {
            // sessions.csv is probably open in Excel: save this session on its own instead
            path = Path.Combine(folder, $"summary_{sessionId}.csv");
            File.WriteAllText(path, header + "\n" + row + "\n");
        }

        text.Append($"\nSaved to {path}");
        summary = text.ToString();
        Debug.Log($"[SessionLogger] Session {sessionId} {(completed ? "complete" : "ended early")}: {Sec(total)} s, saved to {path}");
    }

    // Back to the setup panel without restarting the app.
    // Both sides are already in their main menus, so only the shared values are reset.
    void NewSession()
    {
        if (phase != Phase.Done) return;

        if (SharedState.Instance != null) SharedState.Instance.ResetForNewSession();
        else Debug.LogWarning("[SessionLogger] SharedState missing: shared values were not reset");

        phase = Phase.Setup; // the menu UI stays blocked until Ready is pressed
        pairId = "";
        summary = null;
        Debug.Log("[SessionLogger] New session: waiting for setup");
    }

    void WriteEvent(double t, string puzzle, string evt, int fails)
    {
        try
        {
            File.AppendAllText(eventsPath,
                $"{sessionId},{pairId},{Condition},{Sec(t)},{puzzle},{evt},{fails}\n");
        }
        catch (IOException e)
        {
            Debug.LogWarning($"[SessionLogger] Could not write event: {e.Message}");
        }
    }

    static string Sec(double seconds) => seconds.ToString("F2", Inv);

    // Safe for file names and CSV cells
    static string Sanitize(string s)
    {
        var sb = new StringBuilder();
        foreach (char c in s.Trim())
            sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
        return sb.ToString();
    }

    // ---------- Facilitator UI ----------

    void OnGUI()
    {
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale, h = Screen.height / scale;

        if (phase == Phase.Setup)
        {
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none); // dim the game until the session starts

            GUILayout.BeginArea(new Rect(w / 2f - 200f, h / 2f - 130f, 400f, 260f), GUI.skin.box);
            GUILayout.Label("Test session setup");
            GUILayout.Space(8);
            GUILayout.Label("Pair ID");
            pairId = GUILayout.TextField(pairId, 20);
            GUILayout.Label("Condition");
            conditionIndex = GUILayout.SelectionGrid(conditionIndex, Conditions, Conditions.Length);
            GUILayout.Space(8);

            GUILayout.Label("The clock starts when Start is pressed in the main menu");

            GUI.enabled = pairId.Trim().Length > 0;
            if (GUILayout.Button("Ready", GUILayout.Height(40)))
            {
                phase = Phase.Armed;
                UnblockSceneUI(); // the game's UI works again from here
            }
            GUI.enabled = true;
            GUILayout.EndArea();
        }
        else if (phase == Phase.Armed)
        {
            GUI.Box(new Rect(16, h - 56, 460, 40), VrConnected
                ? "Ready. Clock starts when Start is pressed."
                : "Ready. Waiting for VR player before Start can be pressed.");
        }
        else if (phase == Phase.Done && InMainMenu)
        {
            // Nothing is drawn during the cutscene; the summary appears once back in the menu
            GUI.Box(new Rect(0, 0, w, h), GUIContent.none);

            GUILayout.BeginArea(new Rect(w / 2f - 260f, h / 2f - 180f, 520f, 360f), GUI.skin.box);
            GUILayout.Label("Last session");
            GUILayout.Space(8);
            GUILayout.Label(summary);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("New session", GUILayout.Height(40)))
                NewSession();
            GUILayout.EndArea();
        }
    }
}