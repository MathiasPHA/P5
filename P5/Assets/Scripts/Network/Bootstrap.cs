using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using System.Linq;
using Unity.Multiplayer.PlayMode;
#endif

// Lives in the Boot scene (reached from the start screens: Launcher → MainMenu_PC/VR → Boot).
// PC build hosts, answers LAN discovery and loads the PC scene.
// Quest build shows a connect screen: finds the host automatically,
// falls back to manual IP entry, then loads the VR scene once connected.
// In the editor, a Multiplayer Play Mode player tagged "VR" acts as the Quest.
public class Bootstrap : MonoBehaviour
{
    [SerializeField] GameObject sharedStatePrefab;
    [SerializeField] ushort port = 7777;
    [SerializeField] string pcScene = "PC_Phase1";
    [SerializeField] string vrScene = "PcScreen";

    [Header("VR connect screen")]
    [SerializeField] GameObject vrConnectRoot; // XR rig + connect UI; disabled on PC
    [SerializeField] TMP_Text statusText;
    [SerializeField] IpEntryKeypad ipKeypad;
    [SerializeField] float searchTimeout = 5f; // seconds before the keypad appears

    // PC: the address(es) the host is reachable on, for showing on screen if needed
    public static string HostInfo { get; private set; } = "";

    // The scene Boot loads on this side (the main menu). SessionLogger shows the
    // summary and the New session button when the PC is back in this scene.
    public static string MenuScene { get; private set; } = "";

    const string LastIpKey = "LastHostIp";

    NetworkManager nm;
    UnityTransport utp;
    LanDiscovery discovery;

    bool searching;
    bool connecting;
    float searchStarted;
    float connectStarted;
    string lastTriedIp;

    // Matches the UTP settings below, plus a margin. If Netcode never reports
    // the failure, Update() gives up after this many seconds anyway.
    const float ConnectWatchdog = 7f;

    void Start()
    {
        nm = NetworkManager.Singleton;
        utp = nm.GetComponent<UnityTransport>();

        // Lives on the NetworkManager (DontDestroyOnLoad), so it survives scene loads
        discovery = nm.gameObject.AddComponent<LanDiscovery>();

        // Fail a bad connection after ~5 s instead of UTP's default ~60 s
        utp.ConnectTimeoutMS = 1000;
        utp.MaxConnectAttempts = 5;

        bool isVR = Application.platform == RuntimePlatform.Android;
        bool mppmVR = false;

#if UNITY_EDITOR
        // MPPM virtual player tagged "VR" = fake Quest on the same machine
        if (CurrentPlayer.Tags.Contains("VR"))
        {
            isVR = true;
            mppmVR = true;
        }
#endif

        // Captures only nm, so it keeps logging after Boot is unloaded
        nm.OnClientDisconnectCallback += id =>
            Debug.LogWarning($"[Bootstrap] Disconnected (client {id}). Reason: {nm.DisconnectReason}");

        if (!isVR)
        {
            if (vrConnectRoot != null) vrConnectRoot.SetActive(false);
            StartAsHost();
            return;
        }

        // ---- VR ----
        if (vrConnectRoot != null) vrConnectRoot.SetActive(true);

        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnConnectFailed;
        nm.OnTransportFailure += OnTransportFailure;
        discovery.OnHostFound += Connect;
        ipKeypad.OnConnectPressed += ConnectManual;

        ipKeypad.SetText(PlayerPrefs.GetString(LastIpKey, ""));
        ipKeypad.gameObject.SetActive(false);

        if (mppmVR) Connect("127.0.0.1", port);
        else StartSearching();
    }

    void OnDestroy()
    {
        // Boot is unloaded after connecting; don't leave callbacks pointing at this object
        if (nm != null)
        {
            nm.OnClientConnectedCallback -= OnClientConnected;
            nm.OnClientDisconnectCallback -= OnConnectFailed;
            nm.OnTransportFailure -= OnTransportFailure;
        }
        if (discovery != null) discovery.OnHostFound -= Connect;
        if (ipKeypad != null) ipKeypad.OnConnectPressed -= ConnectManual;
    }

    void Update()
    {
        // Show the keypad if discovery hasn't found anything in time (it keeps searching)
        if (searching && !ipKeypad.gameObject.activeSelf &&
            Time.unscaledTime - searchStarted > searchTimeout)
        {
            ipKeypad.gameObject.SetActive(true);
            SetStatus("No host found yet. Still searching...\nOr type the IP shown on the PC.");
        }

        // Safety net: if no connected/disconnected callback arrives, stop waiting
        if (connecting && Time.unscaledTime - connectStarted > ConnectWatchdog)
        {
            Debug.LogWarning("[Bootstrap] Connect watchdog fired: no response from Netcode.");
            FailConnect("Timed out.");
        }
    }

