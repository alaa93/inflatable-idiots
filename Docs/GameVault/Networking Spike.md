---
status: in-progress
tags: [networking, milestone]
---

# Networking Spike

A thin vertical proof-of-concept to validate the riskiest 20% of online play ([[Networking Architecture]]) **before** building full netcode or porting the [[Fan Trap]]. Approved plan lives at `.claude/plans/write-up-the-spike-temporal-sonnet.md`.

## What it must prove
1. ✅ Steam init on 480, private lobby, overlay invite/accept ([[Steam Integration]])
2. 🔲 FishNet connect host+client over SDR across two PCs ([[FishNet Netcode]])
3. ◑ Two bodies synced host-authoritatively ([[Network Player Controller]], [[Network Spawner]]) — **solo host verified**, 2-PC pending
4. 🔲 Hose simulates on host, renders on client ([[Network Hose]])
5. 🔲 Zero-sum air-share over the wire ([[Network Air Budget]])

## Status (2026-06-02)
- All runtime code written; merged into `main` (was branch `networking-spike`, PR #1)
- **Solo host works**: Play → Host spawns a controllable player ([[Network Spawner]] fix below)
- Scene/prefabs (`NetworkSpikeTest`, `NetworkPlayer`, `NetworkLink`) wired **manually**

## Key fix this session
First [[Network Spawner]] used the wrong hook and nothing appeared. Fixed by mirroring FishNet's PlayerSpawner: `SceneManager.OnClientLoadedStartScenes` + `AddOwnerToDefaultScene`. Remove FishNet's built-in `PlayerSpawner` from the NetworkManager.

## Update (2026-06-02, later) — spawn+movement step, hose deferred
- **FishNet re-confirmed** vs a serious Photon Fusion proposal — see [[Decision - FishNet host-authoritative]] / [[Networking Solutions - Comparison]].
- **Steam standalone fix:** `BuildWindows.cs` now auto-copies `steam_appid.txt` next to the `.exe` (was the "Steam works in editor, not in build" bug). Confirmed working.
- **Client path verified already-implemented** — no new gameplay code. Per-connection spawn + owner-input→ServerRpc→server-physics→NetworkTransform is complete.
- Added `spawnHoseLink` toggle (default **OFF**) to [[Network Spawner]] → goals 4 & 5 (hose, air-share) deliberately deferred so this step isolates **two-body spawn + movement**.

## Next session
Build for PC B → two-PC test (both spawn + move, hose off) → if good, flip `spawnHoseLink` on and tackle [[Network Hose]] + [[Network Air Budget]] → then measure unpredicted feel (informs prediction decision in [[FishNet Netcode]]) → codify setup into a builder. Watch: remote client's `OnClientLoadedStartScenes` must fire server-side (no global scenes) for the 2nd player to spawn. See [[Roadmap]] and [[Setup and Gotchas]].

## Connections
[[Networking Architecture]] · [[Steam Integration]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Windows Build]] · [[Setup and Gotchas]]
