using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class NumpadVRManager : MonoBehaviour
{
    // Order matters: numpadKeys[0] = the 0 key, [1] = the 1 key, ...
    [SerializeField] private VRButtonV2Controller[] numpadKeys = new VRButtonV2Controller[10];
    [SerializeField] private VRButtonV2Controller submitKey;
    [SerializeField] private VRButtonV2Controller backKey;

    [SerializeField] private TMP_Text inputDisplay;
    [SerializeField] private int[] attemptedNumbers = new int[4];
    [SerializeField] private int[] correctNumbers = new int[4]; // must match NumpadPCManager
    [SerializeField] private int currentInputIndex = 0;

    [Header("Feedback")]
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private int failFlashes = 3;
    [SerializeField] private float flashInterval = 0.15f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip keyClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip successClip;
    [Tooltip("Pitch of the 0 key; keys 1-9 step evenly up to Max Key Pitch")]
    [SerializeField] private float minKeyPitch = 0.9f;
    [SerializeField] private float maxKeyPitch = 1.2f;

    [Header("On completion")]
    [SerializeField] private XRSimpleInteractable doorHandle; // the handle can only be grabbed after the puzzle is solved
    [SerializeField] private LockIndicator lockIndicator;
    private Color normalColor;
    private bool inputLocked;
    private bool solved;
    private Coroutine feedbackRoutine;

    IEnumerator Start()
    {
        // Hook up the keys first, so they work even while waiting for SharedState
        for (int i = 0; i < numpadKeys.Length; i++)
        {
            int n = i; // copy, otherwise every key would send 10
            numpadKeys[i].onButtonPressed.AddListener(() => OnNumpadButtonPressed(n));
        }
        submitKey.onButtonPressed.AddListener(OnSubmitButtonPressed);
        backKey.onButtonPressed.AddListener(OnBackButtonPressed);

        normalColor = inputDisplay.color;
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
        if (inputLocked || solved) return;

        if (currentInputIndex < 4)
        {
            attemptedNumbers[currentInputIndex] = number;
            currentInputIndex++;
            PlayKey(Mathf.Lerp(minKeyPitch, maxKeyPitch, number / 9f));
            UpdateDisplay();
        }
        else
        {
            Debug.Log("All 4 numbers have been inputted. Please submit or clear.");
        }
    }

    public void OnSubmitButtonPressed()
    {
        if (inputLocked || solved) return;
        if (SharedState.Instance == null) return; // network not ready yet

        if (currentInputIndex == 4)
        {
            PlayKey(1f);
            SharedState.Instance.SetNumpadNumbersRpc(
                attemptedNumbers[0], attemptedNumbers[1], attemptedNumbers[2], attemptedNumbers[3]);
            inputLocked = true; // wait for the network result before accepting input
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
        if (inputLocked || solved) return;

        if (currentInputIndex > 0)
        {
            currentInputIndex--;
            attemptedNumbers[currentInputIndex] = 0;
            PlayKey(1f);
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
        bool correct = n1 == correctNumbers[0] && n2 == correctNumbers[1] &&
                       n3 == correctNumbers[2] && n4 == correctNumbers[3];

        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(correct ? ShowSuccess() : ShowFail());
    }

    IEnumerator ShowFail()
    {
        inputLocked = true;
        Play(failClip);
        inputDisplay.color = failColor;

        for (int i = 0; i < failFlashes; i++)
        {
            inputDisplay.text = "X X X X";
            yield return new WaitForSeconds(flashInterval);
            inputDisplay.text = "";
            yield return new WaitForSeconds(flashInterval);
        }

        inputDisplay.color = normalColor;
        UpdateDisplay(); // back to "_ _ _ _"
        inputLocked = false;
        feedbackRoutine = null;
    }

    IEnumerator ShowSuccess()
    {
        solved = true; // keypad stays locked after it's solved
        Play(successClip);
        inputDisplay.color = successColor;
        inputDisplay.text = "Correct";
        OpenDoor();
        feedbackRoutine = null;
        yield break;
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

    // Each key has its own fixed pitch, so the same key always sounds the same
    void PlayKey(float pitch)
    {
        if (audioSource == null || keyClip == null) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(keyClip);
    }

    void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = 1f; // fail/success always sound the same
        audioSource.PlayOneShot(clip);
    }

    void OpenDoor()
    {
        if (doorHandle != null)
        doorHandle.enabled = true;   // the handle can now be grabbed

        if (lockIndicator !=null)
            lockIndicator.Unlock(); //swap the lock symbol on completion
        Debug.Log("Door is now open!");
    }
}