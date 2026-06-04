---
status: implemented
tags: [networking, fishnet]
---

# Network Hose

`NetworkHose` — networks the [[Air Hose]] host-authoritatively. The single hardest part of networking this game, because the hose physically couples both bodies every tick.

## Approach
- **Server** runs the real spring-rope (`HoseController.BuildAsHost`) and broadcasts node positions ~25 Hz via `[ObserversRpc] RenderRope(Vector2[])`
- **Clients** never simulate the rope — they store the snapshot and **ease the LineRenderer toward it each frame** (`HoseController.SmoothClientRope`, framerate-independent), so it's smooth at the 25 Hz rate instead of choppy (added 2026-06-03)
- **Rope ends anchor to the live local players** on the client (`NetworkHose.ResolveClientEndpoints` finds them by slot) so the hose stays attached to the smoothly-moving characters instead of trailing the delayed snapshot. Tunable `clientSmoothing` (default 20) on HoseController.
- **Spring softened (2026-06-04):** rope spring frequency 4→2.5 Hz, damping 0.7→0.9 — a gentler, less-bouncy tug shrinks the per-tick pull-back, easing the prediction-divergence stutter when a player moves into the taut hose (see [[Network Player Controller]]).
- Endpoints (the two player rigidbodies) are injected by [[Network Spawner]] *before* spawn; `Managed` is set in Awake so `HoseController.Start` no-ops and the FishNet lifecycle drives it
- `ExcludeServer = true` on the RPC: the host draws its own simulated rope

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetworkHose.cs`

## Connections
[[Air Hose]] · [[Network Spawner]] · [[Network Air Budget]] · [[FishNet Netcode]] · [[Networking Architecture]]
