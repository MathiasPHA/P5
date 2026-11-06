using System;
using TMPro;
using UnityEngine;

// Manual IP entry in VR, used when auto-discovery finds nothing.
// Same VRButtonV2Controller buttons as the numpad, plus a dot key.
public class IpEntryKeypad : MonoBehaviour
{
    // Order matters: digitKeys[0] = the 0 key, [1] = the 1 key, ...
    [SerializeField] private VRButtonV2Controller[] digitKeys = new VRButtonV2Controller[10];
    [SerializeField] private VRButtonV2Controller dotKey;
    [SerializeField] private VRButtonV2Controller backKey;
    [SerializeField] private VRButtonV2Controller connectKey;
    [SerializeField] private TMP_Text display;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keyClip;

    public event Action<string> OnConnectPressed;

    private string ip = "";

    void Start()
    {
        for (int i = 0; i < digitKeys.Length; i++)
        {
            char c = (char)('0' + i); // copy, same reason as in the numpad
            digitKeys[i].onButtonPressed.AddListener(() => Add(c));
        }
        dotKey.onButtonPressed.AddListener(() => Add('.'));
        backKey.onButtonPressed.AddListener(Back);
        connectKey.onButtonPressed.AddListener(Connect);
        Refresh();
    }

    // Prefill, e.g. with the last IP that worked
    public void SetText(string value)
    {
        ip = value ?? "";
        Refresh();
    }

    void Add(char c)
    {
        if (ip.Length >= 15) return; // "255.255.255.255" is 15 chars
        ip += c;
        Click();
        Refresh();
    }

    void Back()
    {
        if (ip.Length == 0) return;
        ip = ip.Substring(0, ip.Length - 1);
        Click();
        Refresh();
    }

    void Connect()
    {
        Click();
        if (IsValidIPv4(ip)) OnConnectPressed?.Invoke(ip);
        else display.text = "Invalid IP";
    }

    void Refresh() => display.text = ip.Length > 0 ? ip : "_";

    void Click()
    {
        if (audioSource != null && keyClip != null) audioSource.PlayOneShot(keyClip);
    }

    // Exactly four numbers 0-255 separated by dots
    static bool IsValidIPv4(string s)
    {
        string[] parts = s.Split('.');
        if (parts.Length != 4) return false;
        foreach (string p in parts)
            if (p.Length == 0 || !byte.TryParse(p, out _)) return false;
        return true;
    }
}
