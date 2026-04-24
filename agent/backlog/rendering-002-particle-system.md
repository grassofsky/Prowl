---
id: "rendering-002"
title: "Particle System"
area: "rendering"
priority: P2
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: v0.5
owner: ""
depends-on: []
created: 2026-04-22
github-issue: "#37"
---

## Problem

Prowl lacks a particle system. Many visual effects (fire, smoke, sparks, trails) cannot be created without one.

## Acceptance Criteria

- [ ] Runtime ParticleSystem component with emitter/simulation/renderer
- [ ] Support basic emission shapes (point, sphere, cone, box)
- [ ] Configurable lifetime, velocity, size-over-life, color-over-life curves
- [ ] GPU-instanced rendering for particle billboards
- [ ] Editor inspector for particle system properties
- [ ] At least one demo particle effect works in editor play mode

## Scope

**IN:** New Prowl.Runtime/Components/ParticleSystem.cs, new render pass or integration with existing TransparentRenderPass, editor UI

**OUT:** GPU simulation (compute-based), particle collision, sub-emitters

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Particle simulation unit tests | `dotnet test --filter "FullyQualifiedName~Particle"` |
| L2 | Full test suite | `dotnet test` |
| L3a | Scene with particles loads without crash | Editor play mode |
| L3b | Particle pixels visible in frame | GPU readback, non-black region check |

## Notes

- GitHub issue: https://github.com/ProwlEngine/Prowl/issues/37
- Consider AnimationCurve.cs for size/color over lifetime.
