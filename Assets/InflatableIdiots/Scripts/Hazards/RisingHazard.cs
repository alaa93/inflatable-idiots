using UnityEngine;
using UnityEngine.InputSystem;
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
        [Tooltip("Rise speed (units/sec) while a player is inside a puzzle station — the hazard relents so the " +
                 "puzzle is fair.")]
        [SerializeField] float speed = 0.12f;
        [Tooltip("Faster rise speed (units/sec) in the transitions BETWEEN stations (nobody in a StationZone) — " +
                 "the chase that makes the climb tense.")]
        [SerializeField] float transitionSpeed = 0.5f;

        /// <summary>
        /// Debug toggle (default ON = lava active). Flip with the L key to switch the hazard off for calm puzzle
        /// iteration, then back on for the pressure pass. Host/local-only: a pure client mirrors the host's
        /// height, so toggling it there does nothing (the host is the authority that climbs and decides "caught").
        /// </summary>
        public static bool Disabled;

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
            // Lava switched off for calm puzzle iteration: hold position, but keep publishing so a client mirrors
            // the freeze instead of drifting off the last value.
            if (Disabled) { run.PublishHazardHeight(rb.position.y); return; }
            // Relent while a player is solving inside a station; chase faster in the transitions between them.
            float spd = StationZone.AnyOccupied ? speed : transitionSpeed;
            Vector2 next = rb.position + Vector2.up * spd * Time.fixedDeltaTime;
            rb.MovePosition(next);
            run.PublishHazardHeight(next.y); // host → clients (no-op locally); publish the commanded target
        }

        void Update()
        {
            // Debug: L toggles the lava on/off for testing (host/local authority; harmless on a pure client).
            if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
                Disabled = !Disabled;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (RunController.Instance == null || !RunController.Instance.RunStarted) return; // run not live yet
            if (other.GetComponentInParent<InflatableCharacter>() == null) return; // only players end the run
            RunController.Instance.ReportLoss();
        }
    }
}
