using System.Collections;
using UnityEngine;

public class NumpadPCManager : MonoBehaviour
{
     [SerializeField] private int correctNum1;
     [SerializeField] private int correctNum2;
     [SerializeField] private int correctNum3;
     [SerializeField] private int correctNum4;

    [SerializeField] private int attemptedNum1;
    [SerializeField] private int attemptedNum2;
    [SerializeField] private int attemptedNum3;
    [SerializeField] private int attemptedNum4;
    IEnumerator Start()
    {
        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);
    }

    void OnDestroy()
    {
        // Required: otherwise SharedState calls into destroyed objects after a scene switch
        if (SharedState.Instance != null);
    }

    

    void Solve()
    {
        SharedState.Instance.SetStepRpc(SharedState.Instance.Step.Value + 1);
    }
}