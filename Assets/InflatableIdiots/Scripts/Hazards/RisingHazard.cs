using UnityEngine;
using InflatableIdiots.Level;
using InflatableIdiots.Player;

namespace InflatableIdiots.Hazards
{
    /// <summary>
    /// A wide hazard bar (think rising lava) that climbs at a constant speed. Touching it with EITHER
    /// player ends the run (reports a loss to <see cref="RunController"/>, which then freezes the scene).
    /// Moves via a kinematic Rigidbody2D so trigger detection stays reliable while it travels.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class RisingHazard : MonoBehaviour
    {
        [Tooltip("Constant rise speed in units/second.")]
        [SerializeField] float speed = 0.1f;

        Rigidbody2D rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        void FixedUpdate()
        {
            // Stop once the run is over (the timeScale freeze already halts us; this also covers a win).
            if (RunController.Instance != null && RunController.Instance.IsOver) return;
            rb.MovePosition(rb.position + Vector2.up * speed * Time.fixedDeltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<InflatableCharacter>() == null) return; // only players end the run
            RunController.Instance?.ReportLoss();
        }
    }
}
