using UnityEngine;

// Procedurally generated sprites for the maze UI – no art assets needed.
public static class MazeGraphics
{
    private static readonly Vector2 Center = new Vector2(32f, 32f);
    private static Sprite tile, block, bar, glow, arrow, ring, enterIcon;

    public static Sprite Tile      => Get(ref tile,      () => Make(64, TileShape));
    public static Sprite Block     => Get(ref block,     () => Make(128, BlockShape));
    public static Sprite Bar       => Get(ref bar,       () => Make(64, BarShape, new Vector4(24f, 24f, 24f, 24f)));
    public static Sprite Glow      => Get(ref glow,      () => Make(64, GlowShape));
    public static Sprite Arrow     => Get(ref arrow,     () => Make(64, ArrowShape));
    public static Sprite Ring      => Get(ref ring,      () => Make(64, RingShape));
    public static Sprite EnterIcon => Get(ref enterIcon, () => Make(64, EnterShape));

    public static Sprite Board(int size, Color board, Color line, bool gridLines)
    {
        Color fine = Color.Lerp(board, line, 0.08f);
        Color major = Color.Lerp(board, line, 0.16f);

        return Make(size, p =>
        {
            if (!gridLines) return board;
            int x = (int)p.x, y = (int)p.y;
            if (x % 64 == 0 || y % 64 == 0) return major;
            if (x % 16 == 0 || y % 16 == 0) return fine;
            return board;
        });
    }

    // ───────────────────────── Shapes ─────────────────────────

    private static Color TileShape(Vector2 p)
    {
        float d = RoundRect(p, 32f, 30f, 6f);
        if (d > 0.5f) return Color.clear;
        return White(d > -3f ? Mathf.Clamp01(0.5f - d) : 0.15f);
    }

    // Beveled block: dark outline, bright rim, body, inner panel with grooves
    private static Color BlockShape(Vector2 p)
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
    }

    // 9-sliced wall bar: dark outline, bright rim, body
    private static Color BarShape(Vector2 p)
    {
        float d = RoundRect(p, 32f, 31f, 12f);
        if (d > 0.5f) return Color.clear;
        float v = d > -3f ? 0.12f : d > -8f ? 1f : 0.7f;
        return new Color(v, v, v, Mathf.Clamp01(0.5f - d));
    }

    private static Color GlowShape(Vector2 p)
    {
        float a = Mathf.Clamp01(1f - Vector2.Distance(p, Center) / 32f);
        return White(a * a);
    }

    private static Color ArrowShape(Vector2 p)
    {
        Vector2 a = new Vector2(32f, 56f), b = new Vector2(10f, 12f), c = new Vector2(54f, 12f);
        float d = Mathf.Min(Edge(a, b, p), Mathf.Min(Edge(b, c, p), Edge(c, a, p)));
        return White(Mathf.Clamp01(d + 0.5f));
    }

    private static Color RingShape(Vector2 p)
    {
        Vector2 v = p - Center;
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        if (Mathf.Repeat(angle + 10f, 90f) < 20f) return Color.clear;
        return White(Mathf.Clamp01(0.5f - (Mathf.Abs(v.magnitude - 22f) - 4f)));
    }

    private static Color EnterShape(Vector2 p)
    {
        float r = Vector2.Distance(p, Center);
        return White(Mathf.Max(Mathf.Clamp01(12.5f - r), Mathf.Clamp01(0.5f - (Mathf.Abs(r - 22f) - 3f))));
    }

    // ───────────────────────── Helpers ─────────────────────────

    private static Sprite Get(ref Sprite cached, System.Func<Sprite> make)
    {
        if (cached == null) cached = make();
        return cached;
    }

    private static Sprite Make(int size, System.Func<Vector2, Color> shape, Vector4 border = default)
    {
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            px[y * size + x] = shape(new Vector2(x + 0.5f, y + 0.5f));

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        tex.SetPixels(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                             100f, 0, SpriteMeshType.FullRect, border);
    }

    private static Color White(float alpha) => new Color(1f, 1f, 1f, alpha);

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