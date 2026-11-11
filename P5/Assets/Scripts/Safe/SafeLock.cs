using UnityEngine;
using UnityEngine.Events;
using TMPro;
public class SafeLock : MonoBehaviour
{
    [Header("Code")]
    [SerializeField] private int [] correctCode = new int[4] {1, 2, 3, 4}; 

    [Header("Displays")]
    [SerializeField] private TMP_Text[] digitDisplays = new TMP_Text[4];
    
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color correctColor = Color.green;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip correctSafeCodeSound;
    [SerializeField] private float minimumPitchShift = 0.95f;
    [SerializeField] private float maximumPitchShift = 1.05f;

    [Header("Events")]
    public UnityEvent onUnlocked;
    // public UnityEvent onWrongCode; //Bruges aldrig

    private int[] currentCode = new int[4];
    private bool unlocked = false;

    void Start()
    {
        UpdateAllDisplays();
    }

    void PlayAudioCue()
        {
            if (audioSource == null || clickSound == null) return;
            audioSource.PlayOneShot(clickSound);
        }
    void CorrectCodeAudio()
        {
            if (audioSource == null || clickSound == null) return;
            audioSource.PlayOneShot(correctSafeCodeSound);
        }
    public void Increase(int index)
    {
        if (unlocked) return;
        
        if (currentCode[index] >= 9)
        {
            currentCode[index] = 0; //Wraps from 9 to 0
        }
        else
        {
            currentCode[index]++;
        }      
        
        audioSource.pitch = Random.Range(minimumPitchShift, maximumPitchShift);
        PlayAudioCue();
        UpdateDisplay(index);
        CheckCode();
    }

    public void Decrease(int index)
    {
        if (unlocked) return;

        if (currentCode[index] <= 0)
        {
            currentCode[index] = 9; //wraps from 0 to 9
        }
        else
        {
            currentCode[index]--;
        }

        audioSource.pitch = Random.Range(minimumPitchShift, maximumPitchShift);
        PlayAudioCue();
        UpdateDisplay(index);
        CheckCode();
    }

    private void CheckCode()
    {
        for (int i = 0; i < correctCode.Length; i++)
        {
            if (currentCode[i] != correctCode[i]) return;
        }

        unlocked = true;
        CorrectCodeAudio();
        SetTextColor(correctColor);
        onUnlocked?.Invoke();
    }

    private void SetTextColor(Color color)
    {
        foreach (TMP_Text display in digitDisplays)
            display.color = color;
    }
    void UpdateDisplay(int index)
    {
        digitDisplays[index].text = currentCode[index].ToString();
    }

    private void UpdateAllDisplays()
    {
        for (int i = 0; i < digitDisplays.Length; i++)
            UpdateDisplay(i);
    }
}
