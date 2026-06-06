using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using InflatableIdiots.Hazards;
using InflatableIdiots.Player;

namespace InflatableIdiots.Level
{
    public enum RunState { Playing, Won, Lost }

    /// <summary>
    /// Owns the win/lose state for a single run. GoalZone reports a win; RisingHazard reports a loss.
    ///
    /// The run doesn't begin until <see cref="RequiredPlayers"/> players are present (<see cref="RunStarted"/>).
    /// Locally both players exist at scene load, so it starts on the first frame — unchanged. In the networked
    /// scene players spawn over time, so this holds the rising hazard until BOTH have joined (otherwise the
    /// lone host would be caught before its partner connects). Each machine gates independently off the player
    /// count it can see, so no replication is needed for the start gate.
    ///
    /// LOCAL play: on win/lose it freezes the scene (Time.timeScale = 0, stopping the hazard AND controls) and
    /// R reloads the scene. NETWORKED play: an <see cref="IRunNetwork"/> attaches when the link spawns — run
    /// state then comes from the host (mirrored, so banners agree) and R resets the run IN PLACE through the
    /// network instead of reloading (a reload would drop the Steam/FishNet session + the host's join GUI). No
    /// timeScale freeze when networked (it would stall FishNet's tick), so two things stand in for it: the
    /// hazard mirrors the host's replicated height (TryGetNetworkHazardHeight) rather than self-rising, and
    /// player input is suppressed whenever <see cref="ControlsLocked"/> — before the run starts and after it
    /// ends — read by the networked controller.
    ///
    /// Also tracks altitude: the peak height any player reaches above the start, shown live during play
    /// and frozen as "Altitude reached: X m" on the end screen. Placeholder OnGUI UI for now.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        public static RunController Instance { get; private set; }

        // Authoritative run state comes from the network when attached (so every peer agrees), else local.
        RunState localState = RunState.Playing;
        IRunNetwork net;
        public RunState State => net != null ? net.State : localState;
        public bool IsOver => State != RunState.Playing;
        public float Altitude => maxAltitude;

        // True once the run has begun (enough players present). The rising hazard only climbs while this is set.
        public bool RunStarted { get; private set; }

        /// <summary>
        /// True when player input should be suppressed: before the run starts (waiting for everyone to join)
        /// or after it ends (caught/solved). Local play freezes the whole scene with timeScale instead, but
        /// networked play can't (it would stall FishNet's tick), so the networked controller reads this to
        /// lock walk/jump/air-transfer until the run is live.
        /// </summary>
        public bool ControlsLocked => !RunStarted || IsOver;

        [Tooltip("Players that must be present before the run starts (and the hazard begins to rise).")]
        [SerializeField] int requiredPlayers = 2;
        public int RequiredPlayers => requiredPlayers;

        InflatableCharacter[] players;
        float baselineY;
        float maxAltitude;
        float nextPlayerScan;   // throttle the pre-start player scan (joining can take a while)

        GUIStyle bannerStyle, subtitleStyle, hintStyle, readoutStyle;

        void Awake()
        {
            Instance = this;
            localState = RunState.Playing;
            Time.timeScale = 1f; // a reloaded scene always starts running, even if we froze before restarting
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // Try to start immediately — locally both players exist by now, so the run begins on frame one.
            // In the networked scene this finds nobody yet and TryStartRun keeps polling in Update.
            RefreshPlayers();
            TryStartRun();
        }

        /// <summary>Networked run coordinator attaches itself here (called when the link spawns).</summary>
        public void AttachNetwork(IRunNetwork network)
        {
            net = network;
            net.Restarted += OnNetworkRestarted;
        }

        public void DetachNetwork(IRunNetwork network)
        {
            if (net != network) return;
            net.Restarted -= OnNetworkRestarted;
            net = null;
        }

        /// <summary>
        /// Networked client: the rising hazard should mirror the host's authoritative height (returned in
        /// <paramref name="y"/>, NaN until the host's first publish) instead of self-rising. Returns false on
        /// the host and in local play, where the hazard drives itself.
        /// </summary>
        public bool TryGetNetworkHazardHeight(out float y)
        {
            if (net != null && !net.IsServer) { y = net.HazardHeight; return true; }
            y = 0f;
            return false;
        }

        /// <summary>Host: replicate the rising hazard's height to clients. No-op on clients and in local play.</summary>
        public void PublishHazardHeight(float y)
        {
            if (net != null && net.IsServer) net.ServerPublishHazardHeight(y);
        }

        public void ReportWin()
        {
            if (!RunStarted || State != RunState.Playing) return;
            if (net != null)
            {
                if (net.IsServer) net.ServerSetState(RunState.Won); // host authority; clients mirror the SyncVar
                return;
            }
            localState = RunState.Won;
            Time.timeScale = 0f;
            Debug.Log($"[InflatableIdiots] Run won — both players reached the goal. Altitude {maxAltitude:F1} m.");
        }

