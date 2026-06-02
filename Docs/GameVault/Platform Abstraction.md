---
status: implemented
tags: [networking, architecture]
---

# Platform Abstraction

A thin `IPlatform` interface that hides the store SDK behind store-agnostic verbs (identity / lobby / invite). IDs are plain `ulong` — **zero Steamworks types leak out** — so a future `EpicPlatform` implements the same surface without touching game code.

## Principle: abstract by extraction, not prediction
The interface shape is taken from a real working Steam implementation ([[Steam Integration]]), not guessed up front. Only the verbs the [[Networking Spike]] actually uses live here.

## Surface
`Initialize / Shutdown / RunCallbacks`, `IsReady / LocalId / LocalName`, `CreateLobby / LeaveLobby / OpenInviteOverlay`, `SetLobbyData / GetLobbyData`, events `OnLobbyCreated / OnInviteAcceptedJoinLobby / OnLobbyEntered`.

- `Assets/InflatableIdiots/Scripts/Platform/IPlatform.cs`
- Implemented by [[Steam Integration]] (`SteamPlatform.cs`)
- Why it exists: [[Decision - Steam-first behind wrapper]]

## Connections
[[Steam Integration]] · [[Networking Architecture]] · [[Decision - Steam-first behind wrapper]] · [[Network Spawner]]
