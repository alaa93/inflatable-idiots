---
status: in-progress
tags: [networking, milestone]
---

# Networking Spike

A thin vertical proof-of-concept to validate the riskiest 20% of online play ([[Networking Architecture]]) **before** building full netcode or porting the [[Fan Trap]]. Approved plan lives at `.claude/plans/write-up-the-spike-temporal-sonnet.md`.

## What it must prove
1. ✅ Steam init on 480, private lobby (overlay invite doesn't inject on 480 direct-launch → overlay-free **Join-by-SteamID**; [[Steam Integration]])
2. ✅ FishNet connect host+client over SDR across two PCs ([[FishNet Netcode]])
3. ✅ Two bodies synced + **client-side prediction** ([[Network Player Controller]], [[Network Spawner]])
4. ✅ Hose simulates on host, renders + smooths on client ([[Network Hose]])
5. ✅ Zero-sum air-share over the wire, per-tick synced ([[Network Air Budget]])

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

## Update — hose enabled (2026-06-02)
- Verified [[Network Hose]] + [[Network Air Budget]] are already fully implemented; both prefabs registered in `DefaultPrefabObjects.asset`; scene `NetPlayerSpawner` wired (player + link prefabs assigned).
- Set `spawnHoseLink: 1` in `NetworkSpikeTest.unity` → the link (hose rope + zero-sum air-share) spawns when both players join. This is **playtest 2** (the movement-only build = playtest 1; uncheck the toggle + rebuild to get it back).
- **Watch:** `NetworkPlayer`'s NetworkTransform syncs scale AND `InflatableCharacter` drives scale locally from the synced budget (double-drive). Should converge; if the remote body's scale jitters, set **Synchronize Scale = off** on the player NetworkTransform (scale is derived everywhere from the budget).

## Update — prediction + smoothness (2026-06-03)
2-PC test: spawn / move / hose / air-share all worked but **felt laggy** (unpredicted host-auth) → enabled **client-side prediction** ([[Network Player Controller]] rewrite). The first prediction build still lagged because the **TimeManager PhysicsMode wasn't set** (silent no-op — see [[Setup and Gotchas]]); fixed. Then a smoothness pass: **graphical-object smoothing** (sprite→child), **hose client interpolation + endpoint anchoring**, **per-tick budget sync** (was 10 Hz → size desync). Result: "much better and smoother." Remaining tuning: deflated remote-body jitter + verifying budget sync — awaiting playtest. The Photon Fusion switch stays parked unless this still isn't smooth enough ([[Networking Solutions - Comparison]]).

## Next session
Awaiting the playtest of the smoothness / air-budget fixes. If the deflated remote-body jitter persists → bump NetworkObject `_spectatorInterpolation`. Then codify the manual scene/prefab setup into a `NetworkSpikeSceneBuilder`, and reset **Project Settings → Physics 2D → Simulation Mode → Fixed Update** for local play (see [[Setup and Gotchas]]). See [[Roadmap]].

## Connections
[[Networking Architecture]] · [[Steam Integration]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Windows Build]] · [[Setup and Gotchas]]
