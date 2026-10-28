using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class StepCounter : MonoBehaviour
{
    [SerializeField] private int stepCount = 0;
    [SerializeField] private TMPro.TextMeshProUGUI stepText = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        // SharedState may not exist yet when the scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);
        SharedState.Instance.Step.OnValueChanged += OnStepChanged;
        Debug.Log($"[StepDebug] Ready, Step = {SharedState.Instance.Step.Value}");
    }

    void OnDestroy()
    {
        // Important for scenes that get replaced (PC_Phase1 -> PC_Phase2)
        if (SharedState.Instance != null)
            SharedState.Instance.Step.OnValueChanged -= OnStepChanged;
    }
    void OnStepChanged(int oldV, int newV) 
    { 
        stepCount = newV;
        stepText.text = stepCount.ToString();
    }

}
