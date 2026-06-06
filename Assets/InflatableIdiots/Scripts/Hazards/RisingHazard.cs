using UnityEngine;
using InflatableIdiots.Level;
using InflatableIdiots.Player;

namespace InflatableIdiots.Hazards
{
    /// <summary>
    /// A wide hazard bar (think rising lava) that climbs at a constant speed. Touching it with EITHER
    /// player ends the run (reports a loss to <see cref="RunController"/>, which then freezes the scene).
    /// Moves via a kinematic Rigidbody2D so trigger detection stays reliable while it travels.
    ///
    /// Networked play: the host drives the climb and replicates its height; clients DON'T self-rise but
    /// mirror that height (via <see cref="RunController.TryGetNetworkHazardHeight"/>). One source of truth —
    /// so the hazard can't drift apart from independent start-gate timing, and the host (the only authority
    /// that ends the run) shows everyone the same bar. Local play is unchanged: it just self-rises.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RisingHazard : MonoBehaviour
    {
        [Tooltip("Constant rise speed in units/second.")]
        [SerializeField] float speed = 0.1f;

        Rigidbody2D rb;
        Vector2 startPosition;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            startPosition = rb.position;
        }

        /// <summary>Send the hazard back to its authored start height (used by an in-place networked restart).</summary>
        public void ResetToStart() => rb.position = startPosition;

        void FixedUpdate()
        {
            var run = RunController.Instance;
            if (run == null) return;

            // Networked client: mirror the host's authoritative height instead of self-rising. NaN = the host
            // hasn't published yet (run not live) → hold at the authored start until its first value arrives.
            if (run.TryGetNetworkHazardHeight(out float netY))
            {
                rb.MovePosition(new Vector2(rb.position.x, float.IsNaN(netY) ? startPosition.y : netY));
                return;
            }

            // Host / local: hold at the start until the run begins (networked: both players joined), and stop
            // once it's over (the timeScale freeze already halts us locally; this also covers a win).
            if (!run.RunStarted || run.IsOver) return;
            Vector2 next = rb.position + Vector2.up * speed * Time.fixedDeltaTime;
            rb.MovePosition(next);
            run.PublishHazardHeight(next.y); // host → clients (no-op locally); publish the commanded target
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (RunController.Instance == null || !RunController.Instance.RunStarted) return; // run not live yet
            if (other.GetComponentInParent<InflatableCharacter>() == null) return; // only players end the run
            RunController.Instance.ReportLoss();
        }
    }
}
