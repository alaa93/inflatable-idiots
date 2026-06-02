---
status: implemented
tags: [networking, steam]
---

# Steam Integration

`SteamPlatform` — the Steamworks.NET implementation of [[Platform Abstraction]]. Owns the SteamAPI lifecycle (Init / RunCallbacks / Shutdown) and the lobby + invite handshake.

## Flow
- **Host**: `CreateLobby(private, 2)` → on `LobbyCreated` set `HostSteamID` in lobby data → start FishNet server + self-client → `ActivateGameOverlayInviteDialog`
- **Joiner**: accept overlay invite → `GameLobbyJoinRequested` → `JoinLobby` → `LobbyEnter` → read `HostSteamID` → set FishySteamworks client address → start client
- Driven by `NetworkBootstrap`; pumped once/frame via `RunCallbacks`

## Testing without publishing
- App ID **480 (Spacewar)** via `steam_appid.txt` at project root → real lobbies + SDR for free
- Confirmed working: `Init OK as Scorpio (765611981...)`

## Where it lives
- `Assets/InflatableIdiots/Scripts/Platform/SteamPlatform.cs`
- Transport: [[FishNet Netcode]] via FishySteamworks (SteamNetworkingSockets / SDR)

## Setup landmines
See [[Setup and Gotchas]] (DLL restart, SteamClient023, appid location, privilege match).

## Connections
[[Platform Abstraction]] · [[FishNet Netcode]] · [[Networking Architecture]] · [[Setup and Gotchas]] · [[Tech Stack and Versions]] · [[Voice Chat - Dissonance]]
