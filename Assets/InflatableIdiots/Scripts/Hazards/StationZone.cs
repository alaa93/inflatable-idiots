using System.Collections.Generic;
using UnityEngine;
using InflatableIdiots.Player;

namespace InflatableIdiots.Hazards
{
    /// <summary>
    /// Marks a "puzzle station" volume. While any player is inside one, <see cref="RisingHazard"/> relents (uses
    /// its slow in-station speed) so the puzzle is fair; in the transitions between stations no zone is occupied,
    /// so the hazard chases at its faster transition speed. Pure trigger + per-zone occupancy; the hazard reads
    /// the static aggregate <see cref="AnyOccupied"/>.
    ///
    /// Per-zone sets (not one shared set) so overlapping stations can't desync the count: a player leaving zone A
    /// while still inside zone B keeps B occupied.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StationZone : MonoBehaviour
    {
        static readonly List<StationZone> zones = new List<StationZone>();
        readonly HashSet<InflatableCharacter> inside = new HashSet<InflatableCharacter>();

        /// <summary>True while at least one player occupies any station zone.</summary>
        public static bool AnyOccupied
        {
            get
            {
                foreach (var z in zones)
                {
                    z.inside.RemoveWhere(p => p == null); // a teleport/restart may skip OnTriggerExit2D
                    if (z.inside.Count > 0) return true;
                }
                return false;
            }
        }

        void OnEnable() => zones.Add(this);
        void OnDisable()
        {
            zones.Remove(this);
            inside.Clear();
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
    }
}
