---
status: implemented
tags: [level, system]
---

# Run Controller

Owns the win/lose state for a single run (`Playing / Won / Lost`).

## Behaviour
- **Start gate:** the run — and the [[Rising Hazard]]'s climb — doesn't begin until `RequiredPlayers` (2) are present (`RunStarted`). Locally both exist at scene load → starts frame one; networked it waits for the partner so the lone host isn't caught first. Each machine gates off its own visible player count (no replication).
- [[Goal Zone]] reports a win; [[Rising Hazard]] reports a loss
- **Local play:** on win/lose sets `Time.timeScale = 0` (freezes hazard AND controls in one stroke); **R reloads the scene**
- **Altitude** tracking: peak height any player reaches above spawn, shown live and frozen on the end screen

## Networked run coordination (2026-06-06)
The same RunController drives the networked scene through the `IRunNetwork` seam, so it stays network-agnostic. `NetworkRunController` — a `NetworkBehaviour` on the [[Network Hose]]/[[Network Air Budget]] link (which spawns once both players join) — attaches itself on spawn and makes the run **host-authoritative**:
- **Win/lose is the host's call**, published via a `SyncVar<RunState>` every peer mirrors so banners agree (a client's local ReportWin/Loss is ignored — only the host's sets state).
- **No `timeScale = 0` when networked** (it would stall FishNet's tick), so two things stand in for it (2026-06-06 polish):
  - **Hazard height is host-authoritative** — the host publishes its climb each tick (`PublishHazardHeight` → a per-tick `SyncVar<float>`); clients mirror it instead of self-rising (`TryGetNetworkHazardHeight`), so the bar can't drift between machines. See [[Rising Hazard]].
  - **Controls lock off-run** — `ControlsLocked` (= `!RunStarted || IsOver`) is read by [[Network Player Controller]], which zeroes the replicate input before the run starts and after it ends. (Local play already gets this free from the timeScale freeze.)
- **R restarts IN PLACE, either player** (client → `ServerRpc`, host direct): the host teleports both players to spawn ([[Network Spawner]] `ServerResetPlayers`) + resets the budget ([[Network Air Budget]] `ResetServer`), then resets the hazard-height SyncVar to `NaN` and bumps a restart-epoch SyncVar whose change resets each peer's local hazard. Reloading the scene was the original bug — it dropped the Steam/FishNet session + the host's join GUI.
- **Clear latched per-session state on restart:** [[Goal Zone]]'s win latch (`reported`) + occupants survive an in-place restart, so `ResetForRestart` clears them — else a replayed win never completes (found in 2-PC testing). Same idea for the hazard position.

⚠️ One manual step: add `NetworkRunController` to the `NetworkLink` prefab (its refs are wired in code by [[Network Spawner]]). See [[Setup and Gotchas]].

## ⚠️ Placeholder UI
All UI is `OnGUI` (banners, altitude readout). Real TMP/Canvas UI is deferred — see [[Roadmap]].

## Where it lives
- `Assets/InflatableIdiots/Scripts/Level/RunController.cs` (+ `IRunNetwork.cs` seam)
- `Assets/InflatableIdiots/Scripts/Net/NetworkRunController.cs`

## Connections
[[Fan Trap]] · [[Goal Zone]] · [[Rising Hazard]] · [[Player Character]] · [[Network Spawner]] · [[Network Air Budget]] · [[FishNet Netcode]] · [[Roadmap]]
