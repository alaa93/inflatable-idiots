using System.Collections.Generic;
using UnityEngine;

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

        readonly HashSet<Rigidbody2D> bodies = new HashSet<Rigidbody2D>();
        Vector2 dir = Vector2.up;

        void Awake() => RecomputeDir();
        void OnValidate() => RecomputeDir();
        void RecomputeDir() => dir = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.up;

        void OnTriggerEnter2D(Collider2D other)
        {
            // Static geometry (ground, housing, perch) has no attached body → ignored.
            var rb = other.attachedRigidbody;
            if (rb != null) bodies.Add(rb);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb != null) bodies.Remove(rb);
        }

        void FixedUpdate()
        {
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
