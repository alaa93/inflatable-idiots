---
status: implemented
tags: [networking, fishnet]
---

# FishNet Netcode

The chosen netcode framework (4.7.2). Runs simple host-authority **now** and supports client-side prediction **later** without swapping frameworks — the hedge behind [[Decision - FishNet host-authoritative]].

## How we use it (spike)
- **Host-authoritative, NO prediction** — deliberately, to *measure* the unpredicted feel
- Transport: **FishySteamworks** (4.1.1) over SDR — see [[Steam Integration]]
- `NetworkTransform` (config Rigidbody2D) replicates bodies; **Client Authoritative = OFF** (server authority) is the critical setting
- `readonly SyncVar<T>` for state, `[ServerRpc]` for input, `[ObserversRpc]` for rope points

## Components built on it
- [[Network Player Controller]] (input relay + ownership)
- [[Network Air Budget]] (server-authoritative SyncVar)
- [[Network Hose]] (host sim → client render)
- [[Network Spawner]] (per-connection spawn)

## The prediction decision (pending)
The [[Networking Spike]] two-PC test measures remote-body input latency. If it feels too mushy → enable FishNet's built-in client-side prediction. If fine → ship host-auth. See [[Roadmap]].

## Connections
[[Networking Architecture]] · [[Decision - FishNet host-authoritative]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Network Spawner]] · [[Steam Integration]]
