---
status: implemented
tags: [level, system]
---

# Run Controller

Owns the win/lose state for a single run (`Playing / Won / Lost`).

## Behaviour
- [[Goal Zone]] reports a win; [[Rising Hazard]] reports a loss
- On either, sets `Time.timeScale = 0` — one stroke that freezes the hazard AND player controls (movement is in FixedUpdate)
- **Altitude** tracking: peak height any player reaches above spawn, shown live and frozen on the end screen
- **R restarts** at any time (polls `Keyboard.current` in Update, which ignores timeScale; resets timeScale then reloads the scene)

## ⚠️ Placeholder UI
All UI is `OnGUI` (banners, altitude readout). Real TMP/Canvas UI is deferred — see [[Roadmap]].

## Where it lives
- `Assets/InflatableIdiots/Scripts/Level/RunController.cs`

## Connections
[[Fan Trap]] · [[Goal Zone]] · [[Rising Hazard]] · [[Player Character]] · [[Roadmap]]
