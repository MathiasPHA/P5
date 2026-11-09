using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Unity.VisualScripting;
using System;
public class SafeLock : MonoBehaviour
{
    [Header("Code")]
    [SerializeField] private int [] correctCode = new int[4] {1, 2, 3, 4}; 

    [Header("Displays")]
    [SerializeField] private TMP_Text[] digitDisplays = new TMP_Text[4];
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;

    [Header("Events")]
    public UnityEvent onUnlocked;
    public UnityEvent onWrongCode;

    private int[] currentCode = new int[4];
    private bool unlocked = false;

    void Start()
    {
        UpdateAllDisplays();
    }

    public void Increase(int index)
    {
        if (unlocked) return;
        if (currentCode[index] >= 9) return; //stops display at 9
        currentCode[index]++;
        UpdateDisplay(index);
        CheckCode();
    }

    public void Decrease(int index)
    {
        if (unlocked) return;
        if (currentCode[index] <= 0) return; // stops display at 0
        currentCode[index]--;
        UpdateDisplay(index);
        CheckCode();
    }

    private void CheckCode()
    {
        for (int i = 0; i < correctCode.Length; i++)
        {
            if (currentCode[i] != correctCode[i]) return;
        }

        unlocked = true;
        SetTextColor(correctColor);
        onUnlocked?.Invoke();
    }

    private void SetTextColor(Color color)
    {
        foreach (TMP_Text display in digitDisplays)
            display.color = color;
    }
    void UpdateDisplay(int index)
    {
        digitDisplays[index].text = currentCode[index].ToString();
    }

    private void UpdateAllDisplays()
    {
        for (int i = 0; i < digitDisplays.Length; i++)
            UpdateDisplay(i);
    }
}
