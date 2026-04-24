---
id: "rendering-004"
title: "Realtime Global Illumination"
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

No global illumination solution exists. Scenes lack indirect lighting, color bleeding, and ambient occlusion, resulting in flat, unrealistic visuals.

## Acceptance Criteria

- [ ] Basic indirect diffuse lighting (e.g. light probe grid or DDGI)
- [ ] Dynamic updates when lights or geometry change
- [ ] Configurable quality levels (probe density, update rate)
- [ ] Minimal per-frame cost on mid-range GPUs

## Scope

**IN:** New GI system in Prowl.Runtime/Rendering/, integration with ForwardRenderer, GI-related shaders

**OUT:** Baked lightmaps (separate backlog item), specular GI, volumetric lighting

## Design Notes

_To be filled during spec→ready transition. Research DDGI, LPV, or screen-space approaches._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L2 | Full test suite | `dotnet test` |
| L3b | Indirect light visible on shadowed surfaces | GPU readback, shadowed area brightness > threshold |
| L3c | Visual regression against reference scene | Golden image RMSE |

## Notes

- This is a large feature; may need to be broken into sub-tasks.
- Consider starting with a simpler approach (SSAO + ambient probes) before full DDGI.
