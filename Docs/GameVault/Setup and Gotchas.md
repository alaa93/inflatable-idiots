---
status: reference
tags: [reference, setup]
---

# Setup and Gotchas

Hard-won setup landmines for the networking stack ([[Tech Stack and Versions]]). Future-you will thank present-you.

## Steam / native DLL
- **`steam_appid.txt` = `480`** must sit in the process working directory: project **root** for the editor, **next to the .exe** for builds (see [[Windows Build]]). It's committed (not gitignored).
- **Native DLL loads only at Editor startup.** After importing Steamworks.NET, fully **restart Unity** or you get `DllNotFoundException: steam_api64`. Managed C# hot-reloads; native DLLs do not.
- **`No SteamClient023` / `FailedGeneric`** — Steam-client interface mismatch. Fixed by a full restart (Steam had a pending client update). **No Steam beta required** → stable clients work → not a release blocker. If a future Steamworks.NET upgrade needs an interface the stable client lacks, pin to a stable-matching version before shipping.
- **Privilege match**: Steam ↔ Unity must run at the same Windows integrity level (don't run one as Administrator and not the other).

## FishNet wiring (the silent breakers)
- **FishySteamworks → Peer To Peer = ON** (uses SteamNetworkingSockets / SDR).
- **Remove FishNet's built-in `PlayerSpawner`** from the demo NetworkManager — [[Network Spawner]] replaces it.
- Spawn players via `SceneManager.OnClientLoadedStartScenes` + `AddOwnerToDefaultScene` (the hook FishNet's own spawner uses), NOT `OnRemoteConnectionState`. See [[Network Spawner]].
- Networked objects need **prefabs** with NetworkObject (contrast: local players are built from code by [[Test Scene Builder]]).

## Prediction gotchas (the silent ones — added 2026-06-03)
These cost a full debugging session because FishNet does **not** warn — it just silently runs unpredicted/laggy:
- **TimeManager Physics Mode = `TimeManager`** is REQUIRED for Rigidbody2D prediction. Without it physics auto-simulates, reconcile can't rewind, and prediction is a **silent no-op** (feels exactly like no prediction). FishNet auto-adds a TimeManager with the default `Unity` mode if you don't add one — so you must add it *and* set the mode.
- **NetworkObject → Enable Prediction + Prediction Type = Rigidbody2D**, and **remove the NetworkTransform** (reconcile replaces it; leaving it on overrides prediction with delayed server positions = client lag).
- **Assign a Graphical Object** (sprite on a child) on the NetworkObject, or reconcile corrections *snap* the body instead of easing → looks laggy/jittery even when prediction works.
- **Physics SimulationMode leaks into ProjectSettings**: after in-editor play-testing, FishNet's runtime Script mode can persist into `Physics2DSettings`/`DynamicsManager` (`m_SimulationMode: 2`). This **freezes local (non-networked) scene physics** until you reset **Project Settings → Physics 2D → Simulation Mode → Fixed Update**. Keep it out of git.
- **SyncVar default send rate is 0.1 s (10 Hz)** — too slow for continuously-changing values. The air budget's size visibly lagged during inflation until set to send every tick (`SyncTypeSettings(0f)`). See [[Network Air Budget]].

## Steam overlay on App ID 480
The in-game Steam overlay (hence `ActivateGameOverlayInviteDialog`) does **not** inject into a directly-launched build on 480 — it only injects when launched through Steam under your own App ID. Not a code bug; the invite code is correct. For 2-PC testing, the spike harness (`NetworkBootstrap`) has an overlay-free **"Join by host SteamID"** path (host auto-copies its SteamID to clipboard). The overlay will work once on a real App ID launched from the Steam library.

## Confirmed working
`SteamAPI.Init` → `Init OK`; 2-PC play with client-side prediction — "much better and smoother" per playtest. Steam standalone fix (auto-copy `steam_appid.txt`) confirmed. See [[Networking Spike]].

## Connections
[[Steam Integration]] · [[FishNet Netcode]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Network Spawner]] · [[Windows Build]] · [[Tech Stack and Versions]] · [[Networking Spike]]
