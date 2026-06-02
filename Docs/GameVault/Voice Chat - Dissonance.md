---
status: deferred
tags: [networking, voice]
---

# Voice Chat - Dissonance

Planned voice solution. Dissonance is transport-based (rides the netcode), so it's **store-agnostic by construction** — fits the [[Platform Abstraction]] / [[Decision - Steam-first behind wrapper]] plan. Chosen over Steam's raw voice API (which is Steam-only + low-level) and EOS RTC (only relevant if we ever standardize on EOS).

Voice is the home of the killer "GIVE ME 40% NOW!" moment of the [[Air-Share Mechanic]].

## ⚠️ Current reality (corrected 2026-06-02)
**NOT installed.** No `DISSONANCE_ENABLED` define is set (an older note wrongly said it was). When voice work begins:
1. Import Dissonance + its FishNet integration (match [[FishNet Netcode]] version)
2. Add `DISSONANCE_ENABLED` per build target (the flag is per-platform in Unity; missing it silently disables voice in that build)

## Sequencing
Out of scope for the [[Networking Spike]] (use Discord during testing). It's an add-on *after* a working transport — see [[Roadmap]].

## Connections
[[Air-Share Mechanic]] · [[Networking Architecture]] · [[Platform Abstraction]] · [[FishNet Netcode]] · [[Roadmap]]
