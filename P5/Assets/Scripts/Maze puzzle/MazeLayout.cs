using UnityEngine;

[CreateAssetMenu(menuName = "Puzzle/Maze Layout", fileName = "MazeLayout")]
public class MazeLayout : ScriptableObject
{
    public int width = 6;
    public int height = 6;
    public Vector2Int start = new Vector2Int(2, 1);
    public Vector2Int exit = new Vector2Int(1, 4);

    [Tooltip("Symbols shown above this maze (the code that identifies it)")]
    public Sprite[] symbols = new Sprite[4];

    // Index = y * width + x   (x = column from left, y = row from top, both from 0)
    public bool[] wallRight;
    public bool[] wallBottom;

    void Reset() => LoadWhiteboardMaze();
    void OnValidate() => EnsureSize();

    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

    public bool IsBlocked(Vector2Int from, Vector2Int dir)
    {
        Vector2Int to = from + dir;
        if (!InBounds(from) || !InBounds(to)) return true;
        if (dir.x == 1)  return wallRight[from.y * width + from.x];
        if (dir.x == -1) return wallRight[to.y * width + to.x];
        if (dir.y == 1)  return wallBottom[from.y * width + from.x];
        if (dir.y == -1) return wallBottom[to.y * width + to.x];
        return true;
    }

    public void EnsureSize()
    {
        int n = width * height;
        if (wallRight == null || wallRight.Length != n || wallBottom == null || wallBottom.Length != n)
            Resize(width, height);
    }

    public void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Max(2, newWidth);
        newHeight = Mathf.Max(2, newHeight);

        var newRight = new bool[newWidth * newHeight];
        var newBottom = new bool[newWidth * newHeight];

        bool canCopy = wallRight != null && wallBottom != null
                       && wallRight.Length == width * height && wallBottom.Length == width * height;

        if (canCopy)
            for (int y = 0; y < Mathf.Min(height, newHeight); y++)
            for (int x = 0; x < Mathf.Min(width, newWidth); x++)
            {
                newRight[y * newWidth + x] = wallRight[y * width + x];
                newBottom[y * newWidth + x] = wallBottom[y * width + x];
            }

        width = newWidth;
        height = newHeight;
        wallRight = newRight;
        wallBottom = newBottom;
        start = Clamp(start);
        exit = Clamp(exit);
    }

    public void ClearWalls()
    {
        wallRight = new bool[width * height];
        wallBottom = new bool[width * height];
    }

    public void LoadWhiteboardMaze()
    {
        width = 6;
        height = 6;
        ClearWalls();
        start = new Vector2Int(2, 1);
        exit = new Vector2Int(1, 4);

        int[,] right =
        {
            {3,0},
            {2,1}, {4,1},
            {1,2}, {4,2},
            {0,3}, {2,3}, {3,3},
            {1,4},
            {2,5}, {3,5}
        };

        int[,] bottom =
        {
            {0,0}, {2,0},
            {1,1}, {2,1}, {3,1}, {4,1},
            {0,2}, {3,2}, {5,2},
            {0,3}, {1,3}, {2,3}, {4,3},
            {1,4}, {4,4}
        };

        for (int i = 0; i < right.GetLength(0); i++) wallRight[right[i, 1] * width + right[i, 0]] = true;
        for (int i = 0; i < bottom.GetLength(0); i++) wallBottom[bottom[i, 1] * width + bottom[i, 0]] = true;
    }

    private Vector2Int Clamp(Vector2Int c) =>
        new Vector2Int(Mathf.Clamp(c.x, 0, width - 1), Mathf.Clamp(c.y, 0, height - 1));
}