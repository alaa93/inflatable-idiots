---
status: built
tags: [level, playtest, networking, puzzle]
---

# Ascent Playtest

The first full co-op **puzzle ladder** — a graybox vertical climb built to decide the big fork: go to real UI/art/level-design, or double down on a few mechanics. Built 2026-06-06 (code-driven, all primitives); compiles clean, both scenes generate, the standalone builds. **Early playtesting positive** (user: "worked well"); a deliberate full 2-PC standalone pass is still the way to lock the verdict.

## The climb (bottom → top)
One enclosed shaft, lava rising from below, six chambers joined by puzzle-gated hatches:
1. **Tutorial** — the [[Fan Trap]] single-kite, take-turns-over-the-wall, repurposed as the intro.
2. **Vertical climb** — float one up / anchor the other, then swap air to bring the anchor up (no fan; pure jump + air-share).
3. **Dual-switch** — HIGH switch (inflated) + LOW plate (deflated) held together. The identity hook; zero-sum air forces one floater + one anchor.
4. **Elevator** — both board the column; a gated lift fan carries them up together (capped speed, mass-independent).
5. **Kite + anchor** — a deflated anchor on the weight plate powers the fan; the light kite rides up to the lever.
6. **Climax** — the dual-switch again, but with **no station relent** (the lava keeps chasing = pressure finale).
Then a Goal room (both inside = win, reusing [[Goal Zone]]).

Everything is ceiling-enclosed on purpose: the inflated jump apex is ~34u, so without ceilings the light player would leap past every puzzle. Each chamber's only exit is its puzzle-gated hatch → the air-share is mandatory.

## Netcode architecture (zero new NetworkObjects)
Reuses the exact proven seam from the [[Networking Spike]]:
- A host-authoritative **64-bit puzzle-flag bitmask** folded into [[Run Controller]]'s `IRunNetwork` seam — synced as one `SyncVar<ulong>` on the link-spawned `NetworkRunController` (alongside run-state + hazard height). Producers write flags on the host; every peer reads the synced mask. See `RunController.GetPuzzleFlag` / `SetPuzzleFlag`.
- Player-affecting forces (lift fan, powered fan) stay in the reconcile-safe `FanZone.SampleForce` → predicted-tick path (see [[Fan Zone]] / [[Network Player Controller]]); gated fans just check a synced flag, so host & owner agree each tick.
- No new prefabs, no scene NetworkObjects — so the netcode risk stays inside already-validated territory.

## New components (all plain MonoBehaviours, run in both scenes)
- `ZoneSensor` — host counts qualifying (inflated / deflated / any) players in a trigger → raises a flag. The one primitive behind every switch/plate/"both aboard"/"both reached" check.
- `FlagGate` — opens a solid graybox gate from flags; latch options (permanent for progression, timed for "who-holds-the-door").
- `FanZone` (extended) — optional activation flag → powered/lift fans.
- `StationZone` — lava relents while a player is inside; chases in the gaps between.
- `RisingHazard` (extended) — in-station vs transition speed + an **L-key on/off toggle** for calm puzzle iteration.
- `FollowCamera` — two-player single-screen dynamic-zoom framing (the long-deferred camera question, graybox answer).
- `PuzzleFlags` — the 0..63 flag-id allocation.

## Enhancements (2026-06-06)
- **Role coloring (net):** networked avatars tint by the synced `_slot` SyncVar — **slot 0 = host = blue, slot 1 = client = red** — so both screens agree who's who. In `NetworkInflatableController` (re-applies on `_slot.OnChange`); local play already colors P1/P2 in the builder.
- **World puzzle labels:** a `WorldLabel` over each puzzle — a 2-word title + a ≤4-word how-to (Fan Trap·"Take turns up", Vertical Climb·"Float, anchor, swap", Dual Switch·"One high, one low", Co-op Lift·"Both aboard to rise", Kite Anchor·"Anchor powers the fan", Final Push·"Lava won't relent", The Goal·"Both reach it"). Drawn via OnGUI projected from the world position (no font-asset setup), sized to a constant world height, drop-shadowed, and self-culling off-screen. Built in `PlaytestLayout.BuildLabels`. Swap to TextMeshPro later if true in-scene 3D text is wanted.

## Build / run
Menu: **Inflatable Idiots → Build Playtest - Local** and **Build Playtest - Net** (code-driven, like [[Test Scene Builder]] / `NetworkSpikeSceneBuilder`). Headless entries `PlaytestSceneBuilder.BuildBothBatch` + `BuildWindows.BuildWindows64Batch`.
- **Local** (`PlaytestLocal.unity`): keyboard 2P (P1 WASD/LCtrl/LShift, P2 Arrows/RCtrl/RShift). The fast solo-iteration scene — use it first to tune puzzle solvability/feel. Enabled in Build Settings (index 1) so R-restart reloads it; `PlaytestNet` is index 0 / startup.
- **Net** (`PlaytestNet.unity`, startup scene): Play → Host on one PC, Join-by-ID on the other (see [[Network Spawner]] / [[Setup and Gotchas]]). Or run the standalone in `Builds/Windows`.
- **L** toggles the lava (host/local).

## ⚠️ Verified vs NOT verified
- ✅ Compiles clean (one pre-existing `CS0108` warning on `NetworkRunController.IsServer`), both scenes generate, NetworkManager/spawner wiring intact, standalone builds + boots into `PlaytestNet`.
- 🟡 **2-PC gameplay over the wire, puzzle solvability/spacing tuning, and reconcile cleanliness of the flag-gated forces** — early playtest reads well; still worth a deliberate full 2-PC standalone pass top-to-bottom to lock the verdict and surface tuning.

## Tuning knobs (first-pass coordinates — expect to move them)
- Dual-switch & climax gates are `latchOncePermanently` for guaranteed traversal; flip to `latchSeconds` to restore the who-holds-the-door tension.
- Lift fan force/cap, hose slack vs vertical separations, hatch-gap alignment after ascents, chamber heights — all in `PlaytestLayout`.
- **Jump speeds:** the net `NetworkPlayer` prefab was tuned to **12/28** (deflated/inflated; inflated apex is huge but ceiling-capped). ⚠️ The LOCAL scene's code-built players still use `InflatableCharacter` defaults **4/10**, so local-vs-net jump feel differs — left unsynced by choice (2026-06-06).

## Connections
[[Fan Trap]] · [[Fan Zone]] · [[Run Controller]] · [[Goal Zone]] · [[Rising Hazard]] · [[Network Player Controller]] · [[Networking Spike]] · [[Air-Share Mechanic]] · [[Inflation State]] · [[Roadmap]]
