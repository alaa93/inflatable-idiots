using UnityEngine;

namespace InflatableIdiots.View
{
    /// <summary>
    /// A world-anchored puzzle label: a short 2-word title + a ≤4-word how-to, drawn via OnGUI projected from
    /// this object's world position so it tracks the puzzle as the <see cref="FollowCamera"/> pans/zooms. OnGUI
    /// (rather than TextMeshPro) means zero font-asset setup — it just works in code-generated graybox scenes
    /// and in the standalone. Text is sized to a constant WORLD height so it reads as anchored to the level, and
    /// off-screen labels cull themselves so only the chamber you're in shows.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] string title = "";
        [SerializeField] string description = "";
        [Tooltip("Title cap height in world units (scales with camera zoom).")]
        [SerializeField] float worldTitleHeight = 0.85f;
        [SerializeField] float worldDescHeight = 0.5f;
        [SerializeField] Color titleColor = new Color(1f, 0.96f, 0.7f);
        [SerializeField] Color descColor = new Color(1f, 1f, 1f, 0.8f);

        public void Configure(string newTitle, string newDescription)
        {
            title = newTitle;
            description = newDescription;
        }

        Camera cam;
        GUIStyle titleStyle, descStyle;

        void OnGUI()
        {
            var c = cam != null ? cam : (cam = Camera.main);
            if (c == null) return;

            Vector3 sp = c.WorldToScreenPoint(transform.position);
            if (sp.z <= 0f) return; // behind the camera

            float guiY = Screen.height - sp.y; // OnGUI y is top-down
            if (sp.x < -200f || sp.x > Screen.width + 200f || guiY < -200f || guiY > Screen.height + 200f) return;

            float pxPerWorld = c.orthographic ? Screen.height / (2f * Mathf.Max(0.1f, c.orthographicSize)) : 50f;
            int titleSize = Mathf.Clamp(Mathf.RoundToInt(worldTitleHeight * pxPerWorld), 9, 60);
            int descSize = Mathf.Clamp(Mathf.RoundToInt(worldDescHeight * pxPerWorld), 8, 44);

            EnsureStyles();
            titleStyle.fontSize = titleSize;
            descStyle.fontSize = descSize;

            const float w = 460f;
            float x = sp.x - w * 0.5f;
            if (!string.IsNullOrEmpty(title))
                DrawShadowed(new Rect(x, guiY - titleSize - 2f, w, titleSize + 6f), title, titleStyle, titleColor);
            if (!string.IsNullOrEmpty(description))
                DrawShadowed(new Rect(x, guiY + 2f, w, descSize + 6f), description, descStyle, descColor);
        }

        static void DrawShadowed(Rect r, string text, GUIStyle style, Color color)
        {
            var prev = style.normal.textColor;
            // Dark drop-shadow for legibility against the busy graybox, then the real text on top.
            style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
            GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = prev;
        }

        void EnsureStyles()
        {
            if (titleStyle == null)
                titleStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (descStyle == null)
                descStyle = new GUIStyle { alignment = TextAnchor.MiddleCenter };
        }
    }
}
