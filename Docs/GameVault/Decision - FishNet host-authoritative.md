---
status: decision
tags: [decision, networking]
date: 2026-06-02
---

# Decision · FishNet, host-authoritative (prediction later)

**Fork:** How precise must the moment-to-moment feel be? (Answer at decision time: *not sure yet.*)

**Chosen:** [[FishNet Netcode]] — because it runs simple host-authority **now** AND supports client-side prediction **later** without swapping frameworks. The safest hedge for a physics game when the latency-feel is unknown.

## Rejected alternative
- **Mirror** — simpler, but would lock in the no-prediction path; adding prediction later would mean switching frameworks (costly). Only the right call if the feel were *known* to be chill.

## Consequences
- Spike runs **host-authoritative, no prediction on purpose** — to *measure* the unpredicted feel before deciding
- The [[Networking Spike]] two-PC test produces that measurement → then decide whether to enable FishNet prediction
- Model: listen-server over SDR — see [[Networking Architecture]]

## Connections
[[FishNet Netcode]] · [[Networking Architecture]] · [[Networking Spike]] · [[Decision - Steam-first behind wrapper]] · [[Decision - 2P now 4P later]]
