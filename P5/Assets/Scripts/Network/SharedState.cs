using System;
using Unity.Netcode;
using UnityEngine;

// Holds every value both sides need to agree on.
// Spawned once by the host from Bootstrap. Lives in DontDestroyOnLoad,
// so it survives the PC scene switch.
public class SharedState : NetworkBehaviour
{
    public static SharedState Instance { get; private set; }

    // Default permissions: everyone reads, only the server (host) writes.
    // Clients change values through the Rpc methods below.
    public NetworkVariable<int> Step = new(0);
    public NetworkVariable<int> Numpad1;
    public NetworkVariable<int> Numpad2;
    public NetworkVariable<int> Numpad3;
    public NetworkVariable<int> Numpad4;
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

    // Callable from host or client, runs on the host.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetStepRpc(int value) => Step.Value = value;

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetNumpadNumbersRpc(int num1, int num2, int num3, int num4)
    {
        if (IsServer)
        {
            Numpad1.Value = num1;
            Numpad2.Value = num2;
            Numpad3.Value = num3;
            Numpad4.Value = num4;
        }
        OnNumpadNumbersSet?.Invoke(num1, num2, num3, num4);
    }
}