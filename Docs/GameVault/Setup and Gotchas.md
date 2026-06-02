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
- **NetworkTransform → Client Authoritative = OFF** (server authority), Component Config = **Rigidbody2D**. With it ON, the kinematic owner sends a frozen transform and nothing syncs.
- **FishySteamworks → Peer To Peer = ON** (uses SteamNetworkingSockets / SDR).
- **Remove FishNet's built-in `PlayerSpawner`** from the demo NetworkManager — [[Network Spawner]] replaces it.
- Spawn players via `SceneManager.OnClientLoadedStartScenes` + `AddOwnerToDefaultScene` (the hook FishNet's own spawner uses), NOT `OnRemoteConnectionState`. See [[Network Spawner]].
- Networked objects need **prefabs** with NetworkObject (contrast: local players are built from code by [[Test Scene Builder]]).

## Confirmed working
`SteamAPI.Init` → `Init OK as Scorpio`; solo host spawns + moves. See [[Networking Spike]].

## Connections
[[Steam Integration]] · [[FishNet Netcode]] · [[Network Spawner]] · [[Windows Build]] · [[Tech Stack and Versions]] · [[Networking Spike]]
