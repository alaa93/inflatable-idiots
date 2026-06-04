---
status: implemented
tags: [mechanic, core]
---

# Player Character

One inflatable. Handles input, movement, jump, ground check, and applies [[Inflation State]] visuals.

## Behaviour
- **Walk**: sets `rb.linearVelocity.x = moveInput.x * walkSpeed` (constant 6)
- **Jump**: direct vertical-velocity assignment (`v.y = lerp(jumpSpeedDeflated, jumpSpeedInflated, k)`), NOT impulse — avoids a divide-by-mass that launched the inflated body at absurd speed. Floaty arc comes from low `gravityScale`, not big jump force.
- **Collider**: `BoxCollider2D` (size 0.9 + 0.05 edge radius ≈ a 1-unit footprint with rounded corners) matching the square sprite — replaced a 1×1 `CapsuleCollider2D`, which was effectively a *circle* touching the floor at a single point. The edge radius lets it slide over ledges instead of snagging.
- **Ground check**: `Physics2D.CircleCast` down, scaled by the body's current scale
- **Visuals**: scale + [[Stalemate Feedback]] jitter in `LateUpdate`

## Where it lives
- `Assets/InflatableIdiots/Scripts/Player/InflatableCharacter.cs`
- Paired by [[AirPair]] locally; driven by [[Network Player Controller]] online

## Networking seams (additive — defaults preserve local play)
Added for the netcode: `SimulatePhysics` (server-only physics gate), `BudgetSource` (override [[AirPair]] with [[Network Air Budget]]), `IgnoreLocalInputCallbacks`, and `SetMoveInput/QueueJump/SetTrigger` server setters.

**Online visual note (2026-06-04):** the inflate/deflate **scale is excluded from FishNet's graphical smoother** (it smooths Position+Rotation only). Scale comes from the synced budget every frame on every machine, so smoothing it made the owner's predicted sprite mis-scale and **dip into the floor** — the spectated player (no smoother) was always fine. See [[Setup and Gotchas]] / [[Network Player Controller]].

## Connections
[[Air-Share Mechanic]] · [[Inflation State]] · [[AirPair]] · [[Input System]] · [[Stalemate Feedback]] · [[Network Player Controller]]
