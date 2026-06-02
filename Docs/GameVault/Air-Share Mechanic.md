---
status: implemented
tags: [mechanic, core]
---

# Air-Share Mechanic

The differentiator. Two players share **one** air budget that always sums to 1.0. Pressing your trigger pulls air *toward you* — a zero-sum tug-of-war. This is the system that proves the [[Core Hypothesis]].

## The formula (authoritative — PDF version)
```
airFlow      = (triggerP1 - triggerP2) * transferSpeed * dt
P1Budget     = Clamp(P1Budget + airFlow, 0.1, 0.9)
P2Budget     = 1.0 - P1Budget
```
- `transferSpeed` = 1.5 (default on disk)
- Budget clamped to **[0.1, 0.9]** so neither player fully starves the other
- Both pressing = stalemate (zero net flow) → [[Stalemate Feedback]]
- Single-sided press = transfer (like arm-wrestling)

## Where it lives
- Local: [[AirPair]] (`Assets/InflatableIdiots/Scripts/Player/AirPair.cs`)
- The budget drives [[Inflation State]] on each [[Player Character]]
- Online: re-implemented server-authoritatively as [[Network Air Budget]] (AirPair is NOT reused — it isn't a NetworkBehaviour and assumes a single shared keyboard)

## Connections
[[Core Hypothesis]] · [[Inflation State]] · [[AirPair]] · [[Air Hose]] · [[Network Air Budget]] · [[Stalemate Feedback]]
