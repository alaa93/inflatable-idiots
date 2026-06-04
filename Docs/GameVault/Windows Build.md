---
status: implemented
tags: [tooling, editor]
---

# Windows Build

Editor build script (**`Inflatable Idiots ▸ Build Windows (x64)`** — top-level menu, moved out of `Tools` 2026-06-04, now behind a confirmation dialog) → `Builds/Windows/InflatableIdiots.exe`. Mac/Linux are commented stubs; mobile unwired (deferred).

- `Assets/InflatableIdiots/Editor/BuildWindows.cs`
- `Builds/` is gitignored

## ⚠️ For the networked spike
Before testing on a 2nd PC:
1. Set the spike scene (`NetworkSpikeTest`) as the **startup scene** in Build Settings
2. `steam_appid.txt` (containing `480`) is now **auto-copied** next to the built `.exe` on a successful build (✅ done — was the #1 "Steam works in editor, not in build" bug). See [[Steam Integration]] / [[Setup and Gotchas]].

## Connections
[[Test Scene Builder]] · [[Networking Spike]] · [[Steam Integration]] · [[Setup and Gotchas]]
