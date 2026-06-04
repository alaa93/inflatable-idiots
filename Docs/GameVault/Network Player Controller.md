---
status: implemented
tags: [networking, fishnet]
---

# Network Player Controller

`NetworkInflatableController` — the netcode driver on each networked [[Player Character]]. **Client-side prediction (FishNet Prediction V2)**, rewritten 2026-06-03 from the old ServerRpc-input model (which felt laggy on the owner — the 2-PC test confirmed it).

## Behaviour
- `TickNetworkBehaviour` with `[Replicate]`/`[Reconcile]` + a `PredictionRigidbody2D` wrapping the body
- **Owner** predicts movement locally each tick for instant response; **server** runs the same replicate authoritatively; **reconcile** snapshots rewind + replay the client
- Movement logic lives in `[[Player Character]].Tick()` so local play and networked prediction share ONE path; `SimulatePhysics = false` on networked players (movement runs in the tick, not FixedUpdate)
- `PlayerInput` enabled only on the owner (avoids dual-keyboard device grabbing — see [[Input System]])
- `SyncVar<int> Slot` (0/1) assigned by [[Network Spawner]], read by [[Network Air Budget]]
- **Visual smoothing** via the NetworkObject's Graphical Object (sprite on a child) — corrections ease instead of snapping. Smooths **Position + Rotation only**; Scale is budget-driven by [[Inflation State]] (not reconcile), and smoothing it dipped the owner's sprite into the floor.
- **Spectated players are NOT predicted** (refined 2026-06-04). The NetworkObject runs with **state forwarding OFF** + a server-auth NetworkTransform, so spectators follow the host's real motion instead of replaying it without the host-only [[Network Hose]] force (the cause of the deflated-body stutter). This controller sets spectator bodies **kinematic** in `OnStartClient` — when `!IsOwner && !IsServerInitialized`: zero velocity + `bodyType = Kinematic` — so local physics can't fight the synced position. See [[Setup and Gotchas]].

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetworkInflatableController.cs`

## Connections
[[Player Character]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Air Budget]] · [[Input System]]
