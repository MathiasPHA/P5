using UnityEngine;
using UnityEngine.EventSystems;

public class DesktopIcon : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] DesktopWindow target;

    public void OnPointerClick(PointerEventData e)
    {
        if (e.clickCount == 2) target.Open();
    }
}