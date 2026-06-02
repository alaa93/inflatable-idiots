---
status: implemented
tags: [networking, fishnet]
---

# Network Spawner

`NetPlayerSpawner` — server-side spawner for the 2-player spike. Replaces FishNet's built-in `PlayerSpawner` (which can't do our slot + link logic).

## Behaviour
- Hooks `SceneManager.OnClientLoadedStartScenes(conn, asServer)` (guard `asServer`) — fires for every connection including the host's clientHost
- Per connection: instantiate `NetworkPlayer` prefab → `ServerManager.Spawn(go, conn)` (ownership) → **`AddOwnerToDefaultScene`** (required to be observed when not using FishNet global scenes) → assign budget slot
- Once both players exist: spawns the `NetworkLink` prefab carrying [[Network Hose]] + [[Network Air Budget]], wiring endpoints/controllers before spawn

## ⚠️ The bug that cost us
The first version used `ServerManager.OnRemoteConnectionState` and skipped `AddOwnerToDefaultScene` → nothing appeared. Fixed by mirroring FishNet's own PlayerSpawner. **Remove FishNet's built-in `PlayerSpawner` from the NetworkManager.**

## Prefabs (require NetworkObject — unlike the code-built local players)
- `NetworkPlayer` (NetworkObject + NetworkTransform + [[Network Player Controller]] + [[Player Character]])
- `NetworkLink` ([[Network Hose]] + [[Network Air Budget]])

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetPlayerSpawner.cs`

## Connections
[[Network Player Controller]] · [[Network Hose]] · [[Network Air Budget]] · [[FishNet Netcode]] · [[Networking Spike]] · [[Test Scene Builder]]
