using System;
using Unity.Netcode;
using UnityEngine;

// Holds every value both sides need to agree on.
// Spawned once by the host from Bootstrap. Lives in DontDestroyOnLoad,
// so it survives the PC scene switch. Must be a root GameObject.

public enum PuzzleId : byte { Captcha, Numpad, Maze, SafeLock }
public enum PuzzleEvent : byte { Start, Fail, Solved }

public class SharedState : NetworkBehaviour
{
    public static SharedState Instance { get; private set; }

    // Default permissions: everyone reads, only the server (host) writes.
    // Clients change values through the Rpc methods below.
    // Every NetworkVariable MUST be initialized here, or spawning fails.
    public NetworkVariable<int> Step = new(0);
    public NetworkVariable<int> CaptchaSet = new(0);
    public NetworkVariable<bool> CaptchaSolved = new(false);
    public NetworkVariable<bool> GameWon = new(false);


    // Fires on BOTH host and client every time a numpad guess is submitted
    public event Action<int, int, int, int> OnNumpadNumbersSet;

    // Fires once on BOTH host and client when the game is won (the milk bottle is grabbed)
    public event Action OnGameWon;



    public override void OnNetworkSpawn()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GameWon.OnValueChanged += HandleGameWonChanged;
    }

    public override void OnNetworkDespawn()
    {
        GameWon.OnValueChanged -= HandleGameWonChanged;
        if (Instance == this) Instance = null;
    }

    // ---------- Step ----------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetStepRpc(int value) => Step.Value = value;

    // ---------- Numpad ----------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetNumpadNumbersRpc(int num1, int num2, int num3, int num4)
    {
        NumpadNumbersSetRpc(num1, num2, num3, num4);
    }

    [Rpc(SendTo.ClientsAndHost)]
    void NumpadNumbersSetRpc(int num1, int num2, int num3, int num4)
    {
        OnNumpadNumbersSet?.Invoke(num1, num2, num3, num4);
    }

    // ---------- Captcha ----------

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetCaptchaRpc(int value) => CaptchaSet.Value = value;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetCaptchaSolvedRpc(bool value) => CaptchaSolved.Value = value;

    // ---------- Win ----------

    // Called by MilkBottleWin on the VR side. Setting the value again later does nothing,
    // so the win can only happen once.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void WinGameRpc() => GameWon.Value = true;

    void HandleGameWonChanged(bool previous, bool current)
    {
        if (current && !previous) OnGameWon?.Invoke();
    }

    // ---------- Test logging ----------
    public event Action<PuzzleId, PuzzleEvent> OnPuzzleEvent; // raised on the host only

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReportPuzzleEventRpc(PuzzleId puzzle, PuzzleEvent evt) => OnPuzzleEvent?.Invoke(puzzle, evt);
}