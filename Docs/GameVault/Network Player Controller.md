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
- **Visual smoothing** via the NetworkObject's Graphical Object (sprite on a child) — corrections ease instead of snapping. NetworkTransform removed (reconcile replaces it). See [[Setup and Gotchas]].

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetworkInflatableController.cs`

## Connections
[[Player Character]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Air Budget]] · [[Input System]]
