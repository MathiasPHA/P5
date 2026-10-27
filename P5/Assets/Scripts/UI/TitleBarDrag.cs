using UnityEngine;
using UnityEngine.EventSystems;

public class TitleBarDrag : MonoBehaviour, IDragHandler
{
    [SerializeField] DesktopWindow owner;
    public void OnDrag(PointerEventData e) => owner.Move(e.delta);
}