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
    /// Overlay-free fallback (App ID 480 can't inject the overlay into a directly-launched build): the host
    /// shows its SteamID and the joiner pastes it into "Join by ID" -> SetClientAddress -> start client.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        IPlatform platform;
        FishySteamworks.FishySteamworks transport;

        ulong currentLobby;
        bool isHost;
        string status = "Initializing…";

        // On-screen log ring buffer so standalone builds show [Steam]/spawn messages without opening Player.log.
        readonly System.Collections.Generic.Queue<string> logLines = new System.Collections.Generic.Queue<string>();
        const int MaxLogLines = 12;

        string joinIdInput = "";   // host SteamID typed by the joiner (overlay-free join path)

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

        void OnEnable() => Application.logMessageReceived += AppendLog;
        void OnDisable() => Application.logMessageReceived -= AppendLog;

        void AppendLog(string message, string stackTrace, LogType type)
        {
            logLines.Enqueue(type == LogType.Log ? message : $"[{type}] {message}");
            while (logLines.Count > MaxLogLines) logLines.Dequeue();
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
            GUIUtility.systemCopyBuffer = platform.LocalId.ToString();   // auto-copy so host can paste it into Discord
            status = $"Hosting — SteamID {platform.LocalId} copied to clipboard. Paste it to your friend.";
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

        // Overlay-free join: connect straight to the host's SteamID (no lobby/overlay). For 480 testing.
        void JoinByIdClicked()
        {
            string id = joinIdInput.Trim();
            if (transport == null) { status = "Transport not ready."; return; }
            if (string.IsNullOrEmpty(id)) { status = "Paste the host's SteamID first."; return; }
            transport.SetClientAddress(id);
            InstanceFinder.ClientManager.StartConnection();
            status = $"Connecting to host {id}…";
        }

        GUIStyle buttonStyle;
        GUIStyle labelStyle;
        GUIStyle textFieldStyle;

        void OnGUI()
        {
            buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 24 };
            labelStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 20 };
            textFieldStyle ??= new GUIStyle(GUI.skin.textField) { fontSize = 20 };

            const float pad = 12f, labelW = 520f, labelH = 28f, btnW = 260f, btnH = 64f;
            float x = pad, y = pad;

            GUI.Label(new Rect(x, y, labelW, labelH), $"Steam: {status}", labelStyle);
            y += labelH + 6f;

            bool connected = InstanceFinder.IsServerStarted || InstanceFinder.IsClientStarted;
            if (!connected)
            {
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Host", buttonStyle)) HostClicked();
                y += btnH + 10f;

                // Overlay-free join path for testing on App ID 480 (the Steam overlay won't inject into a
                // directly-launched build). Joiner pastes the host's SteamID — shown on the host's screen.
                GUI.Label(new Rect(x, y, labelW, labelH), "…or join by host SteamID:", labelStyle);
                y += labelH + 2f;
                joinIdInput = GUI.TextField(new Rect(x, y, btnW, 44f), joinIdInput, textFieldStyle);
                if (GUI.Button(new Rect(x + btnW + 8f, y, btnW, 44f), "Join by ID", buttonStyle))
                    JoinByIdClicked();
            }
            else if (isHost)
            {
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Invite Friend", buttonStyle)) platform.OpenInviteOverlay();
                y += btnH + 6f;
                if (GUI.Button(new Rect(x, y, btnW, btnH), "Copy SteamID", buttonStyle))
                {
                    GUIUtility.systemCopyBuffer = platform.LocalId.ToString();
                    status = $"SteamID {platform.LocalId} copied — paste it to your friend.";
                }
                y += btnH + 6f;
                GUI.Label(new Rect(x, y, labelW, labelH),
                    $"Overlay-free: friend uses 'Join by ID' with your SteamID  {platform.LocalId}", labelStyle);
            }

            DrawOnScreenLog();
        }

        // Renders recent Debug logs at the bottom of the screen. Standalone builds have no Console, so this
        // is how you read [Steam]/spawn messages while play-testing a build (live, on both PCs).
        void DrawOnScreenLog()
        {
            if (logLines.Count == 0) return;

            const float lineH = 24f;
            float boxH = logLines.Count * lineH + 8f;
            GUI.Box(new Rect(8f, Screen.height - boxH - 8f, Screen.width - 16f, boxH), GUIContent.none);

            float ly = Screen.height - boxH - 4f;
            foreach (string line in logLines)
            {
                GUI.Label(new Rect(14f, ly, Screen.width - 28f, lineH), line, labelStyle);
                ly += lineH;
            }
        }
    }
}
