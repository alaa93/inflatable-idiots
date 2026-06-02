---
status: implemented
tags: [system]
---

# Input System

Unity's New Input System with `PlayerInput` (SendMessages).

## Actions (map: "Player")
- `Move` (Vector2)
- `Jump` (Button)
- `AirTransfer` (Value/Axis 0..1) — the trigger that drives the [[Air-Share Mechanic]]

## Control schemes
- **Gamepad**: leftStick / buttonSouth / rightTrigger
- **Keyboard P1**: WASD / LeftCtrl / LeftShift
- **Keyboard P2**: Arrows / RightCtrl / RightShift

## Keyboard fallback (rule)
Always wire keyboard + mouse bindings alongside gamepad so solo editor testing doesn't require gamepads. Locally, [[AirPair]] force-pairs one keyboard to both players.

## Online difference
Per machine there is **one** local player. [[Network Player Controller]] enables `PlayerInput` only on the owner, reads actions directly, and ships input to the server via ServerRpc — sidestepping the dual-keyboard problem.

## Where it lives
- `Assets/InflatableIdiots/Input/InflatableIdiots.inputactions`

## Connections
[[Player Character]] · [[AirPair]] · [[Air-Share Mechanic]] · [[Network Player Controller]]
