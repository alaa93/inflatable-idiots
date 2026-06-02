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
- Tooling: [[Test Scene Builder]], [[Windows Build]]

## 🔜 Now — [[Networking Spike]] (online co-op proof-of-concept)
Prove the riskiest 20% before building full netcode:
1. ✅ Steam init on App ID 480, [[Steam Integration]] working
2. ✅ Solo host path: player spawns + moves in editor ([[Network Spawner]], [[Network Player Controller]])
3. 🔲 **Two-PC test** (next session): build for PC B, host+invite+join, validate two-body sync, [[Network Hose]], and [[Network Air Budget]] under real latency
4. 🔲 Decide if client-side prediction is needed (the spike measures the unpredicted feel — see [[FishNet Netcode]])
5. 🔲 Codify the manual scene/prefab setup into a `NetworkSpikeSceneBuilder` (like [[Test Scene Builder]])

## 🔭 After the spike validates
- Port the [[Fan Trap]] into the networked flow
- [[Voice Chat - Dissonance]] (the "GIVE ME 40%" moment over the wire)
- Real UI (replace the OnGUI placeholders in [[Run Controller]])
- Revisit [[Decision - 2P now 4P later]] and [[Parked Ideas]] only after online co-op is proven
- Before release: pin versions / verify stable Steam client — see [[Tech Stack and Versions]]

## Connections
[[Inflatable Idiots]] · [[Networking Architecture]] · [[Parked Ideas]]
