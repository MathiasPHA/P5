using UnityEngine;
using UnityEngine.Events;
public class VRButtonV2Controller : MonoBehaviour
{
    public GameObject button;
    public UnityEvent onButtonPressed;
    public UnityEvent onButtonReleased;
    public Vector3 pressedPosition;
    public Vector3 neutralPosition;

    private GameObject presser;
    private bool isPressed;
    void Start()
    {
        isPressed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isPressed) 
        {
            button.transform.localPosition = pressedPosition;
            presser = other.gameObject;
            onButtonPressed.Invoke();
            isPressed = true;
        }
        
    }
    private void OnTriggerExit(Collider other)
    {
        if (isPressed && other.gameObject == presser) 
        {
            button.transform.localPosition = neutralPosition;
            onButtonReleased.Invoke();
            isPressed = false;
        }
    }

    public void ButtonPressed() 
    {
        Debug.Log("Button Pressed");
    }
}
