---
status: reference
tags: [meta]
---

# Vault Guide

How to use this Obsidian vault.

## Open it
Obsidian → **Open folder as vault** → select `Docs/GameVault`. Start at [[Inflatable Idiots]] (the home MOC).

## See the network
Open **Graph View** (`Ctrl+G`, or the constellation icon). Every note links to its related subsystems, so the graph *is* the architecture map. Use **Local Graph** on any note to see just its immediate connections.

## Color by status (recommended)
Each note has a `status` in its frontmatter. In Graph View → **Settings (gear) → Groups → New group**, add queries to color nodes, e.g.:
- `tag:#core` or `tag:#mechanic` → one color (built mechanics)
- `tag:#networking` → another (online work)
- `["status":"deferred"]` / `["status":"decision"]` / `["status":"parked"]` → distinct colors

`status` values used: `implemented`, `validated`, `in-progress`, `planning`, `decision`, `deferred`, `parked`, `reference`, `moc`.

## Status legend
- ✅ **implemented / validated** — built and (locally) working
- ◑ **in-progress** — the [[Networking Spike]] frontier
- 🔭 **planning / deferred / parked** — see [[Roadmap]] and [[Parked Ideas]]

## Keeping it true
This vault is a snapshot (2026-06-02). When code changes, update the matching note. The authoritative narrative for next steps is [[Roadmap]]; setup pitfalls live in [[Setup and Gotchas]].

## Connections
[[Inflatable Idiots]] · [[Roadmap]] · [[Setup and Gotchas]]
