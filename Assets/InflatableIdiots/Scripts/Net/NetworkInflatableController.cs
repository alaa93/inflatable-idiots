using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;
using UnityEngine.InputSystem;
using InflatableIdiots.Hazards;
using InflatableIdiots.Level;
using InflatableIdiots.Player;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Client-side-predicted driver for one inflatable player (FishNet Prediction V2). The owner predicts
    /// movement locally each tick for instant response; the server runs the same [Replicate] authoritatively
    /// and sends [Reconcile] snapshots that rewind + replay the client. This replaced the old
    /// host-authoritative ServerRpc-input model, which felt laggy on the owner under real latency (confirmed
    /// in the 2-PC playtest 2026-06-02).
    ///
    /// Movement logic itself lives in <see cref="InflatableCharacter.Tick"/> so local (non-networked) play
    /// and networked prediction share ONE code path; this component only routes input → replicate → reconcile.
    /// The physics body is wrapped in a <see cref="PredictionRigidbody2D"/> for reconcile state capture.
    ///
    /// Requires: NetworkObject "Enable Prediction" = on, Prediction Type = Rigidbody2D, a Graphical Object
    /// assigned (child sprite) for smoothing; NetworkTransform removed; TimeManager Physics Mode = TimeManager.
    /// </summary>
    [RequireComponent(typeof(InflatableCharacter))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class NetworkInflatableController : TickNetworkBehaviour
    {
        #region Prediction data.
        public struct ReplicateData : IReplicateData
        {
            public Vector2 Move;
            public bool Jump;
            public float Trigger;

            public ReplicateData(Vector2 move, bool jump, float trigger)
            {
                Move = move;
                Jump = jump;
                Trigger = trigger;
                _tick = 0;
            }

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }

        public struct ReconcileData : IReconcileData
        {
            // Captures the authoritative Rigidbody2D state (position/velocity/etc.) for rewind.
            public PredictionRigidbody2D Body;

            public ReconcileData(PredictionRigidbody2D body)
            {
                Body = body;
                _tick = 0;
            }

            private uint _tick;
            public void Dispose() { }
            public uint GetTick() => _tick;
            public void SetTick(uint value) => _tick = value;
        }
        #endregion

        // Assigned by NetPlayerSpawner on the server; read by NetworkAirBudget to map onto the zero-sum budget.
        readonly SyncVar<int> _slot = new SyncVar<int>();
        public int SlotValue => _slot.Value;

        InflatableCharacter character;
        Rigidbody2D rb;
        PlayerInput input;
        InputAction moveAction, jumpAction, triggerAction;
        readonly PredictionRigidbody2D _prb = new PredictionRigidbody2D();
        bool jumpEdge;   // owner-only: latch a jump press between ticks
        SpriteRenderer sprite;
        static readonly Color HostColor = new Color(0.40f, 0.60f, 1.00f);   // slot 0 = host  = blue
        static readonly Color ClientColor = new Color(1.00f, 0.50f, 0.40f); // slot 1 = client = red

        public InflatableCharacter Character => character;

        void Awake()
        {
            character = GetComponent<InflatableCharacter>();
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInput>();
            sprite = GetComponentInChildren<SpriteRenderer>(true); // body sprite (on the Graphical Object child)
            // The controller is the single canonical input path; silence InflatableCharacter's local
            // SendMessages callbacks and its FixedUpdate movement (movement now runs in the predicted Tick).
            character.IgnoreLocalInputCallbacks = true;
            character.SimulatePhysics = false;
            _prb.Initialize(rb);
        }

        /// <summary>Server-only: set which budget slot (0 or 1) this player occupies.</summary>
        public void SetSlotServer(int slot) => _slot.Value = slot;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // Rigidbody prediction needs both the tick (replicate) and post-tick (reconcile) callbacks.
            SetTickCallbacks(TickCallback.Tick | TickCallback.PostTick);
            // Tint by role so the two idiots are visually distinct online; slot is a SyncVar, so every peer
            // agrees who's blue (host) and who's red (client). Re-apply on change in case the slot syncs late.
            _slot.OnChange += OnSlotChanged;
            ApplyRoleColor();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _slot.OnChange -= OnSlotChanged;
        }

        // slot 0 = host = blue, slot 1 = client = red. Local play colors players in the scene builder instead;
        // this only runs on networked player objects.
        void OnSlotChanged(int prev, int next, bool asServer) => ApplyRoleColor();

        void ApplyRoleColor()
        {
            if (sprite != null) sprite.color = SlotValue == 0 ? HostColor : ClientColor;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Only the local owner reads a device; everyone else's PlayerInput stays off.
            input.enabled = base.IsOwner;

            // Pure spectators (not the owner, not the host) no longer predict this player — its transform
            // is driven by the NetworkTransform (state forwarding is off). Take the body out of local
            // physics so gravity and collisions can't fight the synced position (was causing idle drift +
            // sinking through the floor, worst while deflated where gravityScale is high). Left simulated so
            // Rigidbody2D.position still tracks the transform for the hose ends; kinematic + zeroed velocity
            // means it never falls or self-moves. The owner keeps a dynamic body for prediction; the host
            // keeps every body dynamic for authoritative simulation.
            if (!base.IsOwner && !base.IsServerInitialized)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            if (!base.IsOwner) return;

            moveAction = input.actions.FindAction("Move");
            jumpAction = input.actions.FindAction("Jump");
            triggerAction = input.actions.FindAction("AirTransfer");
        }

        void Update()
        {
            if (!base.IsOwner) return;
            // Latch the jump press at frame rate so a tap between ticks isn't missed.
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
                jumpEdge = true;
        }

        protected override void TimeManager_OnTick()
        {
            PerformReplicate(BuildMoveData());
        }

        protected override void TimeManager_OnPostTick()
        {
            CreateReconcile();
        }

        /// <summary>Owner builds the input for this tick; everyone else replays/extrapolates from reconcile.</summary>
        ReplicateData BuildMoveData()
        {
            if (!base.IsOwner)
                return default;

            Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            float trigger = triggerAction != null ? triggerAction.ReadValue<float>() : 0f;
            bool jump = jumpEdge;
            jumpEdge = false;
            return new ReplicateData(move, jump, trigger);
        }

        public override void CreateReconcile()
        {
            // Both server and client build this; the client uses its copy as a fallback for dropped packets.
            ReconcileData rd = new ReconcileData(_prb);
            PerformReconcile(rd);
        }

        [Replicate]
        void PerformReplicate(ReplicateData rd, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            // Lock controls while the run hasn't started (waiting for both players) or after it ends
            // (caught/solved). Zeroing here — identically on the predicting owner and the authoritative server —
            // keeps them in agreement so there's no reconcile fight; gravity still settles the body, only
            // walk/jump/air-transfer input is cut. (Local play freezes via timeScale instead; see RunController.)
            bool locked = RunController.Instance != null && RunController.Instance.ControlsLocked;
            if (locked) rd = new ReplicateData(Vector2.zero, false, 0f);

            // Trigger feeds the server-authoritative zero-sum air budget (read in NetworkAirBudget).
            character.SetTrigger(rd.Trigger);
            // Ground check + walk + jump + inflation state, on the rigidbody. Re-runs verbatim during replays.
            character.Tick(rd.Move, rd.Jump);
            // Environmental forces (fan updraft) must be applied INSIDE the predicted tick, through the
            // prediction rigidbody, so reconcile replays them — a raw AddForce in FanZone.FixedUpdate would
            // desync the owner from the host. Suppressed while locked, matching local play's timeScale freeze.
            if (!locked) ApplyEnvironmentForces();
            // Flush queued forces (fan updraft + any future ones); FishNet then steps Physics2D.
            _prb.Simulate();
        }

        // Queue the environmental forces acting on this body this tick (currently fan updrafts) onto the
        // prediction rigidbody. Position/velocity are read post-movement, pre-step — the body's state entering
        // the physics step — and the force is a pure function of them, so each reconcile replay reproduces it.
        void ApplyEnvironmentForces()
        {
            Vector2 pos = rb.position;
            Vector2 vel = rb.linearVelocity;
            var fans = FanZone.All;
            for (int i = 0; i < fans.Count; i++)
            {
                Vector2 f = fans[i].SampleForce(pos, vel);
                if (f != Vector2.zero) _prb.AddForce(f);
            }
        }

        [Reconcile]
        void PerformReconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
        {
            // Snap the rigidbody to the authoritative state; FishNet then replays buffered inputs to catch up.
            _prb.Reconcile(rd.Body);
        }
    }
}
