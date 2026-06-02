---
status: implemented
tags: [tooling, editor]
---

# Windows Build

Editor build script (`Tools ▸ Inflatable Idiots ▸ Build Windows (x64)`) → `Builds/Windows/InflatableIdiots.exe`. Mac/Linux are commented stubs; mobile unwired (deferred).

- `Assets/InflatableIdiots/Editor/BuildWindows.cs`
- `Builds/` is gitignored

## ⚠️ For the networked spike
Two manual steps before testing on a 2nd PC:
1. Set the spike scene (`NetworkSpikeTest`) as the **startup scene** in Build Settings
2. Copy `steam_appid.txt` (containing `480`) next to the built `.exe` — see [[Steam Integration]] / [[Setup and Gotchas]]
(Optional later: auto-copy `steam_appid.txt` in this script.)

## Connections
[[Test Scene Builder]] · [[Networking Spike]] · [[Steam Integration]] · [[Setup and Gotchas]]
