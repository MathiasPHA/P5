using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopWindow : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] RectTransform window;
    Canvas canvas;

    void Awake() => canvas = GetComponentInParent<Canvas>();

    public void Open()
    {
        gameObject.SetActive(true);
        Focus();
    }

    public void Close() => gameObject.SetActive(false);

    public void Focus() => window.SetAsLastSibling(); // bring to front

    public void OnPointerDown(PointerEventData e) => Focus();

    // Called from TitleBarDrag
    public void Move(Vector2 screenDelta)
    {
        // divide by scaleFactor so movement matches the 1920x1080 reference space
        window.anchoredPosition += screenDelta / canvas.scaleFactor;
    }
}