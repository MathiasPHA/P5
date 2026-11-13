using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// PC (host) only: records the test data for one play session.
// Added at runtime to the NetworkManager GameObject by Bootstrap, so it survives scene loads.
//
// Writes two CSV files to Documents/EscapeRoomLogs:
//   events_<session>.csv  one line per event, written immediately (survives a crash)
//   sessions.csv          one row per session, appended when the session ends
//
// The facilitator enters pair ID + condition and presses "Ready".
// The clock starts when the VR player connects (at once, if already connected).
// The session ends when the final puzzle (the maze) is solved, on F10, or when the game quits.
public class SessionLogger : MonoBehaviour
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture; // "12.5", never the Danish "12,5"
    static readonly PuzzleId[] Puzzles = (PuzzleId[])Enum.GetValues(typeof(PuzzleId));
    static readonly string[] Conditions = { "colocated", "remote" };
    const PuzzleId FinalPuzzle = PuzzleId.Maze; // solving this one stops the clock

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

    string sessionId, folder, eventsPath, doneMessage;
    double t0, lastSolveAt;
    bool subscribed;
    readonly Dictionary<PuzzleId, PuzzleStats> stats = new Dictionary<PuzzleId, PuzzleStats>();

    double Now => Time.realtimeSinceStartupAsDouble - t0;
    string Condition => Conditions[conditionIndex];

    // ConnectedClientsIds includes the host itself
    static bool VrConnected
    {
        get
        {
            var nm = NetworkManager.Singleton;
            return nm != null && nm.IsServer && nm.ConnectedClientsIds.Count > 1;
        }
    }

    void Update()
    {
        // Ready was pressed: start the clock the moment the VR player is connected
        if (phase == Phase.Armed && subscribed && VrConnected) Begin();

        // SharedState may not exist yet on the first frames
        if (!subscribed && SharedState.Instance != null)
        {
            SharedState.Instance.OnPuzzleEvent += OnPuzzleEvent;
            subscribed = true;
        }

        // Facilitator: end the session early (logged as not completed)
        if (phase == Phase.Running && Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            Finish(false);
    }

    void OnDestroy()
    {
        if (subscribed && SharedState.Instance != null)
            SharedState.Instance.OnPuzzleEvent -= OnPuzzleEvent;
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

        if (evt != PuzzleEvent.Solved) return;
        lastSolveAt = t;

        if (puzzle == FinalPuzzle) Finish(true);
    }

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

        doneMessage = $"Session {(completed ? "complete" : "ended early")}: {Sec(total)} s\nSaved to {path}";
        Debug.Log($"[SessionLogger] {doneMessage.Replace('\n', ' ')}");
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

            GUILayout.Label(VrConnected
                ? "VR player already connected: the clock starts when you press Ready"
                : "The clock starts when the VR player connects");

            GUI.enabled = pairId.Trim().Length > 0;
            if (GUILayout.Button("Ready", GUILayout.Height(40))) phase = Phase.Armed;
            GUI.enabled = true;
            GUILayout.EndArea();
        }
        else if (phase == Phase.Armed)
        {
            GUI.Box(new Rect(16, h - 56, 420, 40), "Ready. Clock starts when the VR player connects.");
        }
        else if (phase == Phase.Done)
        {
            GUI.Box(new Rect(16, h - 76, 620, 60), doneMessage);
        }
    }
}
