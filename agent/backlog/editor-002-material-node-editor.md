---
id: "editor-002"
title: "Material Node Editor"
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

Materials can only be authored via code or direct property editing. A visual node-based shader/material editor would greatly improve the workflow for artists and non-programmers.

## Acceptance Criteria

- [ ] Node graph editor UI in Prowl.Editor
- [ ] Standard material nodes (texture sample, math ops, UV, normals, PBR output)
- [ ] Graph compiles to shader code compatible with Prowl's rendering pipeline
- [ ] Live preview of material in editor
- [ ] Save/load material graphs as assets

## Scope

**IN:** Editor node graph UI, shader code generation, material asset integration

**OUT:** Custom function nodes, compute shader graphs, VFX graph

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Node graph serialization tests | `dotnet test --filter "FullyQualifiedName~Material"` |
| L2 | Full test suite | `dotnet test` |
| L3a | Node editor opens and creates a basic material | Manual verification |
| L3b | Generated material renders correctly on a mesh | GPU readback sanity |

## Notes

- Prowl's custom UI library should be sufficient for the node editor.
- Shader code generation must target Prowl's shader format (Veldrid-compatible).
