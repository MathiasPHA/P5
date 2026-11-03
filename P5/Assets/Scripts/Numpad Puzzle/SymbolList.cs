using UnityEngine;

public class SymbolList : MonoBehaviour
{
    [SerializeField] Transform content;      // Scroll View > Viewport > Content
    [SerializeField] SymbolRow rowPrefab;
    [SerializeField] UnityEngine.UI.ScrollRect scrollRect;

    public SymbolRow AddRow(Sprite a, Sprite b, Sprite c, Sprite d)
    {
        var row = Instantiate(rowPrefab, content);
        row.Set(new[] { a, b, c, d });

        // Optional: jump to the newest row at the bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        return row;
    }

    public void Clear()
    {
        foreach (Transform child in content) Destroy(child.gameObject);
    }
}