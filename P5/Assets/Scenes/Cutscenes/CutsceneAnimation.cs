using UnityEngine;

public class CutsceneAnimation : MonoBehaviour
{
    private Animator animator;
    void Start()
    {
        animator = GetComponent<Animator>();
        animator.Play("OutroCutscene");
    }

}
