using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRI 3.x
// using UnityEngine.XR.Interaction.Toolkit;            // use this instead on XRI 2.x

public class NumpadVRManager : MonoBehaviour
{
    // Order matters: numpadKeys[0] = the 0 key, [1] = the 1 key, ...
    [SerializeField] private XRSimpleInteractable[] numpadKeys = new XRSimpleInteractable[10];
    [SerializeField] private XRSimpleInteractable submitKey;
    [SerializeField] private XRSimpleInteractable backKey;

    [SerializeField] private TMP_Text inputDisplay;
    [SerializeField] private int[] attemptedNumbers = new int[4];
    [SerializeField] private int[] correctNumbers = new int[4]; // must match NumpadPCManager
    [SerializeField] private int currentInputIndex = 0;
    [SerializeField] private XRSimpleInteractable doorHandle; // Disables the interactable/locks the door
    IEnumerator Start()
    {
        // Hook up the keys first, so they work even while waiting for SharedState
        for (int i = 0; i < numpadKeys.Length; i++)
        {
            int n = i; // copy, otherwise every key would send 10
            numpadKeys[i].selectEntered.AddListener(_ => OnNumpadButtonPressed(n));
        }
        submitKey.selectEntered.AddListener(_ => OnSubmitButtonPressed());
        backKey.selectEntered.AddListener(_ => OnBackButtonPressed());

        UpdateDisplay();

        // SharedState is null for a moment after a scene loads
        yield return new WaitUntil(() => SharedState.Instance != null);
        SharedState.Instance.OnNumpadNumbersSet += OnNumpadNumbersSet;
    }

    void OnDestroy()
    {
        // Required: otherwise SharedState calls into destroyed objects after a scene switch
        if (SharedState.Instance != null)
            SharedState.Instance.OnNumpadNumbersSet -= OnNumpadNumbersSet;
    }

    public void OnNumpadButtonPressed(int number)
    {
        if (currentInputIndex < 4)
        {
            attemptedNumbers[currentInputIndex] = number;
            currentInputIndex++;
            UpdateDisplay();
        }
        else
        {
            Debug.Log("All 4 numbers have been inputted. Please submit or clear.");
        }
    }

    public void OnSubmitButtonPressed()
    {
        if (SharedState.Instance == null) return; // network not ready yet

        if (currentInputIndex == 4)
        {
            SharedState.Instance.SetNumpadNumbersRpc(
                attemptedNumbers[0], attemptedNumbers[1], attemptedNumbers[2], attemptedNumbers[3]);
            currentInputIndex = 0;
            UpdateDisplay();
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
            UpdateDisplay();
        }
        else
        {
            Debug.Log("No numbers to remove.");
        }
    }

    // Runs on both PC and VR after every submitted guess
    void OnNumpadNumbersSet(int n1, int n2, int n3, int n4)
    {
        if (n1 == correctNumbers[0] && n2 == correctNumbers[1] &&
            n3 == correctNumbers[2] && n4 == correctNumbers[3])
        {
            Debug.Log("Correct combination entered!");
            OpenDoor();
        }
        else
        {
            // Input was already cleared on submit, so don't clear again here,
            // or a guess the player has started typing gets wiped
            Debug.Log("Incorrect combination. Please try again.");
        }
    }

    // Builds the text from the typed digits, e.g. "4 7 _ _"
    void UpdateDisplay()
    {
        string text = "";
        for (int i = 0; i < 4; i++)
        {
            text += i < currentInputIndex ? attemptedNumbers[i].ToString() : "_";
            if (i < 3) text += " ";
        }
        inputDisplay.text = text;
    }

    void OpenDoor()
{
    if (doorHandle != null)
        doorHandle.enabled = true;   // the handle can now be grabbed

    Debug.Log("Door is now open!");
}
}