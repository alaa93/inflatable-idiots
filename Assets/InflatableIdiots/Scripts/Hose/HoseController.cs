using UnityEngine;

namespace InflatableIdiots.Hose
{
    /// <summary>
    /// The always-connected air hose between the two players.
    ///
    /// MVP scope ("simplest possible"): a short chain of featherweight Rigidbody2D nodes linked by
    /// soft SpringJoint2Ds, drawn with a LineRenderer. NOT a Verlet rope. It is a visual tether plus
    /// a soft length constraint, and it doubles as (a) the implicit camera constraint via max stretch
    /// and (b) the kite-string for the upcoming Fan Trap.
    ///
    /// The node/spring chain is built at runtime in Start so the saved scene stays clean (just this
    /// GameObject, a LineRenderer, and two serialized endpoint references). UV-scroll airflow direction
    /// is intentionally deferred — see SetupLine for where it would hook in.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class HoseController : MonoBehaviour
    {
        [Header("Endpoints")]
        [SerializeField] Rigidbody2D endA;
        [SerializeField] Rigidbody2D endB;

        [Header("Rope shape")]
        [Tooltip("Intermediate physics nodes. MVP called for a handful (2-3 springs); a few more drape " +
                 "more smoothly. Each node is featherweight, so cost is negligible. Springs = segments + 1.")]
        [SerializeField, Range(1, 16)] int segments = 6;
        [Tooltip("Rest length as a multiple of the initial straight-line distance between endpoints. " +
                 ">1 leaves slack so the hose sags under gravity instead of hanging taut.")]
        [SerializeField] float slack = 1.25f;

        [Header("Node physics")]
        [Tooltip("Keep this small. The whole rope hangs off the players through the springs, and a fully " +
                 "inflated player weighs almost nothing (mass 0.1 * gravityScale 0.15) — heavy nodes pin it down.")]
        [SerializeField] float nodeMass = 0.02f;
        [Tooltip("Low so the rope's weight barely loads the players. Raise for more droop at the cost of jump height.")]
        [SerializeField] float nodeGravityScale = 0.2f;
        [SerializeField] float nodeDrag = 1f;

        [Header("Spring")]
        [Tooltip("Spring stiffness in Hz. Lower = softer / stretchier tether (gentler tug on the players).")]
        [SerializeField] float springFrequency = 4f;
        [SerializeField, Range(0f, 1f)] float springDamping = 0.7f;

        [Header("Interaction (forward-looking — off for MVP visual)")]
        [Tooltip("Give nodes small colliders so the hose can catch on obstacles (e.g. the Fan Trap). " +
                 "Off by default to avoid jitter/self-snag while validating the look.")]
        [SerializeField] bool nodeColliders = false;
        [SerializeField] float nodeColliderRadius = 0.1f;

        [Header("Render")]
        [SerializeField] float width = 0.12f;
        [SerializeField] Color colorA = new Color(0.85f, 0.85f, 0.9f);
        [SerializeField] Color colorB = new Color(0.6f, 0.7f, 0.85f);

        LineRenderer line;
        Transform[] chain;   // [endA, node0, node1, ..., endB] — drawn in this order
        bool built;

        /// <summary>Editor/runtime injection of the two endpoint bodies (called by TestSceneBuilder).</summary>
        public void Initialize(Rigidbody2D a, Rigidbody2D b)
        {
            endA = a;
            endB = b;
        }

        void Awake()
        {
            line = GetComponent<LineRenderer>();
        }

        void Start()
        {
            if (endA == null || endB == null)
            {
                Debug.LogWarning("[InflatableIdiots] HoseController has no endpoints; disabling.", this);
                enabled = false;
                return;
            }
            BuildRope();
        }

        void BuildRope()
        {
            int springCount = segments + 1;
            Vector2 a = endA.position;
            Vector2 b = endB.position;
            float restTotal = Vector2.Distance(a, b) * Mathf.Max(1f, slack);
            float segLen = restTotal / springCount;

            chain = new Transform[segments + 2];
            chain[0] = endA.transform;
            chain[segments + 1] = endB.transform;

            var nodes = new Rigidbody2D[segments];
            for (int i = 0; i < segments; i++)
            {
                float t = (i + 1f) / (segments + 1f);
                Vector2 pos = Vector2.Lerp(a, b, t);

                var go = new GameObject($"HoseNode_{i}");
                go.transform.SetParent(transform, worldPositionStays: true);
                go.transform.position = pos;

                var rb = go.AddComponent<Rigidbody2D>();
                rb.mass = nodeMass;
                rb.gravityScale = nodeGravityScale;
                rb.linearDamping = nodeDrag;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;

                if (nodeColliders)
                {
                    var col = go.AddComponent<CircleCollider2D>();
                    col.radius = nodeColliderRadius;
                }

                nodes[i] = rb;
                chain[i + 1] = go.transform;
            }

            // Link the chain so every joint lives on a node (players stay pristine):
            // node[0]->endA, node[i]->node[i-1], and finally node[last]->endB to close it.
            for (int i = 0; i < segments; i++)
            {
                Rigidbody2D prev = (i == 0) ? endA : nodes[i - 1];
                AddSpring(nodes[i], prev, segLen);
            }
            AddSpring(nodes[segments - 1], endB, segLen);

            SetupLine();
            built = true;
        }

        void AddSpring(Rigidbody2D host, Rigidbody2D connected, float distance)
        {
            var joint = host.gameObject.AddComponent<SpringJoint2D>();
            joint.connectedBody = connected;
            // Anchor BOTH ends at body centers. Critical: the players jitter their transform rotation +
            // scale during an air-transfer stalemate (InflatableCharacter.LateUpdate). An auto-configured,
            // off-center connected anchor would swing in world space every frame as the player jitters,
            // pumping energy into the rope so it stretches forever while both triggers are held. A center
            // anchor (0,0) is invariant to rotation/scale, so the visual jitter can't reach the physics.
            joint.anchor = Vector2.zero;
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = Vector2.zero;
            joint.autoConfigureDistance = false;
            joint.distance = distance;
            joint.frequency = springFrequency;
            joint.dampingRatio = springDamping;
            joint.enableCollision = false; // adjacent links never collide with each other
        }

        void SetupLine()
        {
            line.useWorldSpace = true;
            line.positionCount = chain.Length;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.startColor = colorA;
            line.endColor = colorB;
            // Sprites/Default is always-included and respects the LineRenderer's vertex-color gradient
            // in URP. UV-scroll airflow (scrolling _MainTex offset) would later swap this for a
            // custom material; deferred per MVP scope.
            if (line.sharedMaterial == null)
                line.material = new Material(Shader.Find("Sprites/Default"));
            UpdateLine();
        }

        void LateUpdate()
        {
            if (built) UpdateLine();
        }

        void UpdateLine()
        {
            for (int i = 0; i < chain.Length; i++)
                line.SetPosition(i, chain[i].position);
        }

        void OnDrawGizmos()
        {
            // Edit-mode hint only: the real rope exists at play. Draw a straight tether preview.
            if (built || endA == null || endB == null) return;
            Gizmos.color = new Color(0.7f, 0.8f, 0.95f, 0.6f);
            Gizmos.DrawLine(endA.transform.position, endB.transform.position);
        }
    }
}
