---
status: validated
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

## Update — SMOOTHNESS RESOLVED ✅ (2026-06-04)
Smoothest 2-PC test in the project's history — physics, sync, and smoothing all clean. The **Photon Fusion migration is DROPPED**: the cheap FishNet fixes worked, exactly as predicted ([[Networking Solutions - Comparison]]). Root cause of the residual stutter (worst when a player was **deflated** moving into the taut hose): spectated players were *predicted* on the client via state forwarding, but the hose force is host-only — so the client's hose-less prediction constantly diverged from the host → reconcile stutter, amplified by the heavy deflated body. The user's "it's the mass" hunch was right.

Fix chain (committed `a98443a`..`655c02a`):
- **Spectators sync via NetworkTransform, not prediction** — `Enable State Forwarding = OFF` + a server-auth NetworkTransform (Position only, Send To Owner off). The owner still predicts (instant controls); spectators follow the host's real, hose-affected motion. See [[Network Player Controller]] / [[Setup and Gotchas]].
- **Spectator bodies set kinematic** (in `OnStartClient`, when not owner + not host) so local gravity/collisions stop fighting the synced position — fixed idle drift + sinking through floors.
- **Hose spring softened** 4→2.5 Hz, damping 0.7→0.9 ([[Network Hose]]).
- **Air-size desync fixed** — per-tick budget sync + retry-wire ([[Network Air Budget]]).
- **Own-player sprite floor-dip fixed** — excluded Scale from the prediction graphical smoother + box collider ([[Player Character]]).
- Added an on-screen [[Performance HUD]] (FPS + tick + RTT) for the perf tests.
- **Initial-load fix** (`9600d09`): networked players rendered deflated until the air-budget link wired — now default to the even 50% split at load, so both load matching ([[Network Air Budget]]).

## Update — PlayerTest physics freeze fixed ✅ (2026-06-04, `88fec01`)
The committed `SimulationMode=Script` (FishNet's runtime leak) froze local non-networked physics, and a plain revert re-leaks. Fixed exactly as planned: committed **Fixed Update** as the project default + added a `LocalPhysicsDriver` (`Scripts/Level/`) that drives `Physics2D.Simulate()` in non-networked scenes whenever the mode was left in Script — no-op when a NetworkManager is present (FishNet owns the step) or physics already auto-simulates. Because it keys off the **runtime** mode, `PlayerTest` works even though Unity re-leaks `m_SimulationMode: 2` into the `.asset` after networked play (now just git-hygiene, not a freeze). Wired into [[Test Scene Builder]]. Full write-up in [[Setup and Gotchas]].

## Update — scene builder + Fan Trap run-coordination ✅ (2026-06-06)
- **`NetworkSpikeSceneBuilder`** (`f9314c5`): one-click rebuild of the networked scene from code (full Fan Trap arena + NetworkManager prefab instance + NetSpike spawner wiring + HUD), the sibling of [[Test Scene Builder]]. Shared arena geometry extracted into `SceneBuildHelpers`. Assembles from the existing prefabs (their FishNet wiring is inspector-only).
- **Fan Trap run-coordination ported** (`6188373`): the rising hazard now **waits for both players** before climbing, and **restart resets the run IN PLACE** (either player) instead of reloading the scene — a reload dropped the Steam/FishNet session + host GUI. Host-authoritative via a new `NetworkRunController` on the link (SyncVar run-state + restart epoch + either-player ServerRpc). Bug found + fixed in 2-PC testing: [[Goal Zone]]'s win latch survived the in-place restart and blocked replays → cleared on restart. See [[Run Controller]] / [[Setup and Gotchas]].

## Next session
📌 Finish the [[Fan Trap]] netcode port: authoritative hazard *position* sync (each machine runs its own hazard; re-syncs only on restart) + lock controls on the end screen. Then [[Voice Chat - Dissonance]]. See [[Roadmap]].

## Connections
[[Networking Architecture]] · [[Steam Integration]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Performance HUD]] · [[Windows Build]] · [[Setup and Gotchas]]
