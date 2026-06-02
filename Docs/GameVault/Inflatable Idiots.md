---
status: moc
tags: [home, moc]
---

# 🎈 Inflatable Idiots

> Map of Content (MOC) — the home node. Open this vault's **Graph View** to see how every subsystem connects.

A 2-player physics co-op game. Two inflatable characters share **one** air budget and are joined by a permanent **hose**. Inflated = light/floaty; deflated = heavy/anchored. Levels force the pair to constantly renegotiate who gets the air. Target platform: **Steam** (online co-op).

## The pitch in one line
Read the [[Core Hypothesis]]. Validated by a real playtest — see [[MVP Scope and Validation]].

## Core mechanics (implemented)
- [[Air-Share Mechanic]] — the zero-sum shared air budget (the differentiator)
- [[Inflation State]] — how budget drives mass / gravity / scale
- [[Air Hose]] — the permanent tether between players
- [[Player Character]] — movement, jump, ground check
- [[AirPair]] — owns the local 2-player budget
- [[Stalemate Feedback]] — juice when both pull at once
- [[Input System]] — actions, schemes, keyboard fallback
- [[Camera Shaker]]

## The gym level (implemented)
- [[Fan Trap]] — the single validation level
- [[Fan Zone]] · [[Goal Zone]] · [[Rising Hazard]] · [[Run Controller]]
- [[Test Scene Builder]] · [[Windows Build]]

## Online play (in progress — this is the current frontier)
- [[Networking Architecture]] — the master plan
- [[Networking Spike]] — the current proof-of-concept (solo host works; 2-PC next)
- [[Steam Integration]] · [[FishNet Netcode]] · [[Platform Abstraction]]
- [[Network Player Controller]] · [[Network Air Budget]] · [[Network Hose]] · [[Network Spawner]]
- [[Voice Chat - Dissonance]] (deferred)

## Decisions & reference
- [[Decision - Steam-first behind wrapper]] · [[Decision - FishNet host-authoritative]] · [[Decision - 2P now 4P later]]
- [[Tech Stack and Versions]] · [[Setup and Gotchas]]
- [[Roadmap]] · [[Parked Ideas]]
