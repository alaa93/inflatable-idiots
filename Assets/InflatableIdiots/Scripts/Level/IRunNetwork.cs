namespace InflatableIdiots.Level
{
    /// <summary>
    /// Seam that lets <see cref="RunController"/> stay network-agnostic while the networked scene drives it.
    /// The networked implementation (NetworkRunController, in the Net assembly) attaches itself to the scene's
    /// RunController when the link spawns; RunController then mirrors the authoritative <see cref="State"/>,
    /// routes restart requests here instead of reloading the scene, and resets its local elements when the
    /// server-broadcast <see cref="Restarted"/> arrives. In local (non-networked) play nothing attaches and
    /// RunController behaves exactly as before.
    /// </summary>
    public interface IRunNetwork
    {
        /// <summary>True only on the host/server — the authority for win/lose state.</summary>
        bool IsServer { get; }

        /// <summary>Authoritative run state, synced from the host to every peer.</summary>
        RunState State { get; }

        /// <summary>Host-only: publish the authoritative end state (ignored on clients).</summary>
        void ServerSetState(RunState state);

        /// <summary>Either player: ask the host to reset the run in place (no scene reload).</summary>
        void RequestRestart();

        /// <summary>Fires on every peer when a restart happens, so each resets its local elements (hazard).</summary>
        event System.Action Restarted;
    }
}
