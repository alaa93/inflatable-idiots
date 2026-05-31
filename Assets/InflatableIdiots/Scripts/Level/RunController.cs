using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using InflatableIdiots.Player;

namespace InflatableIdiots.Level
{
    public enum RunState { Playing, Won, Lost }

    /// <summary>
    /// Owns the win/lose state for a single run. GoalZone reports a win; RisingHazard reports a loss.
    /// On either, it freezes the whole scene via Time.timeScale = 0 — which stops the rising hazard AND
    /// the players' controls in one stroke — and shows the end banner. R restarts the scene at ANY time.
    ///
    /// Also tracks altitude: the peak height any player reaches above the start, shown live during play
    /// and frozen as "Altitude reached: X m" on the end screen. Placeholder OnGUI UI for now.
    /// </summary>
    public class RunController : MonoBehaviour
    {
        public static RunController Instance { get; private set; }
        public RunState State { get; private set; }
        public bool IsOver => State != RunState.Playing;
        public float Altitude => maxAltitude;

        InflatableCharacter[] players;
        float baselineY;
        float maxAltitude;

        GUIStyle bannerStyle, subtitleStyle, hintStyle, readoutStyle;

        void Awake()
        {
            Instance = this;
            State = RunState.Playing;
            Time.timeScale = 1f; // a reloaded scene always starts running, even if we froze before restarting
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            // Players exist by Start (created active by the builder). Baseline at the spawn height, so
            // altitude reads ~0 at the bottom and grows as they climb.
            players = FindObjectsByType<InflatableCharacter>(FindObjectsSortMode.None);
            baselineY = HighestPlayerY();
            maxAltitude = 0f;
        }

        public void ReportWin()
        {
            if (State != RunState.Playing) return;
            State = RunState.Won;
            Time.timeScale = 0f;
            Debug.Log($"[InflatableIdiots] Run won — both players reached the goal. Altitude {maxAltitude:F1} m.");
        }

        public void ReportLoss()
        {
            if (State != RunState.Playing) return;
            State = RunState.Lost;
            Time.timeScale = 0f;
            Debug.Log($"[InflatableIdiots] Run lost — a player touched the rising hazard. Altitude {maxAltitude:F1} m.");
        }

        void Update()
        {
            // Track peak altitude while the run is live (frozen once it ends).
            if (State == RunState.Playing)
                maxAltitude = Mathf.Max(maxAltitude, HighestPlayerY() - baselineY);

            // R restarts at any time. Works while frozen because Update ignores timeScale, and the New
            // Input System processes input regardless of timeScale.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
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
            Time.timeScale = 1f; // LoadScene doesn't reset timeScale; do it before reloading
            int idx = SceneManager.GetActiveScene().buildIndex;
            if (idx >= 0)
                SceneManager.LoadScene(idx);
            else
                Debug.LogWarning("[InflatableIdiots] Active scene isn't in Build Settings; can't reload. Re-run Build Test Scene.");
        }

        void OnGUI()
        {
            EnsureStyles();

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
