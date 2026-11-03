using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Mono.Cecil.Cil;
using Unity.VisualScripting;
using System;
public class SafeLock : MonoBehaviour
{
    [Header("Code")]
    [SerializeField] private int [] correctCode = new int[4] {1, 2, 3, 4}; 

    [Header("Displays")]
    [SerializeField] private TMP_Text[] digitDisplays = new TMP_Text[4];

    [Header("Events")]
    public UnityEvent onUnlocked;
    public UnityEvent onWrongCode;

    private int[] currentCode = new int[4];

    void Start()
    {
        UpdateAllDisplays();
    }

    public void Increase(int index)
    {
        if (currentCode[index] >= 9) return; //stops display at 9
        currentCode[index]++;
        UpdateDisplay(index);
    }

    public void Decrease(int index)
    {
        if (currentCode[index] <= 0) return; // stops display at 0
        currentCode[index]--;
        UpdateDisplay(index);
    }
    // Update is called once per frame
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
