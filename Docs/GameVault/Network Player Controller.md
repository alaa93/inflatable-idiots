---
status: implemented
tags: [networking, fishnet]
---

# Network Player Controller

`NetworkInflatableController` — the thin netcode driver on each networked [[Player Character]]. Host-authoritative, no prediction.

## Behaviour
- **Owner** captures local input and ships it to the server each tick via `[ServerRpc] SubmitInput(move, jump, trigger)` (jump latched at frame rate so a tap isn't missed)
- **Server** applies input to [[Player Character]] via the additive `Set*` setters and runs the authoritative physics (`SimulatePhysics = true`)
- **Remote client** body is kinematic and follows `NetworkTransform` (`SimulatePhysics = false`)
- `PlayerInput` enabled only on the owner (avoids dual-keyboard device grabbing — see [[Input System]])
- `SyncVar<int> Slot` (0/1) assigned by [[Network Spawner]], read by [[Network Air Budget]]

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetworkInflatableController.cs`

## Connections
[[Player Character]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Air Budget]] · [[Input System]]
