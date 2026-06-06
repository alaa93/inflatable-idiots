---
status: planning
tags: [process, planning]
---

# Roadmap

Where the project is and what's next. "Now" = online networking spike.

## ✅ Done
- Core loop validated locally — see [[MVP Scope and Validation]]
- [[Air-Share Mechanic]], [[Inflation State]], [[Air Hose]], [[Player Character]], [[AirPair]], [[Stalemate Feedback]]
- [[Fan Trap]] gym level: [[Fan Zone]], [[Goal Zone]], [[Rising Hazard]], [[Run Controller]]
- Tooling: [[Test Scene Builder]], [[Windows Build]], [[Performance HUD]]

## 🔜 Now — [[Networking Spike]] (online co-op proof-of-concept)
Prove the riskiest 20% before building full netcode:
1. ✅ Steam init on App ID 480, [[Steam Integration]] working
2. ✅ Solo host path: player spawns + moves in editor ([[Network Spawner]], [[Network Player Controller]])
3. ✅ **Two-PC test**: two-body sync, [[Network Hose]], [[Network Air Budget]] over real latency (overlay-free Join-by-SteamID)
4. ✅ **Client-side prediction** (owner) + **NetworkTransform sync for spectators** — predicting spectators replayed them without the host-only hose, so the deflated-body stutter is fixed ([[Network Player Controller]], [[FishNet Netcode]])
5. ✅ **Smoothness RESOLVED (2026-06-04)** — smoothest 2-PC test yet; Photon Fusion migration **dropped**. Hose spring softened, air-size desync + own-player sprite floor-dip fixed ([[Networking Spike]])
6. ✅ **PlayerTest physics freeze fixed (2026-06-04)** — `LocalPhysicsDriver` drives `Physics2D.Simulate()` in non-networked scenes when FishNet's Script mode leaks; project default back to Fixed Update ([[Setup and Gotchas]])
7. ✅ **`NetworkSpikeSceneBuilder` (2026-06-04, `f9314c5`)** — one-click rebuild of the networked scene (full Fan Trap arena + NetworkManager/NetSpike wiring) from code, sibling of [[Test Scene Builder]]; assembles from the existing prefabs
8. 🔄 **Fan Trap → netcode (in progress, `6188373`):** run coordination done — hazard **start-gate** (waits for both players) + **host-authoritative in-place restart**, either player, via `NetworkRunController` on the link ([[Run Controller]]). Remaining: authoritative hazard *position* sync (each machine runs its own; re-syncs on restart) + lock controls on the end screen

## 🔭 After the spike validates
- Finish the [[Fan Trap]] netcode port (authoritative hazard sync + end-screen polish — started, item 8 above)
- [[Voice Chat - Dissonance]] (the "GIVE ME 40%" moment over the wire)
- Real UI (replace the OnGUI placeholders in [[Run Controller]])
- Revisit [[Decision - 2P now 4P later]] and [[Parked Ideas]] only after online co-op is proven
- Before release: pin versions / verify stable Steam client — see [[Tech Stack and Versions]]

## Connections
[[Inflatable Idiots]] · [[Networking Architecture]] · [[Parked Ideas]]
