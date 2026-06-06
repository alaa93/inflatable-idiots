using UnityEngine;

namespace InflatableIdiots.Level
{
    /// <summary>
    /// A solid graybox gate (a wall/hatch panel) that opens when its <see cref="requiredFlags"/> are all set on
    /// the host-authoritative puzzle board. The authority computes the open state (with an optional latch) and
    /// writes it to <see cref="gateFlag"/>; every machine then enables/disables the solid collider and dims the
    /// sprite from that synced flag — so the gate blocks/opens identically on both screens.
    ///
    /// Latch options shape the feel:
    /// <list type="bullet">
    /// <item><see cref="latchOncePermanently"/> — once solved it stays open for the run. The forgiving progression
    /// gate between chambers; use this so the playtest is guaranteed traversable end-to-end.</item>
    /// <item><see cref="latchSeconds"/> &gt; 0 — stays open a few seconds after the inputs are satisfied. This is
    /// the "who-holds-the-door" beat: both must coordinate the crossing before it shuts.</item>
    /// <item>both 0/false — momentary; slams shut the instant an input drops.</item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FlagGate : MonoBehaviour
    {
        [SerializeField] int gateFlag;
        [SerializeField] int[] requiredFlags;
        [Tooltip("Stay open this many seconds after the inputs are satisfied (0 = momentary).")]
        [SerializeField] float latchSeconds = 0f;
        [Tooltip("Once opened, stay open for the rest of the run (forgiving progression gate).")]
        [SerializeField] bool latchOncePermanently = false;

        [Header("Visual")]
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Color closedColor = new Color(0.30f, 0.28f, 0.36f, 1f);
        [SerializeField] Color openColor = new Color(0.30f, 0.28f, 0.36f, 0.10f);

        Collider2D solid;
        float openUntil = float.NegativeInfinity;
        bool latchedOpen;

        /// <summary>Editor builder hook.</summary>
        public void Configure(int gate, int[] inputs, float latch, bool once, SpriteRenderer renderer, Color closed, Color open)
        {
            gateFlag = gate;
            requiredFlags = inputs;
            latchSeconds = latch;
            latchOncePermanently = once;
            visual = renderer;
            closedColor = closed;
            openColor = open;
        }

        /// <summary>Clear the latch so an in-place networked restart re-closes a once-latched gate.</summary>
        public void ResetForRestart()
        {
            latchedOpen = false;
            openUntil = float.NegativeInfinity;
        }

        void Awake() => solid = GetComponent<Collider2D>();

        bool InputsSatisfied(RunController run)
        {
            if (requiredFlags == null || requiredFlags.Length == 0) return false;
            foreach (int f in requiredFlags)
                if (!run.GetPuzzleFlag(f)) return false;
            return true;
        }

        void FixedUpdate()
        {
            var run = RunController.Instance;
            if (run == null) return;

            if (run.PuzzleAuthority)
            {
                bool satisfied = InputsSatisfied(run);
                if (satisfied)
                {
                    latchedOpen = true;
                    openUntil = Time.time + latchSeconds;
                }
                bool open = satisfied
                            || (latchOncePermanently && latchedOpen)
                            || (latchSeconds > 0f && Time.time < openUntil);
                run.SetPuzzleFlag(gateFlag, open);
            }

            bool isOpen = run.GetPuzzleFlag(gateFlag);
            if (solid != null) solid.enabled = !isOpen; // open => collider OFF so players pass through
            if (visual != null) visual.color = isOpen ? openColor : closedColor;
        }
    }
}
