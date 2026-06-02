---
status: decision
tags: [decision, networking, design]
date: 2026-06-02
---

# Decision · 2 players now, possibly 3–4 later

**Fork:** Player-count ambition?

**Chosen:** 2 now, possibly 3–4 later. So: keep the **netcode N-agnostic** (it's free under host-authority) and avoid hardcoding "2", but **do NOT build** the N-player air/hose mechanic yet.

## The asymmetry that drove this
- **Netcode**: 2→4 is essentially free (host-auth scales, Steam lobbies scale, bandwidth trivial)
- **Mechanic**: 2→4 is a real *design* shift, because the differentiator is **pairwise** — [[AirPair]] is literally a pair (`P2 = 1 − P1`), [[Air Hose]] has two endpoints. Going to 4 forces a topology question (two pairs? a chain? a shared pool?) that changes what the game *is*.

## Consequences
- Cheap hygiene now: prefer lists + count-based conditions ([[Goal Zone]]'s `requiredPlayers` already is); isolate the pairwise assumption behind one seam ([[AirPair]] / [[Network Air Budget]])
- The N-player mechanic is a [[Parked Ideas|parked]] design exploration for *after* online 2P is proven

## Connections
[[Networking Architecture]] · [[AirPair]] · [[Air Hose]] · [[Network Air Budget]] · [[Goal Zone]] · [[Parked Ideas]]
