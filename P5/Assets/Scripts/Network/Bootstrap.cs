using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Multiplayer.PlayMode;

#if UNITY_EDITOR

#endif

// Lives in the Boot scene (build index 0).
// PC build hosts and loads the PC scene; Quest build joins and loads the VR scene.
// In the editor, a Multiplayer Play Mode player tagged "VR" acts as the Quest.
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameObject sharedStatePrefab;
    [SerializeField] string hostIp = "192.168.1.100"; // PC's IP on the hotspot (real Quest only)
    [SerializeField] ushort port = 7777;
    [SerializeField] string pcScene = "PC_Phase1";
    [SerializeField] string vrScene = "PcScreen";

    void Start()
    {
        var nm = NetworkManager.Singleton;
        var utp = nm.GetComponent<UnityTransport>();

        bool isVR = Application.platform == RuntimePlatform.Android;
        string ip = hostIp;

#if UNITY_EDITOR
        // MPPM virtual player tagged "VR" = fake Quest on the same machine
        if (CurrentPlayer.ReadOnlyTags().Contains("VR"))
        {
            isVR = true;
            ip = "127.0.0.1";
        }
#endif

        nm.OnClientDisconnectCallback += id =>
            Debug.LogWarning($"[Bootstrap] Disconnected (client {id}). Reason: {nm.DisconnectReason}");

        if (isVR)
        {
            Debug.Log($"[Bootstrap] VR: joining {ip}:{port}");
            utp.SetConnectionData(ip, port);
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.StartClient();
        }
        else
        {
            Debug.Log($"[Bootstrap] PC: hosting on port {port}");
            utp.SetConnectionData("0.0.0.0", port);
            nm.StartHost();
            Instantiate(sharedStatePrefab).GetComponent<NetworkObject>().Spawn();
            SceneManager.LoadScene(pcScene);
        }
    }

    // Only load the VR scene once actually connected
    void OnClientConnected(ulong id)
    {
        if (id != NetworkManager.Singleton.LocalClientId) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        SceneManager.LoadScene(vrScene);
    }
}