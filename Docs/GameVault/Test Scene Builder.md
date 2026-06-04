---
status: implemented
tags: [tooling, editor]
---

# Test Scene Builder

Editor tool (**`Inflatable Idiots ▸ Build Test Scene`** — top-level menu, moved out of `Tools` 2026-06-04, now behind a confirmation dialog) that **regenerates** the local `PlayerTest.unity` scene entirely from code — camera, ground, [[Fan Trap]] pieces, two [[Player Character]]s (now built with a `BoxCollider2D`), [[AirPair]], [[Air Hose]], [[Run Controller]], and a `LocalPhysicsDriver` (keeps non-networked physics ticking through FishNet's Script-mode leak — see [[Setup and Gotchas]]).

## Why it matters
The C# builder is the **source of truth** for the scene, which sidesteps Unity scene-file "churn" in git. Players are built from code (no prefabs) — a key contrast with the networked flow, which *needs* prefabs (see [[Network Spawner]]).

- `Assets/InflatableIdiots/Editor/TestSceneBuilder.cs`

## Planned sibling
A `NetworkSpikeSceneBuilder` to codify the (currently manual) networked scene + prefab setup — see [[Networking Spike]].

## Connections
[[Fan Trap]] · [[Player Character]] · [[Air Hose]] · [[Run Controller]] · [[Networking Spike]] · [[Windows Build]]
