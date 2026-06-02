---
status: validated
tags: [process, core]
---

# MVP Scope and Validation

A deliberately lean 2-week MVP built to test the [[Core Hypothesis]] cheaply before scaling scope.

## Locked MVP scope
- 2D, Rigidbody2D, primitives only (art deferred)
- **One** level: [[Fan Trap]]
- Local PC, 2 players (no netcode yet)
- Hose = `LineRenderer` + `SpringJoint2D` ([[Air Hose]]), no Verlet rope
- No meta systems

## What it validated
- [[Air-Share Mechanic]] (the differentiator)
- [[Inflation State]] (floaty vs anchored asymmetry)
- [[Air Hose]] (the kite-string moment in the [[Fan Trap]])

## Validation gate — PASSED (~2026-06-01)
Shared the gym build with a few friends. Result: concept grasped quickly, inflation liked, co-op feeling clear and engaging. The "we want more mechanics / harder levels" complaints were expected — the gym level was only ever about feel, not content. Owner self-reports **~40% confidence** in the overall concept → proceed to scope expansion.

## Next phase
Scope expansion #1 = online play. See [[Networking Architecture]] and [[Roadmap]].

## Connections
[[Inflatable Idiots]] · [[Core Hypothesis]] · [[Parked Ideas]]
