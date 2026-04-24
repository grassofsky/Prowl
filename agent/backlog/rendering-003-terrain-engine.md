---
id: "rendering-003"
title: "Terrain Engine"
area: "rendering"
priority: P2
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: v0.5
owner: ""
depends-on: []
created: 2026-04-22
github-issue: "#38"
---

## Problem

No terrain system exists. Open-world or outdoor scenes require heightmap-based terrain with LOD, texture splatting, and efficient culling.

## Acceptance Criteria

- [ ] Terrain component with heightmap import (16-bit RAW or PNG)
- [ ] Chunked LOD rendering with smooth transitions
- [ ] Multi-layer texture splatting (≥ 4 layers)
- [ ] Terrain collider integration with physics
- [ ] Editor terrain sculpting tools (raise, lower, smooth, paint)

## Scope

**IN:** New Prowl.Runtime/Components/Terrain.cs, terrain shaders, editor terrain tools

**OUT:** Vegetation/tree placement, terrain streaming, procedural generation

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Terrain mesh generation unit tests | `dotnet test --filter "FullyQualifiedName~Terrain"` |
| L2 | Full test suite | `dotnet test` |
| L3a | Terrain scene loads without crash | Editor play mode |
| L3b | Terrain pixels visible, not flat black | GPU readback sanity |

## Notes

- GitHub issue: https://github.com/ProwlEngine/Prowl/issues/38
- Physics integration depends on Jitter Physics 2 terrain support.
