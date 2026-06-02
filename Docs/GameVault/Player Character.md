---
status: implemented
tags: [mechanic, core]
---

# Player Character

One inflatable. Handles input, movement, jump, ground check, and applies [[Inflation State]] visuals.

## Behaviour
- **Walk**: sets `rb.linearVelocity.x = moveInput.x * walkSpeed` (constant 6)
- **Jump**: direct vertical-velocity assignment (`v.y = lerp(jumpSpeedDeflated, jumpSpeedInflated, k)`), NOT impulse — avoids a divide-by-mass that launched the inflated body at absurd speed. Floaty arc comes from low `gravityScale`, not big jump force.
- **Ground check**: `Physics2D.CircleCast` down, scaled by the body's current scale
- **Visuals**: scale + [[Stalemate Feedback]] jitter in `LateUpdate`

## Where it lives
- `Assets/InflatableIdiots/Scripts/Player/InflatableCharacter.cs`
- Paired by [[AirPair]] locally; driven by [[Network Player Controller]] online

## Networking seams (additive — defaults preserve local play)
Added this session, used only by the netcode: `SimulatePhysics` (server-only physics gate), `BudgetSource` (override [[AirPair]] with [[Network Air Budget]]), `IgnoreLocalInputCallbacks`, and `SetMoveInput/QueueJump/SetTrigger` server setters.

## Connections
[[Air-Share Mechanic]] · [[Inflation State]] · [[AirPair]] · [[Input System]] · [[Stalemate Feedback]] · [[Network Player Controller]]
