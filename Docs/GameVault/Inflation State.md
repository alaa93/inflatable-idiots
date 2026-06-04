---
status: implemented
tags: [mechanic, core]
---

# Inflation State

How a player's share of the air ([[Air-Share Mechanic]]) translates into physical asymmetry. More air = lighter, taller, floatier. Less air = heavier, smaller, anchored.

## Thresholds (lerp between)
| Budget | mass | gravityScale | scale | feel |
|---|---|---|---|---|
| 90% (inflated) | 0.1 | 0.15 | ~2.5 | light, floaty, kite-like |
| 10% (deflated) | 3.0 | 2.5 | ~0.6 | heavy, fast-falling, anchored |

`currentInflationK = InverseLerp(0.1, 0.9, budget)` then lerp mass / gravityScale / scale.

## ⚠️ Not implemented
**Traction** is aspirational only — `walkSpeed` is a constant 6 regardless of inflation. No friction/traction coupling exists in code (despite the GDD).

## Where it lives
- `Assets/InflatableIdiots/Scripts/Player/InflatableCharacter.cs` → `UpdatePhysicsState()`
- Reads budget from [[AirPair]] locally, or from [[Network Air Budget]] online (via the additive `BudgetSource` hook)
- **Default at load (fixed 2026-06-04):** with no source yet (networked, before the [[Network Air Budget]] link wires), `UpdatePhysicsState` defaults `budget = 0.5` and `currentInflationK` inits to `0.5` — so both players load at a matching ~50% mid-size instead of rendering deflated

## Why it matters
This asymmetry is the whole point of the [[Fan Trap]]: a fully inflated player gets flung up by the [[Fan Zone]] like a kite while the deflated partner stays anchored on the ground.

## Connections
[[Air-Share Mechanic]] · [[Player Character]] · [[Fan Zone]] · [[Air Hose]] · [[Network Air Budget]]
