using UnityEngine;

public class LockIndicator : MonoBehaviour
{
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;
    [SerializeField] private bool startLocked = true;

    void Awake()
    {
        SetLocked(startLocked);
    }

    public void Unlock() => SetLocked(false);
    public void Lock() => SetLocked(true);

    private void SetLocked(bool locked)
    {
        if (lockedVisual != null) lockedVisual.SetActive(locked);
        if (unlockedVisual != null) unlockedVisual.SetActive(!locked);
    }
}