---
status: implemented
tags: [level]
---

# Goal Zone

The win trigger for the [[Fan Trap]]. Reports a win to [[Run Controller]] once **both** players are inside simultaneously (`requiredPlayers = 2`, counted via a `HashSet`).

Getting both here enforces the take-turns air-share — only one player can be light enough to cross at a time ([[Air-Share Mechanic]]).

- `Assets/InflatableIdiots/Scripts/Level/GoalZone.cs`
- `requiredPlayers` is already a **count** (generalizes cleanly to more players — see [[Decision - 2P now 4P later]])

## Connections
[[Fan Trap]] · [[Run Controller]] · [[Air-Share Mechanic]] · [[Rising Hazard]]
