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
        readonly SyncVar<float> _p1Budget = new SyncVar<float>(0.5f);

        // Server-only refs to the two players, set before Spawn.
        NetworkInflatableController c0, c1;

        /// <summary>Server-only: provide the two player controllers (slot 0, slot 1).</summary>
        public void SetControllersServer(NetworkInflatableController slot0, NetworkInflatableController slot1)
        {
            c0 = slot0;
            c1 = slot1;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            // Both players have already spawned (server spawns this object after them), so wire every
            // controller's budget source. Read slot live so it stays correct regardless of sync timing.
            var controllers = FindObjectsByType<NetworkInflatableController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                var captured = c;
                captured.Character.BudgetSource = () => BudgetForSlot(captured.SlotValue);
            }
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
