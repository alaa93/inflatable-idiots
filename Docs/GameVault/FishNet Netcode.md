---
status: implemented
tags: [networking, fishnet]
---

# FishNet Netcode

The chosen netcode framework (4.7.2). Host-authoritative with **client-side prediction (Prediction V2)** now enabled — the hedge behind [[Decision - FishNet host-authoritative]] paid off: the 2-PC test proved unpredicted play felt laggy, so prediction was turned on without swapping frameworks.

## How we use it
- **Client-side prediction** for the **owner's** movement (predict/reconcile); **spectated** players sync via NetworkTransform (state forwarding off), NOT predicted; server-authoritative for [[Network Air Budget]] + [[Network Hose]]
- Transport: **FishySteamworks** (4.1.1) over SDR — see [[Steam Integration]]
- **TimeManager Physics Mode = `TimeManager`** — REQUIRED for rigidbody prediction (see [[Setup and Gotchas]])
- `[Replicate]`/`[Reconcile]` + `PredictionRigidbody2D` for movement; `SyncVar<T>` for state; `[ObserversRpc]` for rope points
- NetworkObject: Enable Prediction + Rigidbody2D + a **Graphical Object** child (smoothing **Position+Rotation only** — scale is budget-driven, see [[Player Character]]). **Owner**: no NetworkTransform, reconcile drives it. **Spectators**: state forwarding OFF + a server-auth NetworkTransform + kinematic body (see [[Setup and Gotchas]])

## Components built on it
- [[Network Player Controller]] (input relay + ownership)
- [[Network Air Budget]] (server-authoritative SyncVar)
- [[Network Hose]] (host sim → client render)
- [[Network Spawner]] (per-connection spawn)

## Prediction (implemented 2026-06-03)
The 2-PC test answered the open question: unpredicted host-auth felt laggy on the client. Enabled FishNet Prediction V2 — see [[Network Player Controller]] for the predict/reconcile rewrite and [[Setup and Gotchas]] for the catches (PhysicsMode, graphical-object smoothing, the silent-no-op trap). A follow-up smoothness pass added graphical-object smoothing + hose interpolation + per-tick budget sync. **Refined 2026-06-04 (smoothness resolved):** spectators moved off prediction onto NetworkTransform (predicting them replayed movement without the host-only hose → stutter when deflated), spectator bodies set kinematic, and Scale excluded from the graphical smoother (it's budget-driven, was dipping the owner's sprite into the floor). See [[Network Player Controller]] / [[Networking Spike]] / [[Roadmap]].

## Connections
[[Networking Architecture]] · [[Decision - FishNet host-authoritative]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Network Spawner]] · [[Steam Integration]]
