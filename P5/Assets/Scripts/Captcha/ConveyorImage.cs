using UnityEngine;
using UnityEngine.UI;

// Bends a UI Image around a roller at the top and bottom edges of its rect,
// like a belt on a conveyor / scrolling billboard.
// Offset moves the picture along the belt (in UI units, + = up).
// Works with Image Type = Simple. Do NOT put this under a RectMask2D (it clips the rollers).
[RequireComponent(typeof(Graphic))]
public class ConveyorImage : BaseMeshEffect
{
    [Tooltip("Roller radius as a fraction of the picture height")]
    [SerializeField, Range(0.01f, 0.5f)] float rollerRadius = 0.08f;

    [Tooltip("How far around the roller the picture stays visible before it disappears behind it (degrees)")]
    [SerializeField, Range(0f, 180f)] float wrapAngle = 90f;

    [Tooltip("Mesh resolution. Raise it if the curve looks jagged")]
    [SerializeField, Range(4, 256)] int segments = 64;

    [Tooltip("Tick if the picture bulges toward the viewer instead of curving away")]
    [SerializeField] bool flipDepth;

    float offset;
    public float Offset
    {
        get => offset;
        set
        {
            if (Mathf.Approximately(offset, value)) return;
            offset = value;
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount < 4) return;

        // Read the original quad's bounds, UVs and colour
        UIVertex v = default;
        Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue), pMax = -pMin;
        Vector2 uvMin = pMin, uvMax = -pMin;
        for (int i = 0; i < 4; i++)
        {
            vh.PopulateUIVertex(ref v, i);
            pMin = Vector2.Min(pMin, v.position); pMax = Vector2.Max(pMax, v.position);
            uvMin = Vector2.Min(uvMin, v.uv0);    uvMax = Vector2.Max(uvMax, v.uv0);
        }
        Color32 color = v.color;

        float bottom = pMin.y, top = pMax.y, h = top - bottom;
        float r = rollerRadius * h;
        float maxArc = r * wrapAngle * Mathf.Deg2Rad;

        // Part of the belt that is visible: the flat face plus the visible part of each roller
        float lo = Mathf.Max(bottom + offset, bottom - maxArc);
        float hi = Mathf.Min(top + offset, top + maxArc);

        vh.Clear();
        if (hi <= lo) return;

        float step = h / segments;
        int rows = 0;
        AddRow(vh, lo, ref rows);
        for (int k = Mathf.FloorToInt((lo - bottom) / step) + 1; bottom + k * step < hi; k++)
            AddRow(vh, bottom + k * step, ref rows);
        AddRow(vh, hi, ref rows);

        void AddRow(VertexHelper helper, float beltY, ref int rowCount)
        {
            float t = (beltY - offset - bottom) / h;
            float uvY = Mathf.Lerp(uvMin.y, uvMax.y, t);

            float y, z;
            if (beltY > top)          { float a = (beltY - top) / r;    y = top + r * Mathf.Sin(a);    z = r * (1f - Mathf.Cos(a)); }
            else if (beltY < bottom)  { float a = (bottom - beltY) / r; y = bottom - r * Mathf.Sin(a); z = r * (1f - Mathf.Cos(a)); }
            else                      { y = beltY; z = 0f; }
            if (flipDepth) z = -z;

            helper.AddVert(new Vector3(pMin.x, y, z), color, new Vector4(uvMin.x, uvY));
            helper.AddVert(new Vector3(pMax.x, y, z), color, new Vector4(uvMax.x, uvY));

            if (rowCount > 0)
            {
                int b = (rowCount - 1) * 2;   // previous row: b = left, b+1 = right
                helper.AddTriangle(b, b + 2, b + 3);
                helper.AddTriangle(b + 3, b + 1, b);
            }
            rowCount++;
        }
    }
}
