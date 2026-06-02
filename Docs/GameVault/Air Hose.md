---
status: implemented
tags: [mechanic, core]
---

# Air Hose

The permanent tether between the two players. Makes the shared resource ([[Air-Share Mechanic]]) tangible and acts as the kite-string in the [[Fan Trap]].

## Implementation (MVP — simplest possible)
- `LineRenderer` + a runtime chain of **6** featherweight `Rigidbody2D` nodes linked by soft `SpringJoint2D`
- NOT a Verlet rope — a visual tether + soft length constraint
- `slack` = 1.25 (rest length as a multiple of endpoint distance) so it sags
- Clean **sim/render split**: `BuildRope()` (sim) vs `UpdateLine()` (render) — this seam is what makes network render-only mode possible

## ⚠️ Hard-won fix (center anchors)
Springs must anchor at **body centers** (`anchor = connectedAnchor = Vector2.zero`). The players jitter their transform during a [[Stalemate Feedback|stalemate]]; an off-center auto-anchor swung in world space each frame and pumped energy into the rope so it stretched forever. **Rule: any component that visually jitters/scales a body hosting a Joint2D must use center anchors.**

## Where it lives
- `Assets/InflatableIdiots/Scripts/Hose/HoseController.cs`
- Online: host simulates it, clients render from replicated node positions — see [[Network Hose]] (uses additive `RenderOnly` / `Managed` / `RenderFromPoints` / `SnapshotChain` hooks)

## Connections
[[Air-Share Mechanic]] · [[Player Character]] · [[Fan Trap]] · [[Stalemate Feedback]] · [[Network Hose]]
