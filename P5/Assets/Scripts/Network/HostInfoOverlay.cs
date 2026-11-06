using Unity.Netcode;
using UnityEngine;

// PC only: shows the host IP in the corner of the screen while no VR player is connected.
// Added at runtime to the NetworkManager GameObject by Bootstrap, so it survives
// scene loads and works on top of any PC scene without extra setup.
// Reappears if the VR player disconnects.
public class HostInfoOverlay : MonoBehaviour
{
    GUIStyle style;

    void OnGUI()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

        // ConnectedClientsIds includes the host itself
        if (nm.ConnectedClientsIds.Count > 1) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(16, 16, 10, 10),
                wordWrap = false
            };
        }
        style.fontSize = Mathf.Max(14, Screen.height / 40); // scales with resolution

        string text = $"Waiting for VR player...\nIP: {Bootstrap.HostInfo}";
        Vector2 size = style.CalcSize(new GUIContent(text));
        GUI.Box(new Rect(16, 16, size.x, size.y), text, style);
    }
}
