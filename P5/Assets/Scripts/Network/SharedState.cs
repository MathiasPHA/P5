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


    // Fires on BOTH host and client every time a numpad guess is submitted
    public event Action<int, int, int, int> OnNumpadNumbersSet;



    public override void OnNetworkSpawn()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkDespawn()
    {
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

    // ---------- Test logging ----------
    public event Action<PuzzleId, PuzzleEvent> OnPuzzleEvent; // raised on the host only

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReportPuzzleEventRpc(PuzzleId puzzle, PuzzleEvent evt) => OnPuzzleEvent?.Invoke(puzzle, evt);
}