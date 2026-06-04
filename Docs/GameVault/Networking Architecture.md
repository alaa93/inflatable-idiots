---
status: in-progress
tags: [networking, architecture]
---

# Networking Architecture

The master plan for online co-op (scope expansion #1 past the validated [[Core Hypothesis]]). Steam-first.

## Core model
**Host-authoritative listen-server**, peer-to-peer over **Steam Datagram Relay (SDR)**. One friend hosts AND plays; the other joins. No dedicated servers. **No deterministic lockstep** (Unity Physics2D isn't deterministic cross-machine — host authority sidesteps it).

## Two layers (abstracted differently)
- **Transport** (the byte pipe) — abstracted *for us* by [[FishNet Netcode]] (swap transports for Steam/Epic/direct)
- **Platform services** (identity / lobby / invite) — where *our* wrapper lives: [[Platform Abstraction]] with a Steam implementation ([[Steam Integration]])

## Steam = login + party (don't build your own)
Steam account = identity; Steam **Lobbies** + friends overlay = the private party + invite. Tested for free on **Spacewar App ID 480** before paying the $100 Steam Direct fee.

## The hard part: the hose couples both bodies
Host simulates the rope; clients render it from replicated node positions — see [[Network Hose]]. The [[Air-Share Mechanic]] budget runs server-only via [[Network Air Budget]].

## Owner predicts, spectators interpolate (refined 2026-06-04)
The **owner** runs client-side prediction for instant controls; **spectated** players sync via a server-auth NetworkTransform (state forwarding off) with kinematic bodies, following the host's real motion. Predicting spectators replayed them *without* the host-only hose force → stutter (worst when deflated). This split is what made the netcode feel smooth. See [[Network Player Controller]] / [[Setup and Gotchas]].

## Locked decisions
- [[Decision - Steam-first behind wrapper]]
- [[Decision - FishNet host-authoritative]]
- [[Decision - 2P now 4P later]]

## Build it via
[[Networking Spike]] — a thin vertical proof before porting the [[Fan Trap]].

## Connections
[[Networking Spike]] · [[Steam Integration]] · [[FishNet Netcode]] · [[Platform Abstraction]] · [[Voice Chat - Dissonance]] · [[Tech Stack and Versions]]
