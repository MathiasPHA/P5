using UnityEngine;
using UnityEngine.InputSystem;

public class VRHandsController : MonoBehaviour
{
    public InputActionProperty gribAction;
    public InputActionProperty triggerAction;

    public Animator animator;

    void Update()
    {
        float gribValue = gribAction.action.ReadValue<float>();
        animator.SetFloat("grib", gribValue);

        float triggerValue = triggerAction.action.ReadValue<float>();
        animator.SetFloat("trigger", triggerValue);
    }
}
