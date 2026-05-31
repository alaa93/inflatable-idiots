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

        Rigidbody2D rb;
        Vector2 moveInput;
        bool jumpQueued;
        bool isGrounded;
        float currentInflationK;
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

        void OnMove(InputValue v) => moveInput = v.Get<Vector2>();
        void OnJump(InputValue v) { if (v.isPressed) jumpQueued = true; }
        void OnAirTransfer(InputValue v) => TriggerValue = v.Get<float>();

        void FixedUpdate()
        {
            CheckGround();
            UpdatePhysicsState();
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
            if (pair == null) return;
            float budget = isP1 ? pair.P1Budget : pair.P2Budget;
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
