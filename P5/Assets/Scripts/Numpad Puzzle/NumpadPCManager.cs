using System.Collections;
using Microsoft.Unity.VisualStudio.Editor;
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

    [SerializeField] private Image symbol1;
    
    [SerializeField] private Image symbol2;
    
    [SerializeField] private Image symbol3;
    
    [SerializeField] private Image symbol4;
    IEnumerator Start()
    {
        //SharedState.Instance.Numpad1.OnValueChanged =+ OnNumpad1Changed;
        //SharedState.Instance.Numpad2.OnValueChanged =+ OnNumpad2Changed;
       //SharedState.Instance.Numpad3.OnValueChanged =+ OnNumpad3Changed;
      //  SharedState.Instance.Numpad4.OnValueChanged =+ OnNumpad4Changed;
        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);
    }
    

    void OnDestroy()
    {
        // Required: otherwise SharedState calls into destroyed objects after a scene switch
       // if (SharedState.Instance != null);
    }

    

    void OnNumpad1Changed()
    {
        attemptedNum1 = SharedState.Instance.Numpad1.Value;

        if (attemptedNum1 == correctNum1)
        {
            
        } 
    }
}