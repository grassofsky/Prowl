---
id: "editor-003"
title: "2D Support"
area: "editor"
priority: P2
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: v0.6
owner: ""
depends-on: []
created: 2026-04-22
github-issue: ""
---

## Problem

Prowl is currently 3D-only. 2D game development requires sprite rendering, 2D physics, tilemaps, and appropriate editor tooling.

## Acceptance Criteria

- [ ] SpriteRenderer component with atlas/sheet support
- [ ] 2D camera mode (orthographic with pixel-perfect option)
- [ ] 2D physics integration (or thin wrapper over existing 3D physics)
- [ ] Tilemap component and editor painting tool
- [ ] Sprite asset importer with slicing

## Scope

**IN:** 2D rendering components, 2D camera mode, sprite importer, tilemap system

**OUT:** 2D animation (depends on editor-001), 2D lighting, 2D skeletal animation

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Sprite rendering unit tests | `dotnet test --filter "FullyQualifiedName~Sprite"` |
| L2 | Full test suite | `dotnet test` |
| L3a | 2D scene loads and renders sprites | Editor play mode |
| L3b | Sprite pixels visible at expected positions | GPU readback position check |

## Notes

- Should integrate with existing SRP pipeline (new 2D render pass or reuse transparent pass).
- Orthographic camera mode may already be partially supported.
