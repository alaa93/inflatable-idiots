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
- **Physics SimulationMode leaks into ProjectSettings** (✅ FIXED 2026-06-04, `88fec01`): after in-editor play-testing, FishNet's runtime Script mode persists into `Physics2DSettings`/`DynamicsManager` (`m_SimulationMode: 2`), which **freezes local (non-networked) scene physics** (e.g. [[Test Scene Builder]]'s `PlayerTest`) — nothing calls `Physics2D.Simulate()`. A plain reset to Fixed Update does **not** stick: Unity re-leaks Script after the next networked editor play. **Fix = stop fighting the serialized value.** Committed **Fixed Update** as the default AND added a `LocalPhysicsDriver` MonoBehaviour (`Scripts/Level/LocalPhysicsDriver.cs`, `[DefaultExecutionOrder(int.MaxValue)]` so it steps last) that, in `FixedUpdate`, calls `Physics2D.Simulate(Time.fixedDeltaTime)` **only when** `Physics2D.simulationMode == Script` **and** no NetworkManager exists (`NetworkManager.Instances.Count == 0` — reading the static list avoids `InstanceFinder`'s "NetworkManager not found" log spam). It keys off the **runtime** mode, so `PlayerTest` simulates whether the asset reads `0` or has re-leaked to `2`; it no-ops in networked scenes (FishNet owns the step) and when physics already auto-simulates (native ordering preserved). Wired into [[Test Scene Builder]] so rebuilds keep it. ⚠️ You'll **still see** `m_SimulationMode: 2` re-leak into the two `.asset` files after networked editor play — that's now purely a git-hygiene nuisance (don't commit it); it no longer affects gameplay.
- **SyncVar default send rate is 0.1 s (10 Hz)** — too slow for continuously-changing values. The air budget's size visibly lagged during inflation until set to send every tick (`SyncTypeSettings(0f)`). See [[Network Air Budget]].

## Spectator sync + smoothing (added 2026-06-04)
Prediction is great for the owner but *wrong* for spectated players when forces live host-only:
- **Don't predict spectators that depend on host-only forces.** With `Enable State Forwarding = ON`, clients replay a spectated player's inputs — but the [[Network Hose]] spring exists only on the host, so the replay diverges from the host every tick → constant reconcile **stutter** (worst when deflated = heavy mass + high gravity = sharp motion). Fix: **State Forwarding OFF** + a server-auth **NetworkTransform** (Position only) for spectators; the owner keeps prediction.
- **NetworkTransform config for the predicted-spectator combo:** Client Authoritative = OFF, **Send To Owner = OFF** (the owner predicts; don't fight it), **Component Configuration = Disabled**. Do **NOT** use Component Configuration = Rigidbody2D — its `CanMakeKinematic()` returns `!isServer` under server-auth, so it would *also* freeze the non-host client's **own predicted** body.
- **Make spectator bodies kinematic in code** (`OnStartClient`, when `!IsOwner && !IsServerInitialized`): zero velocity + `bodyType = Kinematic`. Otherwise the still-dynamic body's gravity/collisions fight the synced position → idle drift + sinking through floors. Keep it `simulated` so the hose ends still read `Rigidbody2D.position`. See [[Network Player Controller]].
- **Exclude Scale from the graphical smoother when scale is gameplay-driven.** The smoother defaults to smoothing ALL properties (`255`). [[Inflation State]] drives scale every frame from the synced budget, so smoothing it made the owner's sprite mis-scale and **dip into the floor** (spectators, with no smoother, looked fine). Set smoothed properties to **Position+Rotation only (`3`)** — scale never comes from reconcile, so it needs no smoothing. See [[Player Character]].

## Networked run coordination (added 2026-06-06)
- **A NetworkBehaviour added to a spawned prefab must be authored on the prefab, not at runtime.** `NetworkRunController` was dragged onto the `NetworkLink` prefab in Unity so FishNet assigns its NetworkObject component index; its `_componentIndexCache` serializes as `255` (unset) until reassigned at runtime — fine (the 2-PC restart worked with `255`). Adding a NetworkBehaviour via `AddComponent` before `Spawn` is NOT reliable.
- **Don't freeze with `Time.timeScale = 0` in networked play** — it stalls FishNet's TimeManager tick. The local scene freezes on win/lose that way; the networked run instead stops the hazard on `IsOver` and drives banners off a host-authoritative `SyncVar<RunState>`. See [[Run Controller]].
- **An in-place restart must clear latched per-session state.** Reloading a scene resets everything for free; an in-place networked restart (kept so the session/GUI survive) does not. [[Goal Zone]]'s `reported` win-latch + `inside` occupants survived the restart and silently blocked the next win — clear them (`ResetForRestart`), and reset the hazard position. Anything with a one-shot latch is suspect.

## Steam overlay on App ID 480
The in-game Steam overlay (hence `ActivateGameOverlayInviteDialog`) does **not** inject into a directly-launched build on 480 — it only injects when launched through Steam under your own App ID. Not a code bug; the invite code is correct. For 2-PC testing, the spike harness (`NetworkBootstrap`) has an overlay-free **"Join by host SteamID"** path (host auto-copies its SteamID to clipboard). The overlay will work once on a real App ID launched from the Steam library.

## Confirmed working
`SteamAPI.Init` → `Init OK`; 2-PC play — **smoothest test in the project's history (2026-06-04)**: physics, sync, and smoothing all clean after the spectator-NetworkTransform + kinematic-body + scale-smoothing fixes. Steam standalone fix (auto-copy `steam_appid.txt`) confirmed. See [[Networking Spike]].

## Connections
[[Steam Integration]] · [[FishNet Netcode]] · [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Network Spawner]] · [[Player Character]] · [[Performance HUD]] · [[Windows Build]] · [[Tech Stack and Versions]] · [[Networking Spike]]
