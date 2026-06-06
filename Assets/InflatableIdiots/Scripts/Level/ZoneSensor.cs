using System.Collections.Generic;
using UnityEngine;
using InflatableIdiots.Player;

namespace InflatableIdiots.Level
{
    /// <summary>
    /// Host-authoritative trigger sensor: while enough QUALIFYING players occupy it, it raises a puzzle flag on
    /// <see cref="RunController"/>'s synced bitmask. The single primitive behind every co-op switch in the ladder:
    /// <list type="bullet">
    /// <item>a HIGH switch only a floating (inflated) player can reach → <see cref="Qualifier.Inflated"/>, count 1</item>
    /// <item>a LOW plate only a heavy (deflated) player can hold down → <see cref="Qualifier.Deflated"/>, count 1</item>
    /// <item>a lift column that only powers when BOTH players step in → <see cref="Qualifier.AnyPlayer"/>, count 2</item>
    /// <item>a "both reached this ledge" progress check → <see cref="Qualifier.AnyPlayer"/>, count 2</item>
    /// </list>
    /// Only the authority writes the flag (<see cref="RunController.PuzzleAuthority"/>); every machine tints the
    /// visual from the synced flag so it lights up the same on both screens. Occupancy is tracked everywhere
    /// (cheap, harmless) — clients just don't get a vote.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ZoneSensor : MonoBehaviour
    {
        public enum Qualifier { AnyPlayer, Inflated, Deflated }

        [SerializeField] int flagId;
        [SerializeField] Qualifier qualifier = Qualifier.AnyPlayer;
        [Tooltip("Inflated requires InflationK >= this; Deflated requires InflationK <= (1 - this).")]
        [SerializeField, Range(0.5f, 0.95f)] float inflationThreshold = 0.6f;
        [SerializeField] int requiredCount = 1;

        [Header("Visual")]
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Color idleColor = new Color(0.65f, 0.30f, 0.30f, 0.95f);
        [SerializeField] Color activeColor = new Color(0.40f, 0.95f, 0.50f, 1f);

        readonly HashSet<InflatableCharacter> inside = new HashSet<InflatableCharacter>();

        /// <summary>Editor builder hook.</summary>
        public void Configure(int flag, Qualifier q, int count, SpriteRenderer renderer, Color idle, Color active, float threshold = 0.6f)
        {
            flagId = flag;
            qualifier = q;
            requiredCount = count;
            visual = renderer;
            idleColor = idle;
            activeColor = active;
            inflationThreshold = threshold;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.GetComponentInParent<InflatableCharacter>();
            if (p != null) inside.Add(p);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var p = other.GetComponentInParent<InflatableCharacter>();
            if (p != null) inside.Remove(p);
        }

        bool Qualifies(InflatableCharacter p)
        {
            switch (qualifier)
            {
                case Qualifier.Inflated: return p.InflationK >= inflationThreshold;
                case Qualifier.Deflated: return p.InflationK <= 1f - inflationThreshold;
                default: return true;
            }
        }

        void FixedUpdate()
        {
            var run = RunController.Instance;
            if (run == null) return;

            // Only the authority decides the flag; clients mirror it through the synced bitmask.
            if (run.PuzzleAuthority)
            {
                inside.RemoveWhere(p => p == null);
                int n = 0;
                foreach (var p in inside)
                    if (Qualifies(p)) n++;
                run.SetPuzzleFlag(flagId, n >= requiredCount);
            }

            if (visual != null)
                visual.color = run.GetPuzzleFlag(flagId) ? activeColor : idleColor;
        }
    }
}
