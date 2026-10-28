using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeLayout))]
public class MazeLayoutEditor : Editor
{
    private enum Tool { Walls, Start, Exit }
    private Tool tool;

    private const float Cell = 36f;
    private const float Gap = 10f;

    private static readonly Color BorderColor = new Color(0.45f, 0.65f, 1f);
    private static readonly Color BgColor = new Color(0.12f, 0.12f, 0.12f);
    private static readonly Color CellColor = new Color(0.18f, 0.32f, 0.2f);
    private static readonly Color OpenColor = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color WallColor = new Color(0.45f, 0.65f, 1f);
    private static readonly Color StartColor = new Color(0.3f, 1f, 1f);
    private static readonly Color ExitColor = new Color(1f, 0.6f, 0.15f);

    private GUIStyle labelStyle;

    public override void OnInspectorGUI()
    {
        var maze = (MazeLayout)target;
        maze.EnsureSize();

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
            labelStyle.normal.textColor = Color.black;
        }

        EditorGUI.BeginChangeCheck();
        int w = EditorGUILayout.IntSlider("Width", maze.width, 2, 12);
        int h = EditorGUILayout.IntSlider("Height", maze.height, 2, 12);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(maze, "Resize Maze");
            maze.Resize(w, h);
            EditorUtility.SetDirty(maze);
        }

        EditorGUILayout.Space();
        tool = (Tool)GUILayout.Toolbar((int)tool, new[] { "Toggle Walls", "Set Start", "Set Exit" });
        EditorGUILayout.HelpBox(
            tool == Tool.Walls ? "Click the gaps between cells to add or remove walls." : "Click a cell.",
            MessageType.None);

        EditorGUILayout.Space();
        DrawGrid(maze);
        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Walls"))
        {
            Undo.RecordObject(maze, "Clear Walls");
            maze.ClearWalls();
            EditorUtility.SetDirty(maze);
        }
        if (GUILayout.Button("Load Whiteboard Maze"))
        {
            Undo.RecordObject(maze, "Load Whiteboard Maze");
            maze.LoadWhiteboardMaze();
            EditorUtility.SetDirty(maze);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawGrid(MazeLayout maze)
    {
        float totalW = maze.width * Cell + (maze.width + 1) * Gap;
        float totalH = maze.height * Cell + (maze.height + 1) * Gap;

        Rect area = GUILayoutUtility.GetRect(totalW, totalH);
        area.x += (area.width - totalW) / 2f;
        area.width = totalW;
        area.height = totalH;

        EditorGUI.DrawRect(area, BorderColor);
        EditorGUI.DrawRect(new Rect(area.x + Gap, area.y + Gap, totalW - Gap * 2f, totalH - Gap * 2f), BgColor);

        Event e = Event.current;
        bool click = e.type == EventType.MouseDown && e.button == 0;

        for (int y = 0; y < maze.height; y++)
        for (int x = 0; x < maze.width; x++)
        {
            var pos = new Vector2Int(x, y);
            Rect cell = new Rect(area.x + Gap + x * (Cell + Gap), area.y + Gap + y * (Cell + Gap), Cell, Cell);

            bool isStart = maze.start == pos;
            bool isExit = maze.exit == pos;
            EditorGUI.DrawRect(cell, isStart ? StartColor : isExit ? ExitColor : CellColor);
            if (isStart) GUI.Label(cell, "S", labelStyle);
            if (isExit) GUI.Label(cell, "E", labelStyle);

            if (tool != Tool.Walls && click && cell.Contains(e.mousePosition))
            {
                Undo.RecordObject(maze, tool == Tool.Start ? "Set Start" : "Set Exit");
                if (tool == Tool.Start) maze.start = pos;
                else maze.exit = pos;
                EditorUtility.SetDirty(maze);
                e.Use();
            }

            if (x < maze.width - 1)
                DrawWall(maze, new Rect(cell.xMax, cell.y, Gap, Cell), ref maze.wallRight[y * maze.width + x], click, e);

            if (y < maze.height - 1)
                DrawWall(maze, new Rect(cell.x, cell.yMax, Cell, Gap), ref maze.wallBottom[y * maze.width + x], click, e);
        }
    }

    private void DrawWall(MazeLayout maze, Rect r, ref bool wall, bool click, Event e)
    {
        EditorGUI.DrawRect(r, wall ? WallColor : OpenColor);
        if (tool == Tool.Walls) EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);

        if (tool == Tool.Walls && click && r.Contains(e.mousePosition))
        {
            Undo.RecordObject(maze, "Toggle Wall");
            wall = !wall;
            EditorUtility.SetDirty(maze);
            e.Use();
        }
    }
}