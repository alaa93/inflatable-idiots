using System.Collections.Generic;
using UnityEngine;
using InflatableIdiots.Player;

namespace InflatableIdiots.Level
{
    /// <summary>
    /// Win trigger for the Fan Trap demo: latches "solved" once <see cref="requiredPlayers"/> distinct
    /// players are inside the zone at the same time. Both players reaching it requires the take-turns
    /// air-share (only one player can be light enough to cross at a time), which is the whole point.
    ///
    /// Feedback is a placeholder "Solved!" banner drawn with OnGUI — the real end-screen replaces this
    /// (see the End-screen task). No restart/freeze yet.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        [Tooltip("How many distinct players must be inside the zone simultaneously to win.")]
        [SerializeField] int requiredPlayers = 2;

        readonly HashSet<InflatableCharacter> inside = new HashSet<InflatableCharacter>();
        bool solved;
        GUIStyle bannerStyle;

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<InflatableCharacter>();
            if (player == null) return; // ignore hose/geometry — only players count
            inside.Add(player);
            if (!solved && inside.Count >= requiredPlayers)
            {
                solved = true;
                Debug.Log("[InflatableIdiots] Fan Trap solved — both players reached the goal!");
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<InflatableCharacter>();
            if (player != null) inside.Remove(player); // 'solved' stays latched once won
        }

        void OnGUI()
        {
            if (!solved) return;
            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                bannerStyle.normal.textColor = Color.white;
            }
            GUI.Label(new Rect(0f, Screen.height * 0.28f, Screen.width, 80f), "Solved!", bannerStyle);
        }
    }
}
