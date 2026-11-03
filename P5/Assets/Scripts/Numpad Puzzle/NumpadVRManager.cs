using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NumpadVRManager : MonoBehaviour
{
    [SerializeField] private Button[] numpadButtons = new Button[10];
    [SerializeField] private Button submitButton;
    [SerializeField] private Button backButton;

    [SerializeField] private TMP_Text[] inputedNumbers = new TMP_Text[4];
    [SerializeField] private int[] attemptedNumbers = new int[4];
    [SerializeField] private int[] correctNumbers = new int[4];
    [SerializeField] private int currentInputIndex = 0;
    IEnumerator Start()
    {
        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);
        SharedState.Instance.OnNumpadNumbersSet += OnNumpadNumbersSet;
    }

    void OnDestroy()
    {
        if (SharedState.Instance != null)
            // Required: otherwise SharedState calls into destroyed objects after a scene switch
            SharedState.Instance.OnNumpadNumbersSet -= OnNumpadNumbersSet;
    }

    public void OnNumpadButtonPressed(int number)
    {
        if (currentInputIndex < 4)
        {
            attemptedNumbers[currentInputIndex] = number;
            inputedNumbers[currentInputIndex].text = number.ToString();
            currentInputIndex++;
        }
        else
        {
            Debug.Log("All 4 numbers have been inputted. Please submit or clear.");
        }
    }

    public void OnSubmitButtonPressed()
    {
        if (currentInputIndex == 4)
        {
            SharedState.Instance.SetNumpadNumbersRpc(attemptedNumbers[0], attemptedNumbers[1], attemptedNumbers[2], attemptedNumbers[3]);
            currentInputIndex = 0;
            for (int i = 0; i < inputedNumbers.Length; i++)
            {
                inputedNumbers[i].text = "";
            }
        }
        else
        {
            Debug.Log("Please input all 4 numbers before submitting.");
        }
    }

    public void OnBackButtonPressed()
    {
        if (currentInputIndex > 0)
        {
            currentInputIndex--;
            attemptedNumbers[currentInputIndex] = 0;
            inputedNumbers[currentInputIndex].text = "";
        }
        else
        {
            Debug.Log("No numbers to remove.");
        }
    }

    void OnNumpadNumbersSet(int n1, int n2, int n3, int n4)
    {
        if (n1 == correctNumbers[0] && n2 == correctNumbers[1] && n3 == correctNumbers[2] && n4 == correctNumbers[3])
        {
            Debug.Log("Correct combination entered!");
            OpenDoor(); 
        }
        else
        {
            Debug.Log("Incorrect combination. Please try again.");
            
            // Clear the input after an incorrect attempt
            currentInputIndex = 0;
            for (int i = 0; i < inputedNumbers.Length; i++)
            {
                inputedNumbers[i].text = "";
            }
        }
    }

    void OpenDoor()
    {
        // Implement door opening logic here
        Debug.Log("Door is now open!");
    }
}