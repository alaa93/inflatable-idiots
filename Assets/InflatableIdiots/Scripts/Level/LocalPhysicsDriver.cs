using FishNet.Managing;
using UnityEngine;

namespace InflatableIdiots.Level
{
    /// <summary>
    /// Keeps 2D physics ticking in non-networked scenes — the fix for the "PlayerTest physics freeze".
    ///
    /// FishNet's Rigidbody2D prediction needs <see cref="Physics2D"/> in <c>Script</c> simulation mode: its
    /// TimeManager steps physics once per network tick. When a networked scene starts, FishNet flips the
    /// global <see cref="Physics2D.simulationMode"/> to Script at runtime — and Unity does NOT restore it
    /// when you leave Play mode. The Script mode "leaks" into the rest of the editor session (until the next
    /// domain reload). The next time you press Play on a non-networked scene, nothing is calling
    /// <see cref="Physics2D.Simulate"/>, so every Rigidbody2D just hangs frozen in mid-air.
    ///
    /// Restoring the project default to FixedUpdate (see ProjectSettings) fixes a *fresh* editor and all
    /// builds, but not that mid-session leak — so this driver is the durable belt-and-suspenders. Drop it on
    /// a GameObject in any offline scene:
    ///   • Networked scene (a NetworkManager is present) → no-op; FishNet owns the Script-mode step.
    ///   • Physics already in FixedUpdate mode → no-op; Unity auto-simulates with native ordering.
    ///   • Physics left in Script mode with no NetworkManager (the leak) → we drive the step ourselves.
    ///
    /// A maxed execution order makes our step run after every other FixedUpdate, matching Unity's native
    /// "run all FixedUpdates, then simulate" ordering.
    /// </summary>
    [DefaultExecutionOrder(int.MaxValue)]
    public class LocalPhysicsDriver : MonoBehaviour
    {
        void FixedUpdate()
        {
            // A networked scene? FishNet's TimeManager owns the per-tick Script-mode step — never double-step.
            // Reading the static instance list avoids InstanceFinder's "NetworkManager not found" log spam.
            if (NetworkManager.Instances.Count > 0) return;

            // Only step when physics was left in Script mode (leaked from a prior networked run). In normal
            // FixedUpdate mode Unity already simulates, so stepping here would double up — stay out of it.
            if (Physics2D.simulationMode == SimulationMode2D.Script)
                Physics2D.Simulate(Time.fixedDeltaTime);
        }
    }
}
