---
status: decision
tags: [decision, networking]
date: 2026-06-02
---

# Decision · FishNet, host-authoritative (prediction later)

**Fork:** How precise must the moment-to-moment feel be? (Answer at decision time: *not sure yet.*)

**Chosen:** [[FishNet Netcode]] — because it runs simple host-authority **now** AND supports client-side prediction **later** without swapping frameworks. The safest hedge for a physics game when the latency-feel is unknown.

## Rejected alternatives
- **Mirror** — simpler, but would lock in the no-prediction path; adding prediction later would mean switching frameworks (costly). Only the right call if the feel were *known* to be chill.
- **Photon Fusion** — seriously evaluated 2026-06-02 (see [[Networking Solutions - Comparison]]). Strong option, but rejected for *this* game: it's 2P (host-disconnect resilience ≈ worthless; the mode that gives it, Shared, fights the coupled physics), solo + pre-revenue (FishNet+SDR is $0/no-ceiling/self-owned vs Photon's cloud dependency + CCU cliff), and the Steam bug that prompted the look was a Steamworks-layer issue Fusion wouldn't fix.

## Re-confirmed (2026-06-02)
Decision held after a full Fusion comparison. Revisit Fusion (Host Mode) only if: validates online **and** genuinely goes 3-4P drop-in co-op needing resilience **and** wants matchmaking/ecosystem **and** accepts cloud dependency.

## Consequences
- Spike runs **host-authoritative, no prediction on purpose** — to *measure* the unpredicted feel before deciding
- The [[Networking Spike]] two-PC test produces that measurement → then decide whether to enable FishNet prediction
- Model: listen-server over SDR — see [[Networking Architecture]]

## Connections
[[FishNet Netcode]] · [[Networking Architecture]] · [[Networking Spike]] · [[Networking Solutions - Comparison]] · [[Decision - Steam-first behind wrapper]] · [[Decision - 2P now 4P later]]
