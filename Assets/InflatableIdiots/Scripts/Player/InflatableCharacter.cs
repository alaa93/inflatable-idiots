using UnityEngine;
using UnityEngine.InputSystem;

namespace InflatableIdiots.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class InflatableCharacter : MonoBehaviour
    {
        [Header("Pair")]
        [SerializeField] AirPair pair;
        [SerializeField] bool isP1;

        [Header("Body state @ extremes")]
        [SerializeField] float massInflated = 0.1f;
        [SerializeField] float massDeflated = 3.0f;
        [SerializeField] float gravInflated = 0.15f;
        [SerializeField] float gravDeflated = 2.5f;
        [SerializeField] float scaleInflated = 2.5f;
        [SerializeField] float scaleDeflated = 0.6f;

        [Header("Movement")]
        [SerializeField] float walkSpeed = 6f;
        [SerializeField] float jumpSpeedDeflated = 4f;
        [SerializeField] float jumpSpeedInflated = 10f;

        [Header("Ground check")]
        [SerializeField] LayerMask groundLayers;
        [SerializeField] Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
        [SerializeField] float groundCheckRadius = 0.45f;
        [SerializeField] float groundCheckDistance = 0.08f;
        [SerializeField, Range(0f, 1f)] float minGroundNormalY = 0.5f;

        [Header("Stalemate feedback")]
        [SerializeField] float stalemateScaleAmplitude = 0.08f;
        [SerializeField] float stalemateRotationAmplitude = 6f;
        [SerializeField] float stalemateFrequency = 15f;

        public float TriggerValue { get; private set; }

        /// <summary>
        /// Current inflation, 0 (deflated / heavy / anchored) .. 1 (inflated / light / floaty), derived from the
        /// air budget exactly as the mass/gravity/scale lerps are. Read by <see cref="Level.ZoneSensor"/> to
        /// qualify a player as "light" or "heavy" for the co-op switch/plate puzzles.
        /// </summary>
        public float InflationK => currentInflationK;

        // --- Networking seam (additive; defaults preserve local play) ---
        // When false, FixedUpdate skips ground check + walk/jump so a remote client's body can be driven by
        // NetworkTransform without local physics fighting it. UpdatePhysicsState still runs so inflation
        // visuals (mass/gravity/scale) track the budget on every machine.
        public bool SimulatePhysics = true;
        // When set, overrides AirPair as the inflation-budget source (used by the server-authoritative
        // NetworkAirBudget). Null => unchanged local behavior reading from `pair`.
        public System.Func<float> BudgetSource = null;

        // Server-side input injection mirroring the local PlayerInput callbacks (OnMove/OnJump/OnAirTransfer).
        public void SetMoveInput(Vector2 v) => moveInput = v;
        public void QueueJump() => jumpQueued = true;
        public void SetTrigger(float t) => TriggerValue = t;

        /// <summary>
        /// One networked movement step, driven by <see cref="Net.NetworkInflatableController"/>'s prediction
        /// [Replicate] so movement runs per network tick (and replays correctly during reconcile) instead of
        /// FixedUpdate. Shares the exact ground-check/walk/jump/inflation logic with local play. Operates on
        /// the Rigidbody2D directly; the controller wraps that body in a PredictionRigidbody2D for reconcile.
        /// </summary>
        public void Tick(Vector2 move, bool jump)
        {
            moveInput = move;
            if (jump) jumpQueued = true;
            UpdatePhysicsState();
            CheckGround();
            ApplyWalk();
            ApplyJump();
        }

        Rigidbody2D rb;
        Vector2 moveInput;
        bool jumpQueued;
        bool isGrounded;
        float currentInflationK = 0.5f; // even-split default so the very first rendered frame is 50%, before any budget source/FixedUpdate
        float noiseSeed;

        public void Initialize(AirPair airPair, bool playerOne, LayerMask ground)
        {
            pair = airPair;
            isP1 = playerOne;
            groundLayers = ground;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.freezeRotation = true;
            noiseSeed = Random.value * 1000f;
        }

        // Networked players read input via NetworkInflatableController and feed it through the Set* methods,
        // so set this true there to make these local SendMessages callbacks no-ops (one canonical input path).
        public bool IgnoreLocalInputCallbacks = false;

        void OnMove(InputValue v) { if (!IgnoreLocalInputCallbacks) moveInput = v.Get<Vector2>(); }
        void OnJump(InputValue v) { if (!IgnoreLocalInputCallbacks && v.isPressed) jumpQueued = true; }
        void OnAirTransfer(InputValue v) { if (!IgnoreLocalInputCallbacks) TriggerValue = v.Get<float>(); }

        void FixedUpdate()
        {
            // Inflation visuals track the budget on every machine (owner, remote, host).
            UpdatePhysicsState();
            // Only the authority that owns this body's physics runs movement. On a remote client this body
            // is kinematic and driven by NetworkTransform, so local walk/jump are skipped.
            if (!SimulatePhysics) return;
            CheckGround();
            ApplyWalk();
            ApplyJump();
        }

        void LateUpdate()
        {
            // Visual scale/rotation live here so jitter samples at framerate (smoother than
            // FixedUpdate's 50 Hz). FixedUpdate already set currentInflationK.
            float baseScale = Mathf.Lerp(scaleDeflated, scaleInflated, currentInflationK);
            float intensity = pair != null ? pair.StalemateIntensity : 0f;

            if (intensity <= 0f)
            {
                transform.localScale = new Vector3(baseScale, baseScale, 1f);
                transform.rotation = Quaternion.identity;
                return;
            }

            float t = Time.time * stalemateFrequency;
            float jx = (Mathf.PerlinNoise(t, noiseSeed) - 0.5f) * 2f * intensity * stalemateScaleAmplitude;
            float jy = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f * intensity * stalemateScaleAmplitude;
            transform.localScale = new Vector3(baseScale + jx, baseScale + jy, 1f);

            float rj = (Mathf.PerlinNoise(t * 0.7f, noiseSeed + 100f) - 0.5f) * 2f * intensity * stalemateRotationAmplitude;
            transform.rotation = Quaternion.Euler(0f, 0f, rj);
        }

        void CheckGround()
        {
            Vector2 origin = (Vector2)transform.position + Vector2.Scale(groundCheckOffset, transform.localScale);
            float radius = groundCheckRadius * Mathf.Abs(transform.localScale.x);
            float dist = groundCheckDistance * Mathf.Abs(transform.localScale.y);
            var hit = Physics2D.CircleCast(origin, radius, Vector2.down, dist, groundLayers);
            isGrounded = hit.collider != null && hit.normal.y >= minGroundNormalY;
        }

        void UpdatePhysicsState()
        {
            float budget;
            if (BudgetSource != null)
                budget = BudgetSource();                       // networked: server-authoritative budget
            else if (pair != null)
                budget = isP1 ? pair.P1Budget : pair.P2Budget; // local: unchanged path
            else
                budget = 0.5f; // no source yet (networked, before the air-budget link spawns/wires) →
                               // even 50% split so both players load at a matching mid-size, not deflated
            currentInflationK = Mathf.InverseLerp(0.1f, 0.9f, budget);

            rb.mass = Mathf.Lerp(massDeflated, massInflated, currentInflationK);
            rb.gravityScale = Mathf.Lerp(gravDeflated, gravInflated, currentInflationK);
        }

        void ApplyWalk()
        {
            var v = rb.linearVelocity;
            v.x = moveInput.x * walkSpeed;
            rb.linearVelocity = v;
        }

        void ApplyJump()
        {
            if (!jumpQueued) return;
            jumpQueued = false;
            if (!isGrounded) return;
            // Set vertical velocity directly: avoids ForceMode2D.Impulse's implicit divide-by-mass.
            // Inflated state gets a slightly higher kick AND lower gravityScale → naturally floaty.
            float jumpSpeed = Mathf.Lerp(jumpSpeedDeflated, jumpSpeedInflated, currentInflationK);
            var v = rb.linearVelocity;
            v.y = jumpSpeed;
            rb.linearVelocity = v;
        }

        void OnDrawGizmosSelected()
        {
            Vector2 origin = (Vector2)transform.position + Vector2.Scale(groundCheckOffset, transform.localScale);
            float radius = groundCheckRadius * Mathf.Abs(transform.localScale.x);
            float dist = groundCheckDistance * Mathf.Abs(transform.localScale.y);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawWireSphere(origin + Vector2.down * dist, radius);
        }
    }
}
