using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// TEMPORARY test script. Put one in the PC scene and one in the VR scene.
// Space bumps Step by 1; both sides should log the change.
public class StepDebug : MonoBehaviour
{
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

    void OnStepChanged(int oldV, int newV) => Debug.Log($"[StepDebug] Step {oldV} -> {newV}");

    void Update()
    {
        if (SharedState.Instance == null) return;
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            SharedState.Instance.SetStepRpc(SharedState.Instance.Step.Value + 1);
    }
}