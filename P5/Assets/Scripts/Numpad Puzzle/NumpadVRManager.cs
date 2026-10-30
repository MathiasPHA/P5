using System.Collections;
using UnityEngine;

public class NumpadVRManager : MonoBehaviour
{
    IEnumerator Start()
    {
        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);
    }

    void OnDestroy()
    {
        // Required: otherwise SharedState calls into destroyed objects after a scene switch
        if (SharedState.Instance != null)
            SharedState.Instance.Step.OnValueChanged -= OnStepChanged;
    }

    void OnStepChanged(int oldValue, int newValue)
    {
        // react here
    }

    void Solve() { 

        SharedState.Instance.SetNumpadNumbersRpc(1, 2, 3, 4);
    }
}