        public void ReportLoss()
        {
            if (!RunStarted || State != RunState.Playing) return;
            if (net != null)
            {
                if (net.IsServer) net.ServerSetState(RunState.Lost);
                return;
            }
            localState = RunState.Lost;
            Time.timeScale = 0f;
            Debug.Log($"[InflatableIdiots] Run lost — a player touched the rising hazard. Altitude {maxAltitude:F1} m.");
        }

        void Update()
        {
            if (!RunStarted)
            {
                // Wait for everyone to join before the run (and the hazard) starts. Throttled — the wait can
                // last as long as it takes a friend to connect. Uses unscaled time so it ticks even if frozen.
                if (Time.unscaledTime >= nextPlayerScan)
                {
                    nextPlayerScan = Time.unscaledTime + 0.25f;
                    RefreshPlayers();
                    TryStartRun();
                }
                return;
            }

            // Track peak altitude while the run is live (frozen once it ends).
            if (State == RunState.Playing)
                maxAltitude = Mathf.Max(maxAltitude, HighestPlayerY() - baselineY);

            // R restarts at any time. Works while frozen because Update ignores timeScale, and the New
            // Input System processes input regardless of timeScale.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
        }

        void RefreshPlayers()
        {
            players = FindObjectsByType<InflatableCharacter>(FindObjectsSortMode.None);
        }

        void TryStartRun()
        {
            if (RunStarted) return;
            if (players == null || players.Length < requiredPlayers) return;

            RunStarted = true;
            // Baseline at the spawn height, so altitude reads ~0 at the bottom and grows as they climb.
            baselineY = HighestPlayerY();
            maxAltitude = 0f;
        }

        float HighestPlayerY()
        {
            float h = float.NegativeInfinity;
            if (players != null)
                foreach (var p in players)
                    if (p != null) h = Mathf.Max(h, p.transform.position.y);
            return float.IsNegativeInfinity(h) ? baselineY : h;
        }

        void Restart()
        {
            // Networked: reset the run IN PLACE (either player) — reloading would drop the session + host GUI.
            if (net != null)
            {
                net.RequestRestart();
                return;
            }

            // Local: reload the scene (resets everything for free).
            Time.timeScale = 1f; // LoadScene doesn't reset timeScale; do it before reloading
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx >= 0)
                SceneManager.LoadScene(idx);
            else
                Debug.LogWarning("[InflatableIdiots] Active scene isn't in Build Settings; can't reload. Re-run Build Test Scene.");
        }

        // Networked restart broadcast: the host already reset players + budget + state; each peer resets its
        // own local rising hazard here and re-baselines altitude. RunStarted stays true (nobody left).
        void OnNetworkRestarted()
        {
            foreach (var hazard in FindObjectsByType<RisingHazard>(FindObjectsSortMode.None))
                hazard.ResetToStart();
            // Clear the goal's win latch + tracked occupants, else last session's win blocks a new one.
            foreach (var goal in FindObjectsByType<GoalZone>(FindObjectsSortMode.None))
                goal.ResetForRestart();
            RefreshPlayers();
            baselineY = HighestPlayerY();
            maxAltitude = 0f;
        }

        void OnGUI()
        {
            EnsureStyles();

            if (!RunStarted)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 52f), "Waiting for both players…", hintStyle);
                return;
            }

            // Live altitude readout (top-left) while the run is live.
            if (State == RunState.Playing)
                GUI.Label(new Rect(16f, 12f, 400f, 44f), $"Altitude: {maxAltitude:F1} m", readoutStyle);

            if (State == RunState.Won)
                EndBanner("Solved!", Color.white);
            else if (State == RunState.Lost)
                EndBanner("Caught!", new Color(1f, 0.5f, 0.4f));

            GUI.Label(new Rect(0f, 12f, Screen.width, 52f), "Press R to restart", hintStyle);
        }

        void EndBanner(string title, Color color)
        {
            bannerStyle.normal.textColor = color;
            GUI.Label(new Rect(0f, Screen.height * 0.3f, Screen.width, 90f), title, bannerStyle);
            GUI.Label(new Rect(0f, Screen.height * 0.3f + 78f, Screen.width, 48f),
                $"Altitude reached: {maxAltitude:F1} m", subtitleStyle);
        }

        void EnsureStyles()
        {
            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 52,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
            }
            if (subtitleStyle == null)
            {
                subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                subtitleStyle.normal.textColor = Color.white;
            }
            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
                hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            }
            if (readoutStyle == null)
            {
                readoutStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
                readoutStyle.normal.textColor = Color.white;
            }
        }
    }
}
