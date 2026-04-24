---
id: "rendering-005"
title: "Lightmaps and Light Probes"
area: "rendering"
priority: P3
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: v0.5
owner: ""
depends-on: []
created: 2026-04-22
github-issue: ""
---

## Problem

No baked lighting solution exists. Static scenes cannot benefit from pre-computed high-quality GI, requiring all lighting to be computed in real-time.

## Acceptance Criteria

- [ ] Lightmap UV generation (or import from asset pipeline)
- [ ] Lightmap baking (CPU-based path tracer or integration with external baker)
- [ ] Light probe placement and spherical harmonics storage
- [ ] Runtime sampling of lightmaps and probes in shaders
- [ ] Editor UI for bake controls and probe visualization

## Scope

**IN:** Lightmap baker, light probe system, shader integration, editor bake UI

**OUT:** Realtime GI (separate backlog item), reflection probes

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L2 | Full test suite | `dotnet test` |
| L3a | Baked scene loads and renders | Editor play mode |
| L3b | Lightmapped surface brightness > ambient-only | GPU readback comparison |

## Notes

- Can be done independently of realtime GI.
- Lightmap UV generation may require mesh processing utilities.
