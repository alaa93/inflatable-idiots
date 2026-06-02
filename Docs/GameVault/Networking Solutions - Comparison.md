---
status: reference
tags: [networking, reference]
date: 2026-06-02
---

# Networking Solutions · Comparison

A learning/landscape note: the core netcode concepts behind the [[Decision - FishNet host-authoritative]], and how the main Unity networking solutions differ. Captured from a 2026-06-02 explainer session. This is background knowledge, **not** a re-open of the decision.

---

## The core concepts (mental models)

### The "boss computer" — host authority
One machine is the **source of truth** (the host / server). For us it's a **listen-server**: one friend hosts *and* plays, the other joins as a client. The host runs the real physics; clients mostly **copy** what the host says.

This is what stops two screens from disagreeing. See [[Networking Architecture]].

### The latency problem
Asking the boss "is this right?" takes a network round trip (~60–100ms). If your own character only moved *after* the boss replied, your inputs would feel mushy and delayed — bad for a twitchy physics game.

### Client-side prediction
The client **guesses ahead**: the moment you press a button, your own machine runs the same physics locally and shows the result *now*, without waiting for the boss. Makes your own character feel instant.

### Reconciliation
A guess can be wrong (the host knew something you didn't — a collision, the partner eating the air budget). When the host's authoritative state arrives, the client **snaps to it and replays** its buffered inputs to catch back up to "now." Done well, it's a tiny smoothed correction, not a rubber-band snap.

> **Key insight:** Prediction/reconciliation is about *responsiveness (snappy feel)*, **NOT** about stopping drift. The boss computer is what stops drift. Prediction actually *reintroduces* a small drift risk on the client (because the client now simulates too) — and reconciliation is the cleanup for it.

---

## Determinism & the "crumbs" problem

### What "deterministic" means
Same start → exactly the same ending, every time. **Cross-machine** deterministic = two *different* computers get the *exact* same answer.

### Why Unity's Physics2D (Box2D) is NOT cross-machine deterministic
Computers do decimal math with tiny rounding ("crumbs" — like a calculator stopping at `0.3333...`). Different CPUs / OSes / **builds** (a Windows build vs a Mac build are compiled differently) round in slightly different spots. Physics amplifies tiny differences fast (domino effect), so two machines drift into different game states.

- This is a **computer-math reality**, not a Unity bug — but it's *also* a Unity **design choice** not to fight it.
- On the **same** machine + same build, the math **does** match — which is exactly why client-side *local replay* (reconciliation) works.
- It only breaks **across** machines/builds. And "same computers would fix it" is true in theory but false in practice — friends never have truly identical hardware + build.
- **Beating it is possible** but expensive: you need fixed-point math + fixed operation order + your own physics engine (see Quantum below). Games that *need* it (RTS, fighting games, Factorio) pay that price on purpose.

### Two ways to build netcode (and why we rejected one)
- **Deterministic lockstep** — send only *inputs*; every machine simulates independently and must stay byte-identical forever. **Rejected** — Unity Physics2D isn't cross-machine deterministic, so it would desync.
- **State replication (host-authoritative)** — host sends authoritative *state*; clients copy/reconcile to it. Non-determinism gets corrected continuously instead of accumulating. **This is what we use.**

---

## The solutions compared

### FishNet — *chosen* (see [[FishNet Netcode]])
- **Prediction/reconciliation:** ✅ optional — host-auth now, enable prediction later **without swapping frameworks**.
- **Physics syncing:** Unity physics, server-authoritative, optional predict + rollback.
- **Why us:** free, Steam-native (FishySteamworks over SDR), and the safe hedge when latency-feel is unknown.

### Mirror — rejected alternative
- **Prediction/reconciliation:** ❌ none built-in.
- **Physics syncing:** copy positions + interpolate (still uses a boss, so it does **not** drift — its only downside is your own character can feel a bit laggy).
- **Why not:** simpler, but adding prediction later would mean switching frameworks. Right call only if the feel were *known* chill.

### Photon — a family, not one product
- **Photon PUN** (legacy): ❌ no prediction. Owner simulates + broadcasts transforms through Photon Cloud; others interpolate. Being retired.
- **Photon Fusion** (modern): ✅ strong prediction/reconciliation — the real FishNet rival. Tick-based, server/host mode, runs Unity physics with rollback.
- **Photon Quantum**: ✅ deterministic predict-rollback — but uses its **own fixed-point physics engine** instead of Unity's. This is the "beat the crumbs" approach: no crumbs → only inputs need syncing. Would mean rebuilding our physics from scratch.
- **Why not (for us):** Photon runs through **Photon Cloud, priced by concurrent users (CCU)** = ongoing cost as the game grows, vs. FishNet + Steam SDR = **$0 forever**. Also less Steam-native. Fusion was a legit alternative; FishNet won on *free + Steam-native*, not on features.

---

## At a glance

| Solution | Prediction? | Physics syncing | Cost model | Verdict |
|---|---|---|---|---|
| **FishNet** | ✅ optional | Unity physics, boss + optional predict | Free (+ free Steam SDR) | **Chosen** |
| Mirror | ❌ | copy positions + interpolate | Free | Rejected (no upgrade path) |
| Photon PUN | ❌ | broadcast transforms + interpolate | Cloud CCU | Legacy, skip |
| Photon Fusion | ✅ strong | Unity physics, boss + rollback | Cloud CCU | Real rival; cost lost it |
| Photon Quantum | ✅ deterministic | **own** fixed-point physics | Cloud CCU | Overkill; full rewrite |
| Deterministic lockstep | ✅ (needs determinism) | inputs only | n/a | Rejected — Box2D not deterministic |

---

## Bottom line for this project
FishNet was picked **not** because it stops drift better (any host-authoritative option does) but because it's **free, Steam-native, and lets us add the snappy-feel prediction trick later without changing frameworks** — the safest bet while the two-PC latency feel is still unknown. The [[Networking Spike]] measures that feel before we decide whether to turn prediction on.

## Connections
[[Decision - FishNet host-authoritative]] · [[FishNet Netcode]] · [[Networking Architecture]] · [[Networking Spike]] · [[Tech Stack and Versions]] · [[Voice Chat - Dissonance]]
