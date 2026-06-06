# 🎈 Inflatable Idiots

> A two-player physics co-op game about sharing one breath of air — and never quite agreeing on who gets it.

<p>
  <img alt="Unity 6" src="https://img.shields.io/badge/Unity-6000.3-000000?logo=unity&logoColor=white">
  <img alt="C#" src="https://img.shields.io/badge/C%23-2D%20%2F%20URP-239120?logo=csharp&logoColor=white">
  <img alt="Netcode: FishNet" src="https://img.shields.io/badge/Netcode-FishNet%204.7-1f6feb">
  <img alt="Platform: Steam" src="https://img.shields.io/badge/Platform-Steam-171a21?logo=steam&logoColor=white">
  <img alt="Status: prototype" src="https://img.shields.io/badge/status-playable%20prototype-f59e0b">
  <img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-green">
</p>

---

## The concept

Two inflatable characters are joined by a permanent **hose** and share **one** pool of air that always sums to 100%. Pump air into yourself and you go **light and floaty**; lose it to your partner and you go **heavy and anchored**. There is never enough air for both of you to be light at once — so every obstacle becomes a frantic, funny negotiation:

> **"GIVE ME 40% — NOW!"**

That mid-action renegotiation *is* the game. It's the one hypothesis the whole project exists to test, and it held up in playtesting: people grasped it immediately and the co-op feel landed.

## The core mechanic — shared air

The air budget is a single zero-sum value. Holding your air-transfer trigger pulls the budget *toward you*; your partner doing the same pulls it back — an arm-wrestle over a shared resource.

```
airFlow   = (triggerP1 − triggerP2) · transferSpeed · dt
P1Budget  = clamp(P1Budget + airFlow, 0.1, 0.9)
P2Budget  = 1.0 − P1Budget
```

- The budget is clamped to **[0.1, 0.9]** so neither player can fully starve the other.
- Both pressing at once = **stalemate** (zero net flow), with juice/feedback to sell the struggle.
- The budget drives each body's **mass, gravity, and scale** — light bodies get flung by wind, heavy bodies stay put.

## The level — *Fan Trap*

The single gym level, built to be the most legible demo of *why* shared air matters:

- A **Fan Zone** updraft lifts a **fully inflated** player like a kite, while their **deflated** partner stays anchored to the ground.
- The hose tethers the floating kite to the anchor — the signature *hose-as-kite* moment.
- To get **both** players into the **Goal Zone**, they have to take turns being the light one — only one can cross at a time.
- A **Rising Hazard** ("lava") climbs steadily underneath as constant time pressure.

## Status

| Milestone | State |
|---|---|
| Core air-share + inflation + hose | ✅ Implemented & playtested |
| *Fan Trap* gym level (local 2P) | ✅ Implemented & validated |
| Online co-op spike (Steam + 2 PCs) | ✅ Validated — smoothest test to date |
| *Fan Trap* run coordination over the network | ✅ Host-authoritative start-gate, in-place restart, synced hazard, off-run control lock |
| Fan Zone force authority over the network | 🔄 Implementing / to validate |
| Voice chat, real UI, art pass | 🔭 Planned |

This is an in-development **prototype** by a solo developer — primitive art, programmer UI, and a deliberately lean scope while the core idea is being proven. The current frontier is taking the validated local co-op online.

## Tech stack

- **Engine:** Unity **6000.3** (Unity 6), 2D + URP, Rigidbody2D physics
- **Input:** Unity New Input System (gamepad + keyboard/mouse)
- **Netcode:** [FishNet](https://github.com/FirstGearGames/FishNet) **4.7** — host-authoritative listen-server with client-side prediction & reconciliation
- **Transport / online:** Steam Datagram Relay via FishySteamworks **4.1**, Steamworks.NET **2025.163** (SDK 1.63)
- **Architecture:** platform services sit behind a thin `IPlatform` wrapper (Steam-first, others by extension); voice (Dissonance) is deferred but transport-based and store-agnostic

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | `WASD` | Left stick |
| Jump | `Left Ctrl` | South button |
| Transfer air (inflate) | `Left Shift` | Trigger |
| Restart run | `R` | — |

*(Two players share the keyboard locally; online play uses one input device per machine.)*

## Project layout

```
Assets/InflatableIdiots/      First-party game code
  ├─ Scripts/Player/          Characters, shared air budget, movement
  ├─ Scripts/Hazards/         Fan Zone, Rising Hazard
  ├─ Scripts/Level/           Run Controller, win/lose, altitude
  ├─ Scripts/Net/             FishNet controllers, spawner, networked hose/air/run
  ├─ Scripts/Platform/        IPlatform wrapper + Steam implementation
  └─ Editor/                  One-click scene builders (local + networked)
Assets/Third-Party/           FishNet, FishySteamworks, Steamworks.NET (committed)
Docs/GameVault/               Design + engineering knowledge base (see below)
```

## Documentation

The full design and engineering rationale lives in **[`Docs/GameVault/`](Docs/GameVault/)** — an interlinked knowledge base (best browsed in Obsidian's graph view). Good entry points:

- **[Inflatable Idiots](Docs/GameVault/Inflatable%20Idiots.md)** — map of content / home node
- **[Core Hypothesis](Docs/GameVault/Core%20Hypothesis.md)** — the one bet the project tests
- **[Air-Share Mechanic](Docs/GameVault/Air-Share%20Mechanic.md)** — the differentiator, in detail
- **[Networking Architecture](Docs/GameVault/Networking%20Architecture.md)** — the online plan
- **[Roadmap](Docs/GameVault/Roadmap.md)** — where it is and what's next

## License

First-party code and docs in this repository are released under the **[MIT License](LICENSE)** (© 2026 Ala Abuhadbeh). Third-party packages bundled under `Assets/Third-Party/` (FishNet, FishySteamworks, Steamworks.NET) are © their respective authors and remain under their own licenses.
