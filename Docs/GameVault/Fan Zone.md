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

## Networked (2026-06-06)
In net play the fan force can't be a raw `AddForce` from FixedUpdate — for a **predicted** player body that force lives outside FishNet's `[Replicate]`, so reconcile never replays it and the owner diverges from the host every tick in the column (the host-only-hose stutter, again). Fix:
- FanZone **skips predicted player bodies** in its FixedUpdate (`InflatableCharacter.SimulatePhysics == false`) and registers itself in a static `FanZone.All`.
- [[Network Player Controller]] **samples the force inside its predicted tick** — `SampleForce(pos, vel)`, a pure function of position/velocity (center-point overlap, no trigger membership) — and queues it on the `PredictionRigidbody2D` (`AddForce` → `Simulate`), so replicate/reconcile reproduce it deterministically. Spectators inherit it for free via the host's NetworkTransform-synced position.
- Suppressed while [[Run Controller]]'s `ControlsLocked` (run not live), matching the local timeScale freeze.
- Locally-simulated bodies (single-PC play) keep the direct FixedUpdate push unchanged. `ForceMode2D.Force` is rate-based, so the 50 Hz FixedUpdate vs 60 Hz tick give the same net lift — no retuning.

✅ **2-PC validated (2026-06-06)** — the owner rides the updraft into the taut hose smoothly; no reconcile stutter.

## Where it lives
- `Assets/InflatableIdiots/Scripts/Hazards/FanZone.cs`

## Connections
[[Fan Trap]] · [[Inflation State]] · [[Air Hose]] · [[Player Character]] · [[Network Player Controller]] · [[FishNet Netcode]] · [[Run Controller]]
