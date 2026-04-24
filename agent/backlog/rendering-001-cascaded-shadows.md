---
id: "rendering-001"
title: "Cascaded Shadow Mapping"
area: "rendering"
priority: P1
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: v0.5
owner: ""
depends-on: []
created: 2026-04-22
github-issue: ""
---

## Problem

Current shadow implementation uses a single shadow map for the main directional light. At medium-to-far distances shadow resolution degrades severely, producing visible aliasing and blocky artifacts.

## Acceptance Criteria

- [ ] Support 2–4 cascade splits for the main directional light
- [ ] Split distances configurable in PipelineSettings
- [ ] Shadow atlas layout (single texture, sub-viewports) instead of multiple textures
- [ ] Smooth transition between cascades (blend band)
- [ ] Existing MainLightShadowPass unit tests do not regress
- [ ] New cascade-specific unit tests ≥ 3

## Scope

**IN:** MainLightShadowPass, ForwardRenderer, PipelineSettings, shadow-related shaders

**OUT:** Point/spot light shadows, soft shadow filtering algorithms

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Shadow pass tests | `dotnet test --filter "FullyQualifiedName~Shadow"` |
| L2 | Full test suite | `dotnet test` |
| L3b | Each cascade region has valid depth values | GPU readback, depth continuity check |
| L3c | Shadow quality visual regression | Golden image comparison vs reference scene |

## Notes

- MainLightShadowPass in ForwardRenderer.cs is the primary entry point.
- Must verify both editor scene view and runtime camera paths.
