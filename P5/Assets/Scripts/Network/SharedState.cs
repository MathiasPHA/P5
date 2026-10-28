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
}