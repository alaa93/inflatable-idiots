using System.Collections.Generic;
using UnityEngine;
using InflatableIdiots.Player;
using InflatableIdiots.Level;

namespace InflatableIdiots.Hazards
{
    /// <summary>
    /// An updraft column. While a Rigidbody2D is inside the trigger, pushes it along <see cref="direction"/>
    /// with a constant FORCE (so acceleration is mass-dependent), stopping once the body already moves faster
    /// than <see cref="maxRiseSpeed"/> along that direction.
    ///
    /// The constant force is the whole point of the Fan Trap: a fully inflated player is nearly weightless
    /// (mass 0.1 * gravityScale 0.15) so the fan flings it up like a kite, while a deflated player
    /// (mass 3 * gravityScale 2.5) barely feels it and stays anchored. The hose then tethers the floating
    /// "kite" to the grounded "anchor" — the GDD's hose-as-kite moment. Tune the float threshold via `force`:
    /// a body lifts when force > mass * gravityScale * |Physics2D.gravity|.
    ///
    /// NETWORKED play: a predicted player body (<see cref="InflatableCharacter.SimulatePhysics"/> == false)
    /// is NOT pushed from this FixedUpdate — a raw AddForce outside FishNet's [Replicate] wouldn't be replayed
    /// during reconcile, so the owner's prediction would diverge from the host every tick in the column (the
    /// same class of stutter the host-only hose caused). Instead each player's NetworkInflatableController
    /// samples <see cref="SampleForce"/> inside its predicted tick and queues the force on its
    /// PredictionRigidbody2D. Locally-simulated bodies keep the direct FixedUpdate push below unchanged.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FanZone : MonoBehaviour
    {
        [Tooltip("Push direction (normalized at runtime). Up = a classic updraft.")]
        [SerializeField] Vector2 direction = Vector2.up;
        [Tooltip("Constant force in newtons. Mass-dependent BY DESIGN: light bodies lift, heavy bodies don't. " +
                 "A body floats when force > mass * gravityScale * 9.81.")]
        [SerializeField] float force = 10f;
        [Tooltip("Stop adding force once the body already moves this fast (m/s) along `direction`, so a light " +
                 "body gets carried up steadily instead of flung out the top.")]
        [SerializeField] float maxRiseSpeed = 4f;
        [Tooltip("Optional gate: -1 = always on (the tutorial fan). Otherwise this updraft only blows while the " +
                 "named puzzle flag is set on the host-authoritative RunController board — e.g. a weight-plate " +
                 "pressed (powered fan), or both players standing in the column (the co-op lift).")]
        [SerializeField] int activationFlag = -1;

        /// <summary>Every enabled fan zone, so a predicted player can sample them each tick without a scene scan.</summary>
        public static readonly List<FanZone> All = new List<FanZone>();

        readonly HashSet<Rigidbody2D> bodies = new HashSet<Rigidbody2D>();
        Vector2 dir = Vector2.up;
        Collider2D zone;

        void Awake()
        {
            zone = GetComponent<Collider2D>();
            RecomputeDir();
        }
        void OnValidate() => RecomputeDir();
        void RecomputeDir() => dir = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.up;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>
        /// Whether this updraft is currently blowing. Gated fans read the host-authoritative puzzle board, so
        /// the host and the predicting owner see the same answer each tick — keeping the sampled force
        /// reconcile-safe. -1 flag = always on.
        /// </summary>
        bool IsActive => activationFlag < 0 ||
                         (RunController.Instance != null && RunController.Instance.GetPuzzleFlag(activationFlag));

        /// <summary>Editor builder hook: set the updraft's strength, speed cap, direction and optional gate flag.</summary>
        public void Configure(float newForce, float newMaxRiseSpeed, Vector2 newDirection, int newActivationFlag = -1)
        {
            force = newForce;
            maxRiseSpeed = newMaxRiseSpeed;
            direction = newDirection;
            activationFlag = newActivationFlag;
            RecomputeDir();
        }

        /// <summary>
        /// The updraft force this zone exerts on a body at <paramref name="position"/> moving at
        /// <paramref name="velocity"/> this tick — or zero if the point is outside the column or already rising
        /// at the <see cref="maxRiseSpeed"/> cap. A pure function of the passed state (no trigger membership),
        /// so a networked player can apply it inside its predicted tick and have reconcile replay it
        /// deterministically. Membership is a center-point test, a hair tighter than the FixedUpdate path's
        /// full-collider overlap — negligible for a wide column.
        /// </summary>
        public Vector2 SampleForce(Vector2 position, Vector2 velocity)
        {
            if (!IsActive) return Vector2.zero;
            if (zone == null || !zone.OverlapPoint(position)) return Vector2.zero;
            if (Vector2.Dot(velocity, dir) >= maxRiseSpeed) return Vector2.zero;
            return dir * force;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            // Only push dynamic bodies. Static geometry has no attached body; the kinematic rising hazard
            // would ignore forces anyway, so skip it rather than tracking it pointlessly.
            var rb = other.attachedRigidbody;
            if (rb == null || rb.bodyType != RigidbodyType2D.Dynamic) return;
            // A networked predicted player drives its own physics through the prediction tick and pulls the
            // fan force via SampleForce there; pushing it from FixedUpdate too would double-apply + desync.
            var character = rb.GetComponent<InflatableCharacter>();
            if (character != null && !character.SimulatePhysics) return;
            bodies.Add(rb);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null) bodies.Remove(rb);
        }

        void FixedUpdate()
        {
            if (!IsActive) return; // gated fan off this tick (e.g. nobody on the weight plate / lift empty)
            bodies.RemoveWhere(rb => rb == null);
            foreach (var rb in bodies)
            {
                if (Vector2.Dot(rb.linearVelocity, dir) < maxRiseSpeed)
                    rb.AddForce(dir * force, ForceMode2D.Force);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.9f);
            Vector3 d = direction.sqrMagnitude > 1e-6f ? (Vector3)direction.normalized : Vector3.up;
            Vector3 p = transform.position;
            Gizmos.DrawLine(p, p + d * 1.5f);
            Gizmos.DrawLine(p + d * 1.5f, p + d * 1.2f + (Vector3)(Vector2.Perpendicular(d) * 0.2f));
            Gizmos.DrawLine(p + d * 1.5f, p + d * 1.2f - (Vector3)(Vector2.Perpendicular(d) * 0.2f));
        }
    }
}
