using FishNet;
using UnityEngine;
using InflatableIdiots.Platform;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Spike harness: owns the Steam platform lifecycle and drives the host/join handshake, plus a tiny
    /// OnGUI control panel. Place on a scene GameObject alongside the NetworkManager + NetPlayerSpawner.
    ///
    /// Host flow:   Host -> CreateLobby -> (LobbyCreated) set HostSteamID + start server & self-client -> Invite.
    /// Joiner flow: accept overlay invite -> (JoinRequested -> JoinLobby) -> (LobbyEntered) read HostSteamID
    ///              -> SetClientAddress -> start client.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        IPlatform platform;
        FishySteamworks.FishySteamworks transport;

        ulong currentLobby;
        bool isHost;
        string status = "Initializing…";

        void Awake()
        {
            platform = new SteamPlatform();
            platform.OnLobbyCreated += HandleLobbyCreated;
            platform.OnInviteAcceptedJoinLobby += id => currentLobby = id;
            platform.OnLobbyEntered += HandleLobbyEntered;
        }

        void Start()
        {
            platform.Initialize();
            transport = InstanceFinder.NetworkManager.TransportManager.Transport as FishySteamworks.FishySteamworks;
            status = transport == null
                ? "ERROR: FishySteamworks is not the NetworkManager's transport."
                : (platform.IsReady ? $"Ready as {platform.LocalName}" : "ERROR: Steam not initialized (see Console).");
        }

        void Update() => platform.RunCallbacks();   // single pump point

        void OnDestroy()
        {
            if (platform != null) platform.Shutdown();
        }

        void HostClicked()
        {
            isHost = true;
            status = "Creating lobby…";
            platform.CreateLobby(2);
        }

        void HandleLobbyCreated(ulong lobbyId)
        {
            currentLobby = lobbyId;
            platform.SetLobbyData("HostSteamID", platform.LocalId.ToString());
            InstanceFinder.ServerManager.StartConnection();
            InstanceFinder.ClientManager.StartConnection();   // host self-connect -> ClientHostSocket
            status = $"Hosting (lobby {lobbyId}). Click Invite.";
        }

        void HandleLobbyEntered(ulong lobbyId)
        {
            currentLobby = lobbyId;
            if (isHost) return;   // host already started its server + client

            string hostIdStr = platform.GetLobbyData(lobbyId, "HostSteamID");
            if (string.IsNullOrEmpty(hostIdStr))
            {
                status = "Joined lobby but no HostSteamID yet…";
                return;
            }
            transport.SetClientAddress(hostIdStr);            // host SteamID as decimal string
            InstanceFinder.ClientManager.StartConnection();
            status = $"Connecting to host {hostIdStr}…";
        }

        GUIStyle buttonStyle;

        void OnGUI()
        {
            buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 24 };

            const float pad = 12f, labelW = 420f, labelH = 24f, btnW = 260f, btnH = 64f;
            float x = pad, y = pad;

            GUI.Label(new Rect(x, y, labelW, labelH), $"Steam: {status}");
            y += labelH + 6f;

            bool connected = InstanceFinder.IsServerStarted || InstanceFinder.IsClientStarted;
            if (!connected)
            {
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Host", buttonStyle)) HostClicked();
                y += btnH + 6f;
                GUI.Label(new Rect(x, y, labelW, labelH), "Joiner: accept the Steam overlay invite to connect.");
            }
            else if (isHost)
            {
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Invite Friend", buttonStyle)) platform.OpenInviteOverlay();
            }
        }
    }
}
