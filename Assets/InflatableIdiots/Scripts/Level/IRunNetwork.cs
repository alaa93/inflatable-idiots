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

        /// <summary>
        /// Host-authoritative rising-hazard height, synced to every peer so the hazard sits at the same place
        /// on both screens (and the host's loss decision matches what each player sees). NaN until the host
        /// first publishes — clients hold the hazard at its authored start until then.
        /// </summary>
        float HazardHeight { get; }

        /// <summary>Host-only: publish the authoritative end state (ignored on clients).</summary>
        void ServerSetState(RunState state);

        /// <summary>Host-only: replicate the rising hazard's current height to clients (ignored on clients).</summary>
        void ServerPublishHazardHeight(float y);

        /// <summary>
        /// Host-authoritative bitmask of co-op puzzle flags (switch held, plate pressed, gate open, lift active…),
        /// synced to every peer so gates, visuals, and gated fans agree across machines. See
        /// <see cref="RunController.GetPuzzleFlag"/>.
        /// </summary>
        ulong PuzzleFlags { get; }

        /// <summary>Host-only: publish the authoritative puzzle-flag bitmask to clients (ignored on clients).</summary>
        void ServerSetPuzzleFlags(ulong flags);

        /// <summary>Either player: ask the host to reset the run in place (no scene reload).</summary>
        void RequestRestart();

        /// <summary>Fires on every peer when a restart happens, so each resets its local elements (hazard).</summary>
        event System.Action Restarted;
    }
}
