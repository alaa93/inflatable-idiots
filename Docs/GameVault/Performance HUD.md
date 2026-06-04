---
status: implemented
tags: [tooling, networking]
---

# Performance HUD

`FpsCounter` — an IMGUI on-screen overlay for performance testing. Added 2026-06-04 during the netcode smoothness work ([[Networking Spike]]).

## Shows
- **Render FPS + frame-ms**, color-coded (green ≥55 / yellow ≥30 / red below)
- When a [[FishNet Netcode]] NetworkManager is live: **role** (Host / Server / Client / Offline), **tick rate**, and the client's **RTT** (ms)

## Notes
- Uses `Time.unscaledDeltaTime` so it stays accurate when the game is paused/slowed
- Network stats read from `InstanceFinder.TimeManager` (`TickRate`, `RoundTripTime`); null-guarded, so it's just the FPS line when offline
- The tick rate doubles as a **physics-fidelity** readout — under `PhysicsMode.TimeManager` the network tick rate *is* the physics step rate (see [[Setup and Gotchas]])
- Drop the component on any GameObject; `DontDestroyOnLoad` by default. One lives in `NetworkSpikeTest` (top-right corner)

## Where it lives
- `Assets/InflatableIdiots/Scripts/View/FpsCounter.cs`

## Connections
[[Networking Spike]] · [[FishNet Netcode]] · [[Setup and Gotchas]] · [[Roadmap]]
