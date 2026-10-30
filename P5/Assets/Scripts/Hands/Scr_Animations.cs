using UnityEngine;
using UnityEngine.InputSystem;

public class Scr_Animations : MonoBehaviour
{

    public InputActionProperty grabAction;
    public InputActionProperty triggerAction;

    public Animator animator;

    void Update()
    {
        float grabValue = grabAction.action.ReadValue<float>();
        animator.SetFloat("grab", grabValue);

        float triggerValue = triggerAction.action.ReadValue<float>();
        animator.SetFloat("trigger", triggerValue);
    }
}
