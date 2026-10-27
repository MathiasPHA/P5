using TMPro;
using UnityEngine;

public class AddressBar : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    [SerializeField] string correctAddress = "192.168.0.1";
    [SerializeField] DesktopWindow captchaWindow;   // Window_Captcha
    [SerializeField] TMP_Text feedbackText;         // optional

    void OnEnable() => input.onSubmit.AddListener(Submit);
    void OnDisable() => input.onSubmit.RemoveListener(Submit);

    public void OnEnterButton() => Submit(input.text);

    void Submit(string text)
    {
        if (text.Trim() == correctAddress)
        {
            if (feedbackText) feedbackText.text = "";
            captchaWindow.Open();   // shows it and brings it to the front
        }
        else
        {
            if (feedbackText) feedbackText.text = "Connection failed";
            input.text = "";
            input.ActivateInputField();
        }
    }

    public void Retry(string message)
    {
        GetComponent<DesktopWindow>().Open();   // in case the window was closed
        if (feedbackText) feedbackText.text = message;
        input.text = "";
        input.ActivateInputField();             // cursor ready in the field
    }
}