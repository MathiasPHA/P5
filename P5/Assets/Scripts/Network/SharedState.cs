using System;
using Unity.Netcode;
using UnityEngine;

// Holds every value both sides need to agree on.
// Spawned once by the host from Bootstrap. Lives in DontDestroyOnLoad,
// so it survives the PC scene switch. Must be a root GameObject.
//
// Between test sessions the host calls ResetForNewSession(): every value goes back
// to its default, so the next game starts clean without restarting the app.

public enum PuzzleId : byte { Captcha, Numpad, Maze, SafeLock }
public enum PuzzleEvent : byte { Start, Fail, Solved }

public class SharedState : NetworkBehaviour
{
    public static SharedState Instance { get; private set; }

    // Default permissions: everyone reads, only the server (host) writes.
    // Clients change values through the Rpc methods below.
    // Every NetworkVariable MUST be initialized here, or spawning fails.
    // If you add one, also reset it in ResetForNewSession().
    public NetworkVariable<int> Step = new(0);
    public NetworkVariable<int> CaptchaSet = new(0);
    public NetworkVariable<bool> CaptchaSolved = new(false);
    public NetworkVariable<bool> GameWon = new(false);
    public NetworkVariable<bool> GameStarted = new(false);


    // Fires on BOTH host and client every time a numpad guess is submitted
    public event Action<int, int, int, int> OnNumpadNumbersSet;

    // Fires once per session on BOTH host and client when the game is won (the milk bottle is grabbed)
    public event Action OnGameWon;

    // Fires on BOTH host and client every time MainMenu.StartGame is called
    public event Action OnGameStarted;


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

    // ---------- Game Started ----------

    // Called by MainMenu.StartGame (in the menus, and anywhere else MainMenu is used to move
    // both players on together). Every call with true fires OnGameStarted on BOTH sides
    // through an RPC. RPCs are always delivered, unlike a NetworkVariable that flips
    // true -> false within one network tick (the headset then only sees "false").
    // GameStarted itself is just a status flag now; nothing waits for it to change.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetGameStartedRpc(bool value)
    {
        GameStarted.Value = value;
        GameWon.Value = false;  // reset the win for the next game
        if (value) GameStartedEventRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void GameStartedEventRpc()
    {
        Debug.Log($"[SharedState] Game start received (server: {IsServer})");
        OnGameStarted?.Invoke();
    }

    // ---------- Win ----------

    // Called by MilkBottleWin on the VR side. Setting the value again later does nothing,
    // so the win can only happen once per session (ResetForNewSession clears it).
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void WinGameRpc()
    {
        GameWon.Value = true;
        GameStarted.Value = false;
    }

    void HandleGameWonChanged(bool previous, bool current)
    {
        if (current && !previous) OnGameWon?.Invoke();
    }

    // ---------- Test logging ----------

    public event Action<PuzzleId, PuzzleEvent> OnPuzzleEvent; // raised on the host only

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReportPuzzleEventRpc(PuzzleId puzzle, PuzzleEvent evt) => OnPuzzleEvent?.Invoke(puzzle, evt);

    // ---------- New session ----------

    // Server only. Called by SessionLogger's "New session" button in the PC main menu.
    // Both sides are already back in their main menus (the game loads them after the
    // end cutscene), so no scene is reloaded here: the values just go back to their
    // defaults before the next game starts.
    public void ResetForNewSession()
    {
        if (!IsServer) return;

        Step.Value = 0;
        CaptchaSet.Value = 0;
        CaptchaSolved.Value = false;
        GameWon.Value = false;      // the change handler only fires on false -> true,
        GameStarted.Value = false;  // so resetting doesn't trigger OnGameWon
    }
}