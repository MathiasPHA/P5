using System.Collections;
using UnityEngine;

public class NumpadPCManager : MonoBehaviour
{
    [SerializeField] private int correctNum1;
    [SerializeField] private int correctNum2;
    [SerializeField] private int correctNum3;
    [SerializeField] private int correctNum4;

    // 0 = correct, 1 = right number wrong place, 2 = not in code
    [SerializeField] private Sprite[] symbolSprites = new Sprite[3];
    [SerializeField] private SymbolList symbolList;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => SharedState.Instance != null);
        SharedState.Instance.OnNumpadNumbersSet += OnNumpadNumbersSet;
    }

    void OnDestroy()
    {
        if (SharedState.Instance != null)
            SharedState.Instance.OnNumpadNumbersSet -= OnNumpadNumbersSet;
    }

    void OnNumpadNumbersSet(int n1, int n2, int n3, int n4)
    {
        symbolList.AddRow(
            GetSymbol(n1, correctNum1),
            GetSymbol(n2, correctNum2),
            GetSymbol(n3, correctNum3),
            GetSymbol(n4, correctNum4));
    }

    Sprite GetSymbol(int guess, int correctHere)
    {
        if (guess == correctHere) return symbolSprites[0];
        if (guess == correctNum1 || guess == correctNum2 ||
            guess == correctNum3 || guess == correctNum4) return symbolSprites[1];
        return symbolSprites[2];
    }
}