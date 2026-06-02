---
status: implemented
tags: [level, hazard]
---

# Rising Hazard

A wide "lava" bar that climbs at a constant speed (0.1 u/s). Touching it with **either** player reports a loss to [[Run Controller]]. Provides the time pressure in the [[Fan Trap]].

- Kinematic `Rigidbody2D` + trigger; moves via `MovePosition` in FixedUpdate; stops once the run is over
- Only [[Player Character]] contact ends the run (ignores hose/geometry)
- `Assets/InflatableIdiots/Scripts/Hazards/RisingHazard.cs`

## Connections
[[Fan Trap]] · [[Run Controller]] · [[Player Character]] · [[Goal Zone]]
