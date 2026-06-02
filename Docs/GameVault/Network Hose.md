---
status: implemented
tags: [networking, fishnet]
---

# Network Hose

`NetworkHose` — networks the [[Air Hose]] host-authoritatively. The single hardest part of networking this game, because the hose physically couples both bodies every tick.

## Approach
- **Server** runs the real spring-rope (`HoseController.BuildAsHost`) and broadcasts node positions ~25 Hz via `[ObserversRpc] RenderRope(Vector2[])`
- **Clients** never simulate the rope — they call `HoseController.RenderFromPoints(...)` to draw it (uses the additive `RenderOnly` / `Managed` hooks)
- Endpoints (the two player rigidbodies) are injected by [[Network Spawner]] *before* spawn; `Managed` is set in Awake so `HoseController.Start` no-ops and the FishNet lifecycle drives it
- `ExcludeServer = true` on the RPC: the host draws its own simulated rope

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetworkHose.cs`

## Connections
[[Air Hose]] · [[Network Spawner]] · [[Network Air Budget]] · [[FishNet Netcode]] · [[Networking Architecture]]
