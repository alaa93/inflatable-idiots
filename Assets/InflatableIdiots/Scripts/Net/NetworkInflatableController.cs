using FishNet.Managing.Timing;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;
using UnityEngine.InputSystem;
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

        public InflatableCharacter Character => character;

        void Awake()
        {
            character = GetComponent<InflatableCharacter>();
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInput>();
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
            // Trigger feeds the server-authoritative zero-sum air budget (read in NetworkAirBudget).
            character.SetTrigger(rd.Trigger);
            // Ground check + walk + jump + inflation state, on the rigidbody. Re-runs verbatim during replays.
            character.Tick(rd.Move, rd.Jump);
            // Flush any queued forces (none here — movement is velocity-based); FishNet then steps Physics2D.
            _prb.Simulate();
        }

        [Reconcile]
        void PerformReconcile(ReconcileData rd, Channel channel = Channel.Unreliable)
        {
            // Snap the rigidbody to the authoritative state; FishNet then replays buffered inputs to catch up.
            _prb.Reconcile(rd.Body);
        }
    }
}