    // ---------- PC ----------

    void StartAsHost()
    {
        // Cap the PC build's frame rate. Without this, a simple scene renders at
        // hundreds or thousands of FPS and runs the GPU at 100 %.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        HostInfo = $"{LanDiscovery.LocalIPv4s()} (port {port})";
        nm.gameObject.AddComponent<SessionLogger>();
        Debug.Log($"[Bootstrap] PC: hosting on {HostInfo}");

        utp.SetConnectionData("0.0.0.0", port);
        nm.StartHost();
        Instantiate(sharedStatePrefab).GetComponent<NetworkObject>().Spawn();
        discovery.StartServer(port);
        nm.gameObject.AddComponent<HostInfoOverlay>(); // IP on screen until VR connects

        if (!Application.CanStreamedLevelBeLoaded(pcScene))
            Debug.LogError($"[Bootstrap] Scene '{pcScene}' can't be loaded: not in the build or misspelled.");
        else
        {
            MenuScene = pcScene;
            SceneManager.LoadScene(pcScene);
        }
    }

    // ---------- VR ----------

    void StartSearching()
    {
        searching = true;
        connecting = false;
        searchStarted = Time.unscaledTime;
        SetStatus("Searching for host...");
        discovery.StartSearching();
    }

    void ConnectManual(string ip) => Connect(ip, port);

    void Connect(string ip, ushort hostPort)
    {
        if (connecting) return;
        searching = false;
        connecting = true;
        connectStarted = Time.unscaledTime;
        lastTriedIp = ip;

        discovery.Stop();
        ipKeypad.gameObject.SetActive(false);
        SetStatus($"Connecting to {ip}...");
        Debug.Log($"[Bootstrap] VR: joining {ip}:{hostPort}");

        utp.SetConnectionData(ip, hostPort);
        if (!nm.StartClient()) FailConnect("Could not start the client.");
    }

    // Only load the VR scene once actually connected
    void OnClientConnected(ulong id)
    {
        if (id != nm.LocalClientId) return;
        connecting = false;

        PlayerPrefs.SetString(LastIpKey, lastTriedIp); // prefill the keypad next time
        PlayerPrefs.Save();

        // Catch a wrong name or a scene missing from the build list, instead of silently staying in Boot
        if (!Application.CanStreamedLevelBeLoaded(vrScene))
        {
            SetStatus($"Connected, but scene '{vrScene}' is not in the build.\nCheck the name and the Build Profile scene list.");
            Debug.LogError($"[Bootstrap] Scene '{vrScene}' can't be loaded: not in the build or misspelled.");
            return;
        }

        SetStatus($"Connected! Loading {vrScene}...");
        Debug.Log($"[Bootstrap] Loading VR scene '{vrScene}'");
        MenuScene = vrScene;
        SceneManager.LoadScene(vrScene);
    }

    void OnConnectFailed(ulong id) =>
        FailConnect(string.IsNullOrEmpty(nm.DisconnectReason) ? "No response." : nm.DisconnectReason);

    void OnTransportFailure() => FailConnect("Network error.");

    // Every failure path ends here, so the keypad always comes back
    void FailConnect(string reason)
    {
        if (!connecting) return; // only handle failures while trying to connect
        connecting = false;

        SetStatus($"Couldn't connect to {lastTriedIp}.\n{reason}");
        StartCoroutine(BackToSearch());
    }

    IEnumerator BackToSearch()
    {
        if (nm.IsListening || nm.IsClient) nm.Shutdown();

        // Don't wait forever if shutdown hangs
        float t = Time.unscaledTime;
        yield return new WaitUntil(() => !nm.ShutdownInProgress || Time.unscaledTime - t > 3f);
        yield return new WaitForSeconds(2f); // time to read the error

        // Keypad first, so nothing below can stop it from showing
        ipKeypad.gameObject.SetActive(true);
        StartSearching();
        SetStatus("Searching again...\nOr type the IP shown on the PC.");
    }

    void SetStatus(string text)
    {
        if (statusText != null) statusText.text = text;
        Debug.Log($"[Bootstrap] {text.Replace('\n', ' ')}");
    }
}