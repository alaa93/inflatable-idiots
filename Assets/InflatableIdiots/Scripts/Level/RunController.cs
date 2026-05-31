using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace InflatableIdiots.Level
{
    public enum RunState { Playing, Won, Lost }

    /// <summary>
    /// Owns the win/lose state for a single run. GoalZone reports a win; RisingHazard reports a loss.
    /// On either, it freezes the whole scene via Time.timeScale = 0 — which stops the rising hazard AND
    /// the players' controls in one stroke — and shows a banner. R restarts the scene at ANY time.
    ///
    /// Placeholder OnGUI UI for now: this is the seed of the real end-screen (it grows the
    /// "Altitude reached" score next).
    /// </summary>
    public class RunController : MonoBehaviour
    {
        public static RunController Instance { get; private set; }
        public RunState State { get; private set; }
        public bool IsOver => State != RunState.Playing;

        GUIStyle bannerStyle, hintStyle;

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

        public void ReportWin()
        {
            if (State != RunState.Playing) return;
            State = RunState.Won;
            Time.timeScale = 0f;
            Debug.Log("[InflatableIdiots] Run won — both players reached the goal.");
        }

        public void ReportLoss()
        {
            if (State != RunState.Playing) return;
            State = RunState.Lost;
            Time.timeScale = 0f;
            Debug.Log("[InflatableIdiots] Run lost — a player touched the rising hazard.");
        }

        void Update()
        {
            // R restarts at any time. Works while frozen because Update ignores timeScale, and the New
            // Input System processes input regardless of timeScale.
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
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
            if (State == RunState.Won)
                Banner("Solved!", Color.white);
            else if (State == RunState.Lost)
                Banner("Caught!", new Color(1f, 0.5f, 0.4f));

            GUI.Label(new Rect(0f, 12f, Screen.width, 52f), "Press R to restart", hintStyle);
        }

        void Banner(string text, Color color)
        {
            bannerStyle.normal.textColor = color;
            GUI.Label(new Rect(0f, Screen.height * 0.3f, Screen.width, 90f), text, bannerStyle);
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
            if (hintStyle == null)
            {
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 36, alignment = TextAnchor.MiddleCenter };
                hintStyle.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
            }
        }
    }
}
