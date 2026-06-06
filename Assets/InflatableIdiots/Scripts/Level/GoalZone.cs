using System.Collections.Generic;
using UnityEngine;
using InflatableIdiots.Player;

namespace InflatableIdiots.Level
{
    /// <summary>
    /// Win trigger for the Fan Trap demo: reports a win to <see cref="RunController"/> once
    /// <see cref="requiredPlayers"/> distinct players are inside at the same time. Getting both here
    /// requires the take-turns air-share (only one player can be light enough to cross at a time).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalZone : MonoBehaviour
    {
        [Tooltip("How many distinct players must be inside the zone simultaneously to win.")]
        [SerializeField] int requiredPlayers = 2;

        readonly HashSet<InflatableCharacter> inside = new HashSet<InflatableCharacter>();
        bool reported;

        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<InflatableCharacter>();
            if (player == null) return; // ignore hose/geometry — only players count
            inside.Add(player);
            if (!reported && inside.Count >= requiredPlayers)
            {
                reported = true;
                RunController.Instance?.ReportWin();
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var player = other.GetComponentInParent<InflatableCharacter>();
            if (player != null) inside.Remove(player);
        }

        /// <summary>
        /// Clear tracked occupants + the win latch. Used by the in-place networked restart, which doesn't
        /// reload the scene — so without this the previous session's <c>reported</c> latch permanently blocks
        /// a new win, and a teleport-out (which may not fire OnTriggerExit2D) could leave stale occupants.
        /// </summary>
        public void ResetForRestart()
        {
            inside.Clear();
            reported = false;
        }
    }
}
