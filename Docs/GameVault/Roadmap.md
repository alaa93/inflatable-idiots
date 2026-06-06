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
8. ✅ **Fan Trap run-coordination → netcode (`6188373` + `5185107`):** hazard **start-gate** (waits for both players) + **host-authoritative in-place restart** (either player) via `NetworkRunController` on the link; **hazard height now host-authoritative** (clients mirror a per-tick SyncVar — no per-machine drift) + **controls lock off-run** (`ControlsLocked` zeroes the replicate input before start / after end). See [[Run Controller]], [[Rising Hazard]], [[Network Player Controller]]
9. ✅ **[[Fan Zone]] force over netcode (2-PC validated 2026-06-06):** the updraft is applied inside the player's predicted tick through the `PredictionRigidbody2D` (reconcile-safe); FanZone no longer raw-pushes predicted bodies from FixedUpdate; spectators follow the host via NetworkTransform. See [[Fan Zone]] / [[Network Player Controller]]

## 🔭 After the spike validates
- ✅ **[[Fan Trap]] netcode port complete — 2-PC validated (2026-06-06):** hazard sync, off-run control lock (item 8) + [[Fan Zone]] force (item 9). The full loop now runs online. **The spike is validated end-to-end.**
- [[Voice Chat - Dissonance]] (the "GIVE ME 40%" moment over the wire)
- Real UI (replace the OnGUI placeholders in [[Run Controller]])
- Revisit [[Decision - 2P now 4P later]] and [[Parked Ideas]] only after online co-op is proven
- Before release: pin versions / verify stable Steam client — see [[Tech Stack and Versions]]

## Connections
[[Inflatable Idiots]] · [[Networking Architecture]] · [[Parked Ideas]]
