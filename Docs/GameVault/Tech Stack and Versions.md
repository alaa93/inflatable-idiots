---
status: reference
tags: [reference]
---

# Tech Stack and Versions

Confirmed-working stack as of 2026-06-02.

## Engine
- **Unity 6000.3.14f1** (Unity 6 line), 2D, URP, Rigidbody2D physics
- **New Input System** 1.19.0 — see [[Input System]]

## Project layout
- `Assets/InflatableIdiots/` — all first-party code (Scripts, Scenes, Prefabs, Editor)
- `Assets/Third-Party/` — imported packages (committed)
- Single assembly (no asmdefs) — fine for now; FishNet code-gen processes `Assembly-CSharp` because it auto-references `FishNet.Runtime`

## Networking packages (force-imported under `Assets/Third-Party/`)
| Package | Version | Role |
|---|---|---|
| FishNet | **4.7.2** | netcode framework — see [[FishNet Netcode]] |
| FishySteamworks | **4.1.1** | FishNet transport over Steam SDR |
| Steamworks.NET | **2025.163.0** (SDK 1.63) | Steam C# wrapper — see [[Steam Integration]] |

## Release note ⚠️
`SteamAPI.Init` succeeds on the **stable** Steam client — **no Steam beta required** → not a release blocker. If a future Steamworks.NET upgrade ever needs an interface (e.g. `SteamClientNNN`) the stable client lacks, pin Steamworks.NET to a stable-client version before shipping. See [[Setup and Gotchas]].

## Voice (deferred)
[[Voice Chat - Dissonance]] — NOT installed yet; transport-based so store-agnostic.

## Connections
[[Inflatable Idiots]] · [[Networking Architecture]] · [[Setup and Gotchas]]
