---
status: decision
tags: [decision, networking]
date: 2026-06-02
---

# Decision · Steam-first behind a thin wrapper

**Fork:** How far beyond Steam should the game reach?

**Chosen:** Steam first, **maybe Epic later** → build native Steamworks now, but route every platform call through a thin [[Platform Abstraction|IPlatform wrapper]] so adding Epic later is one new class, not a refactor.

## Rejected alternatives
- **Steam-only forever** — cheapest now, but a painful retrofit if we ever change our mind.
- **EOS as universal layer** — lowest total multi-store effort, but only worth it if multi-store + mobile were a *firm* goal (they're not yet). Slightly less native feel; ties us to Epic.

## Consequences
- [[Steam Integration]] implements [[Platform Abstraction]]
- [[Voice Chat - Dissonance]] chosen partly because it's transport-based / store-agnostic, consistent with this
- Keeps the Epic door open at near-zero cost today

## Connections
[[Networking Architecture]] · [[Platform Abstraction]] · [[Steam Integration]] · [[Decision - FishNet host-authoritative]] · [[Decision - 2P now 4P later]]
