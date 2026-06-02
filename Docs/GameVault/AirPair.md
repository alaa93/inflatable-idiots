---
status: implemented
tags: [mechanic]
---

# AirPair

Owns the shared air budget for a **local** 2-player session and resolves the [[Air-Share Mechanic]] formula each `FixedUpdate`.

## Responsibilities
- Holds `P1Budget` (starts 0.5); `P2Budget = 1 - P1Budget`
- Exposes `StalemateIntensity = Min(p1.Trigger, p2.Trigger)` → drives [[Stalemate Feedback]]
- Force-pairs the single `Keyboard.current` to **both** PlayerInputs on Start (works around Input System's "one device per PlayerInput" rule) — see [[Input System]]

## Where it lives
- `Assets/InflatableIdiots/Scripts/Player/AirPair.cs`

## ⚠️ Not used online
The single-keyboard assumption and non-NetworkBehaviour nature mean AirPair is **replaced** (not reused) in online play by the server-authoritative [[Network Air Budget]].

## Connections
[[Air-Share Mechanic]] · [[Player Character]] · [[Input System]] · [[Stalemate Feedback]] · [[Network Air Budget]]
