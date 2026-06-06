using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using InflatableIdiots.Level;

namespace InflatableIdiots.Net
{
    /// <summary>
    /// Networked, host-authoritative run coordination for the spike — the piece <see cref="RunController"/>
    /// can't do alone because run state + restart must agree across machines. Add this to the NetworkLink
    /// prefab (alongside <see cref="NetworkHose"/> + <see cref="NetworkAirBudget"/>): the link spawns exactly
    /// when both players have joined, so it exists for the whole live run.
    ///
    /// • Win/lose state is a SyncVar the host sets and every peer mirrors (so banners agree) — see
    ///   <see cref="RunController.ReportWin"/>/<see cref="RunController.ReportLoss"/>, which defer to the host
    ///   when networked.
    /// • Restart works for EITHER player: a client routes through a ServerRpc, the host runs it directly. The
    ///   server teleports both players to spawn (<see cref="NetPlayerSpawner"/>), resets the air budget
    ///   (<see cref="NetworkAirBudget"/>), clears the state, and bumps a restart epoch. The epoch change fires
    ///   <see cref="Restarted"/> on every peer so each resets its own local rising hazard — all WITHOUT
    ///   reloading the scene, which would tear down the Steam/FishNet session and the host's join GUI.
    /// </summary>
    public class NetworkRunController : NetworkBehaviour, IRunNetwork
    {
        // Authoritative run state, mirrored to every peer (banners read it). Sync every tick so the end
        // banner / restart clear feels immediate rather than lagging the default 10 Hz.
        readonly SyncVar<RunState> _state = new SyncVar<RunState>(RunState.Playing, new SyncTypeSettings(0f));
        // Bumped on each restart; the change is the signal for every peer to reset its local elements.
        readonly SyncVar<uint> _restartEpoch = new SyncVar<uint>(0, new SyncTypeSettings(0f));

        // Server-only refs, handed in by NetPlayerSpawner before Spawn.
        NetPlayerSpawner spawner;
        NetworkAirBudget airBudget;

        public bool IsServer => base.IsServerInitialized;
        public RunState State => _state.Value;
        public event System.Action Restarted;

        /// <summary>Server-only: wire the references needed to perform a reset.</summary>
        public void ConfigureServer(NetPlayerSpawner playerSpawner, NetworkAirBudget budget)
        {
            spawner = playerSpawner;
            airBudget = budget;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _restartEpoch.OnChange += OnRestartEpochChanged;
            // Hand ourselves to the scene's RunController so it mirrors state + routes restart through us.
            if (RunController.Instance != null) RunController.Instance.AttachNetwork(this);
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            _restartEpoch.OnChange -= OnRestartEpochChanged;
            if (RunController.Instance != null) RunController.Instance.DetachNetwork(this);
        }

        // Fire Restarted once per peer. On the host the change raises both the asServer and the clientHost
        // callback; gate on !asServer so it fires exactly once everywhere (the host always has a clientHost).
        void OnRestartEpochChanged(uint prev, uint next, bool asServer)
        {
            if (asServer) return;
            Restarted?.Invoke();
        }

        // IRunNetwork — host publishes the authoritative end state.
        public void ServerSetState(RunState state)
        {
            if (!base.IsServerInitialized) return;
            _state.Value = state;
        }

        // IRunNetwork — either player asks to restart.
        public void RequestRestart()
        {
            if (base.IsServerInitialized) ServerRestart();
            else ServerRequestRestart();
        }

        [ServerRpc(RequireOwnership = false)]
        void ServerRequestRestart() => ServerRestart();

        void ServerRestart()
        {
            if (!base.IsServerInitialized) return;

            if (spawner != null) spawner.ServerResetPlayers();
            else Debug.LogWarning("[Spike] NetworkRunController has no spawner ref — players won't be repositioned on restart.");
            if (airBudget != null) airBudget.ResetServer();

            _state.Value = RunState.Playing;
            _restartEpoch.Value++; // → OnRestartEpochChanged on every peer → each resets its local hazard
        }
    }
}
