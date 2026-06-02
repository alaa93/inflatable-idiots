using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;
using InflatableIdiots.Player;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Thin networking driver for one inflatable player. Host-authoritative, NO client-side prediction:
    /// the owner captures local input and ships it to the server each tick; the server applies it to the
    /// authoritative <see cref="InflatableCharacter"/> (which simulates physics only on the server). On
    /// remote clients the body is kinematic and driven by NetworkTransform.
    ///
    /// `Slot` (0 or 1) is assigned by <see cref="NetPlayerSpawner"/> on the server and read by
    /// <see cref="NetworkAirBudget"/> to map this player onto the zero-sum budget.
    /// </summary>
    [RequireComponent(typeof(InflatableCharacter))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class NetworkInflatableController : NetworkBehaviour
    {
        readonly SyncVar<int> _slot = new SyncVar<int>();
        public int SlotValue => _slot.Value;

        InflatableCharacter character;
        Rigidbody2D rb;
        PlayerInput input;
        InputAction moveAction, jumpAction, triggerAction;
        bool jumpEdge;

        public InflatableCharacter Character => character;

        void Awake()
        {
            character = GetComponent<InflatableCharacter>();
            rb = GetComponent<Rigidbody2D>();
            input = GetComponent<PlayerInput>();
            // Networked input flows through SubmitInput -> InflatableCharacter.Set*; silence the local
            // SendMessages callbacks so there's a single canonical input path.
            character.IgnoreLocalInputCallbacks = true;
        }

        /// <summary>Server-only: set which budget slot (0 or 1) this player occupies.</summary>
        public void SetSlotServer(int slot) => _slot.Value = slot;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            bool server = base.IsServerInitialized;
            // Only the server runs movement physics; remote clients are kinematic and follow NetworkTransform.
            character.SimulatePhysics = server;
            if (!server)
                rb.bodyType = RigidbodyType2D.Kinematic;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            // Only the local owner reads a device; everyone else's PlayerInput stays off (avoids device-grab
            // conflicts between the two PlayerInputs).
            input.enabled = base.IsOwner;
            if (!base.IsOwner) return;

            moveAction = input.actions.FindAction("Move");
            jumpAction = input.actions.FindAction("Jump");
            triggerAction = input.actions.FindAction("AirTransfer");
            base.TimeManager.OnTick += OnTick;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (base.IsOwner && base.TimeManager != null)
                base.TimeManager.OnTick -= OnTick;
        }

        void Update()
        {
            if (!base.IsOwner) return;
            // Latch the jump press at frame rate so a tap between ticks isn't missed.
            if (jumpAction != null && jumpAction.WasPressedThisFrame())
                jumpEdge = true;
        }

        void OnTick()
        {
            if (!base.IsOwner) return;
            Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
            float trigger = triggerAction != null ? triggerAction.ReadValue<float>() : 0f;
            bool jump = jumpEdge;
            jumpEdge = false;
            SubmitInput(move, jump, trigger);
        }

        [ServerRpc]
        void SubmitInput(Vector2 move, bool jump, float trigger)
        {
            character.SetMoveInput(move);
            character.SetTrigger(trigger);
            if (jump) character.QueueJump();
        }
    }
}
