using UnityEngine;
using UnityEngine.InputSystem;

public class VRHandsController : MonoBehaviour
{
    public InputActionProperty grabAction;
    public InputActionProperty triggerAction;

    public Animator animator;

    [SerializeField] private GameObject pointInteractor;

    void Update()
    {
        float grabValue = grabAction.action.ReadValue<float>();
        animator.SetFloat("grab", grabValue);

        float triggerValue = triggerAction.action.ReadValue<float>();
        animator.SetFloat("trigger", triggerValue);
    }
}
