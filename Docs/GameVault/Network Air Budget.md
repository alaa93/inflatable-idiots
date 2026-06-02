---
status: implemented
tags: [networking, fishnet]
---

# Network Air Budget

`NetworkAirBudget` — the **server-authoritative** online replacement for [[AirPair]]. Re-implements the [[Air-Share Mechanic]] formula on the server and publishes the result.

## Behaviour
- Server integrates `flow = (t0 - t1) * transferSpeed * dt` from both [[Network Player Controller]]s' relayed trigger values; clamps `_p1Budget` to [0.1, 0.9]
- `readonly SyncVar<float> _p1Budget` replicates to all clients
- Every peer wires each [[Player Character]]'s `BudgetSource` to read this by slot (slot 0 = budget, slot 1 = 1 − budget) → both bodies inflate/deflate identically on both screens ([[Inflation State]])
- Lives on the same spawned object as [[Network Hose]] (the "link"), spawned by [[Network Spawner]] once both players exist

## Why not reuse AirPair
[[AirPair]] isn't a NetworkBehaviour and assumes a single shared keyboard.

## Where it lives
- `Assets/InflatableIdiots/Scripts/Net/NetworkAirBudget.cs`

## Connections
[[Air-Share Mechanic]] · [[AirPair]] · [[Inflation State]] · [[Network Player Controller]] · [[Network Hose]] · [[Network Spawner]] · [[FishNet Netcode]]
