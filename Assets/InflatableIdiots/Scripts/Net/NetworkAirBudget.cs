using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using InflatableIdiots.Player;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Server-authoritative replacement for AirPair in networked play. The server integrates the zero-sum
    /// air-transfer formula from both players' trigger values and publishes <see cref="_p1Budget"/> via a
    /// SyncVar. Every peer wires each <see cref="InflatableCharacter.BudgetSource"/> to read this, so both
    /// bodies inflate/deflate identically on both screens.
    ///
    /// Spawned by <see cref="NetPlayerSpawner"/> once both players exist; lives on the same object as
    /// <see cref="NetworkHose"/>. (We don't reuse AirPair: it isn't a NetworkBehaviour and its Start assumes
    /// a single shared keyboard.)
    /// </summary>
    public class NetworkAirBudget : NetworkBehaviour
    {
        [SerializeField] float transferSpeed = 1.5f;
        [SerializeField] float minBudget = 0.1f;
        [SerializeField] float maxBudget = 0.9f;

        // Mirrors AirPair.P1Budget: budget of slot 0; slot 1 gets (1 - this). Starts at an even split.
        // sendRate 0 = sync every tick. The default (0.1s / 10 Hz) makes the client's size visibly lag the
        // host's during active inflation/deflation, since SyncVars snap rather than interpolate.
        readonly SyncVar<float> _p1Budget = new SyncVar<float>(0.5f, new SyncTypeSettings(0f));

        // Server-only refs to the two players, set before Spawn.
        NetworkInflatableController c0, c1;

        /// <summary>Server-only: provide the two player controllers (slot 0, slot 1).</summary>
        public void SetControllersServer(NetworkInflatableController slot0, NetworkInflatableController slot1)
        {
            c0 = slot0;
            c1 = slot1;
        }

        bool wired;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            TryWireBudgetSources();
        }

        void Update()
        {
            if (!wired) TryWireBudgetSources();
        }

        // Wire each player's inflation source to the synced budget. Retried until BOTH slots are present,
        // because on a client a player may spawn / slot-sync a frame or two after this link object — a missed
        // wire would freeze that player's size on that machine (a cause of the size-desync bug). Slot is read
        // live in the closure, so it stays correct regardless of sync timing.
        void TryWireBudgetSources()
        {
            var controllers = FindObjectsByType<NetworkInflatableController>(FindObjectsSortMode.None);
            bool s0 = false, s1 = false;
            foreach (var c in controllers)
            {
                var captured = c;
                captured.Character.BudgetSource = () => BudgetForSlot(captured.SlotValue);
                if (captured.SlotValue == 0) s0 = true;
                else if (captured.SlotValue == 1) s1 = true;
            }
            wired = s0 && s1;
        }

        float BudgetForSlot(int slot) => slot == 0 ? _p1Budget.Value : 1f - _p1Budget.Value;

        void FixedUpdate()
        {
            if (!base.IsServerInitialized) return;
            if (c0 == null || c1 == null) return;

            // AirPair's formula, server-authoritative. Trigger values arrive via each controller's SubmitInput.
            float flow = (c0.Character.TriggerValue - c1.Character.TriggerValue) * transferSpeed * Time.fixedDeltaTime;
            _p1Budget.Value = Mathf.Clamp(_p1Budget.Value + flow, minBudget, maxBudget);
        }
    }
}
