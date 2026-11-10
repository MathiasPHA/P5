using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// Finds the PC host on the local network, so the Quest never needs a typed IP.
// The Quest broadcasts "is there a host?", and the PC replies directly to it.
// Added at runtime to the NetworkManager GameObject by Bootstrap,
// so it survives scene loads (the PC keeps answering after loading its scene).
public class LanDiscovery : MonoBehaviour
{
    public const int DiscoveryPort = 47777; // must differ from the game port (7777)
    const string Request = "P5_DISCOVER";
    const string ReplyPrefix = "P5_HOST:";

    // ip, game port
    public event Action<string, ushort> OnHostFound;

    UdpClient udp;
    bool isServer;
    ushort gamePort;
    float nextBroadcast;

    // PC: listen for requests and answer with our game port
    public void StartServer(ushort port)
    {
        Stop();
        isServer = true;
        gamePort = port;
        try
        {
            udp = new UdpClient(DiscoveryPort);
            Debug.Log($"[LanDiscovery] Answering discovery on UDP {DiscoveryPort}");
        }
        catch (SocketException e)
        {
            // e.g. another host already running on this machine
            Debug.LogWarning($"[LanDiscovery] Could not open discovery port: {e.Message}");
            udp = null;
        }
    }

    // Quest: broadcast a request every second until a host answers
    public void StartSearching()
    {
        Stop();
        isServer = false;
        nextBroadcast = 0f;
        try
        {
            udp = new UdpClient(0) { EnableBroadcast = true }; // any free local port
        }
        catch (SocketException e)
        {
            // No network: skip discovery, manual entry still works once Wi-Fi is back
            Debug.LogWarning($"[LanDiscovery] Could not start searching: {e.Message}");
            udp = null;
        }
    }

    public void Stop()
    {
        udp?.Close();
        udp = null;
    }

    void Update()
    {
        if (udp == null) return;

        try
        {
            if (!isServer && Time.unscaledTime >= nextBroadcast)
            {
                // Set BEFORE sending: if Send throws (e.g. no Wi-Fi), we still wait
                // a second instead of retrying and logging every frame
                nextBroadcast = Time.unscaledTime + 1f;
                byte[] msg = Encoding.ASCII.GetBytes(Request);
                udp.Send(msg, msg.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
                LastError = null; // sending works again
            }

            while (udp != null && udp.Available > 0)
            {
                var from = new IPEndPoint(IPAddress.Any, 0);
                string text = Encoding.ASCII.GetString(udp.Receive(ref from));

                if (isServer && text == Request)
                {
                    byte[] reply = Encoding.ASCII.GetBytes(ReplyPrefix + gamePort);
                    udp.Send(reply, reply.Length, from);
                }
                else if (!isServer && text.StartsWith(ReplyPrefix) &&
                         ushort.TryParse(text.Substring(ReplyPrefix.Length), out ushort port))
                {
                    Stop();
                    OnHostFound?.Invoke(from.Address.ToString(), port);
                }
            }
        }
        catch (SocketException e)
        {
            // Log each distinct error once, not every attempt (logging is slow on Quest)
            if (e.Message != LastError)
            {
                LastError = e.Message;
                Debug.LogWarning($"[LanDiscovery] {e.Message} (is the headset on the same Wi-Fi as the PC?)");
            }
        }
    }

    // Last error logged, so repeats are skipped; null while things work
    public string LastError { get; private set; }

    void OnDestroy() => Stop();

    // All IPv4 addresses of this machine, e.g. "192.168.43.12"
    public static string LocalIPv4s()
    {
        var ips = new List<string>();
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up ||
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

            foreach (var a in ni.GetIPProperties().UnicastAddresses)
                if (a.Address.AddressFamily == AddressFamily.InterNetwork)
                    ips.Add(a.Address.ToString());
        }
        return string.Join(", ", ips);
    }
}