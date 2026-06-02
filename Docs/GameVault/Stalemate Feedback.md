---
status: implemented
tags: [mechanic, juice]
---

# Stalemate Feedback

When both players hold AirTransfer at once, net air flow is zero ([[Air-Share Mechanic]]) — but it must *feel* like a struggle. So the game adds juice scaled by `StalemateIntensity = Min(p1.Trigger, p2.Trigger)` (from [[AirPair]]).

## Three layers
1. Perlin **scale jitter** on each [[Player Character]]
2. **Rotation wobble** on each character
3. **Camera shake** — [[Camera Shaker]]

Tuned defaults: camera maxOffset 0.08 / freq 35; per-character scale amp 0.08, rotation amp 6°, freq 15 Hz.

## ⚠️ Caused a real bug
The transform jitter is what broke the [[Air Hose]] springs (off-center anchors pumping energy) — fixed with center anchors. See [[Air Hose]].

## Connections
[[Air-Share Mechanic]] · [[AirPair]] · [[Player Character]] · [[Camera Shaker]] · [[Air Hose]]
