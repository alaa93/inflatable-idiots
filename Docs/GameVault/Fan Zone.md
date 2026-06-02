---
status: implemented
tags: [level, hazard]
---

# Fan Zone

The updraft column at the heart of the [[Fan Trap]].

## Behaviour
- Trigger volume; while a **dynamic** `Rigidbody2D` is inside, applies a constant upward **force** (`force` 10 N default, `maxRiseSpeed` cap 4 m/s)
- Constant force = **mass-dependent acceleration by design**: a body lifts when `force > mass * gravityScale * 9.81`
- So an inflated player (0.1 × 0.15) gets flung up like a kite; a deflated one (3 × 2.5) barely feels it → see [[Inflation State]]
- Horizontal air control comes free from [[Player Character]]'s walk (sets velocity.x every FixedUpdate regardless of grounded)

## Where it lives
- `Assets/InflatableIdiots/Scripts/Hazards/FanZone.cs`

## Connections
[[Fan Trap]] · [[Inflation State]] · [[Air Hose]] · [[Player Character]]
