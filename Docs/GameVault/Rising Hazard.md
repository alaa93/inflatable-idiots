---
status: implemented
tags: [level, hazard]
---

# Rising Hazard

A wide "lava" bar that climbs at a constant speed (0.1 u/s). Touching it with **either** player reports a loss to [[Run Controller]]. Provides the time pressure in the [[Fan Trap]].

- Kinematic `Rigidbody2D` + trigger; moves via `MovePosition` in FixedUpdate; stops once the run is over
- Only [[Player Character]] contact ends the run (ignores hose/geometry)
- `Assets/InflatableIdiots/Scripts/Hazards/RisingHazard.cs`

## Host-authoritative height (networked, 2026-06-06)
Before, every machine self-rose the bar, each gated on its own `RunStarted`/`IsOver` — independent start timing + `IsOver` sync latency drifted it apart, so the visual "caught" moment didn't match the host's authoritative loss. Now there's **one source of truth**:
- **Host/local** self-rise as before, then `RunController.PublishHazardHeight` replicates the commanded height (a per-tick `SyncVar<float>` on [[Run Controller]]'s `NetworkRunController`).
- **Clients** don't self-rise; they mirror that synced height (`TryGetNetworkHazardHeight`). `NaN` = host hasn't published yet → pin to the authored start. On restart the SyncVar resets to `NaN` first, so no stale-high value flashes before the re-climb.

The only authority that decides "caught" (the host) is the one whose bar every player sees.

## Connections
[[Fan Trap]] · [[Run Controller]] · [[Player Character]] · [[Goal Zone]]
