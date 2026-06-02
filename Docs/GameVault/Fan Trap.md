---
status: implemented
tags: [level, core]
---

# Fan Trap

The single gym level — chosen as the most legible demo of why the [[Air-Share Mechanic]] matters. Showcases the "hose-as-kite" moment.

## The puzzle
An updraft column ([[Fan Zone]]) lifts a **fully inflated** player like a kite while their **deflated** partner stays anchored ([[Inflation State]]). The [[Air Hose]] tethers the floating kite to the ground anchor. To get **both** players into the [[Goal Zone]], they must take turns sharing air — only one can be light enough to cross at a time.

## Pieces
- [[Fan Zone]] — the updraft
- Blocking wall + [[Goal Zone]] — the win condition (both players inside)
- [[Rising Hazard]] — lose condition / pressure
- [[Run Controller]] — owns win/lose, freeze, restart, altitude

## ⚠️ Design tension (open)
A fully inflated jump apex (~34 units) can also clear the wall, so the fan isn't strictly mandatory. To force it: lower inflated jump or switch the obstacle to a ceiling/overhang. Not yet decided.

## Status
Playtested OK locally. Next: port into the networked flow once the [[Networking Spike]] validates (the spike scene is bare ground only — no Fan Trap yet).

## Connections
[[Air-Share Mechanic]] · [[Inflation State]] · [[Air Hose]] · [[Fan Zone]] · [[Goal Zone]] · [[Rising Hazard]] · [[Run Controller]]
