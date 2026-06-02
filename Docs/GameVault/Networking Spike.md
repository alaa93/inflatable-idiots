---
status: in-progress
tags: [networking, milestone]
---

# Networking Spike

A thin vertical proof-of-concept to validate the riskiest 20% of online play ([[Networking Architecture]]) **before** building full netcode or porting the [[Fan Trap]]. Approved plan: `Docs`/`plans/write-up-the-spike-temporal-sonnet.md`.

## What it must prove
1. ✅ Steam init on 480, private lobby, overlay invite/accept ([[Steam Integration]])
2. 🔲 FishNet connect host+client over SDR across two PCs ([[FishNet Netcode]])
3. ◑ Two bodies synced host-authoritatively ([[Network Player Controller]], [[Network Spawner]]) — **solo host verified**, 2-PC pending
4. 🔲 Hose simulates on host, renders on client ([[Network Hose]])
5. 🔲 Zero-sum air-share over the wire ([[Network Air Budget]])

## Status (2026-06-02)
- All runtime code written; committed on branch `networking-spike` (pushed to origin)
- **Solo host works**: Play → Host spawns a controllable player ([[Network Spawner]] fix below)
- Scene/prefabs (`NetworkSpikeTest`, `NetworkPlayer`, `NetworkLink`) wired **manually**

## Key fix this session
First [[Network Spawner]] used the wrong hook and nothing appeared. Fixed by mirroring FishNet's PlayerSpawner: `SceneManager.OnClientLoadedStartScenes` + `AddOwnerToDefaultScene`. Remove FishNet's built-in `PlayerSpawner` from the NetworkManager.

## Next session
Build for PC B → two-PC test → measure unpredicted feel (informs the prediction decision in [[FishNet Netcode]]) → then codify setup into a builder. See [[Roadmap]] and [[Setup and Gotchas]].

## Connections
[[Networking Architecture]] · [[Steam Integration]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Windows Build]] · [[Setup and Gotchas]]
