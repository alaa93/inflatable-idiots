using System.Collections.Generic;
using FishNet;
using FishNet.Managing.Timing;
using UnityEngine;

namespace InflatableIdiots.View
{
    /// <summary>
    /// On-screen performance readout drawn with IMGUI for quick perf testing.
    /// Shows render FPS plus FishNet network stats (role, tick rate, RTT) when a
    /// NetworkManager is present. Drop on any GameObject in the scene.
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }

        [SerializeField] Corner corner = Corner.TopLeft;
        [SerializeField] float sampleInterval = 0.5f;
        [SerializeField] int fontSize = 22;
        [SerializeField] bool showNetworkStats = true;
        [SerializeField] bool persistAcrossScenes = true;

        float accumTime;
        int accumFrames;
        float fps;
        float msPerFrame;

        GUIStyle style;
        readonly Vector2 margin = new Vector2(10f, 10f);
        readonly List<(string text, Color color)> lines = new List<(string, Color)>(4);

        void Awake()
        {
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            // Use unscaled time so a paused/slowed game still reports real frame rate.
            accumTime += Time.unscaledDeltaTime;
            accumFrames++;

            if (accumTime >= sampleInterval)
            {
                fps = accumFrames / accumTime;
                msPerFrame = (accumTime / accumFrames) * 1000f;
                accumTime = 0f;
                accumFrames = 0;
            }
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft
                };
            }

            BuildLines();

            const float width = 280f;
            float lineH = fontSize + 6f;
            float blockH = lines.Count * lineH;

            float x = corner == Corner.TopRight || corner == Corner.BottomRight
                ? Screen.width - width - margin.x
                : margin.x;
            float yTop = corner == Corner.BottomLeft || corner == Corner.BottomRight
                ? Screen.height - blockH - margin.y
                : margin.y;

            for (int i = 0; i < lines.Count; i++)
            {
                var rect = new Rect(x, yTop + i * lineH, width, lineH);
                DrawShadowed(rect, lines[i].text, lines[i].color);
            }
        }

        void BuildLines()
        {
            lines.Clear();

            Color fpsColor = fps >= 55f ? Color.green : fps >= 30f ? Color.yellow : Color.red;
            lines.Add(($"{fps:0.} FPS  ({msPerFrame:0.0} ms)", fpsColor));

            if (!showNetworkStats)
                return;

            // NetworkManager / TimeManager only exist once FishNet is initialized in the scene.
            TimeManager tm = InstanceFinder.TimeManager;
            if (tm == null)
                return;

            bool server = InstanceFinder.IsServerStarted;
            bool client = InstanceFinder.IsClientStarted;
            string role = server && client ? "Host" : server ? "Server" : client ? "Client" : "Offline";
            Color roleColor = server || client ? Color.cyan : Color.gray;
            lines.Add(($"{role}  ·  {tm.TickRate} tick/s", roleColor));

            // RTT is the client's round-trip to the server, in ms (≈0 on the host's own client).
            if (client)
            {
                long rtt = tm.RoundTripTime;
                Color rttColor = rtt <= 60 ? Color.green : rtt <= 120 ? Color.yellow : Color.red;
                lines.Add(($"RTT {rtt} ms", rttColor));
            }
        }

        void DrawShadowed(Rect rect, string text, Color color)
        {
            var shadow = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
            style.normal.textColor = Color.black;
            GUI.Label(shadow, text, style);

            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }
    }
}
