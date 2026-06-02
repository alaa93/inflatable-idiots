---
status: implemented
tags: [juice, view]
---

# Camera Shaker

Perlin-noise camera offset scaled by [[AirPair]]'s `StalemateIntensity`. One of the three layers of [[Stalemate Feedback]].

- `Assets/InflatableIdiots/Scripts/View/CameraShaker.cs`
- Tuned defaults: maxOffset 0.08, frequency 35

## Open question
Single-screen framing for two (and later up to four — see [[Decision - 2P now 4P later]]) players: dynamic zoom vs split-screen. Deferred until the hose forces players to stay near each other in real levels.

## Connections
[[Stalemate Feedback]] · [[AirPair]] · [[Inflatable Idiots]]
