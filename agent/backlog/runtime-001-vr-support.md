---
id: "runtime-001"
title: "VR Support"
area: "runtime"
priority: P3
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: v0.6
owner: ""
depends-on: []
created: 2026-04-22
github-issue: ""
---

## Problem

No VR/XR support exists. Prowl cannot target VR headsets, blocking an entire category of applications.

## Acceptance Criteria

- [ ] OpenXR integration for HMD tracking and controller input
- [ ] Stereo rendering path (two-eye rendering with correct projection)
- [ ] VR camera rig component
- [ ] Basic hand/controller input mapping
- [ ] Works with at least one headset family (Quest via Link, or SteamVR)

## Scope

**IN:** OpenXR binding, stereo rendering in SRP, VR camera component, input mapping

**OUT:** Hand tracking, eye tracking, AR/MR passthrough, haptics

## Design Notes

_To be filled during spec→ready transition. Evaluate OpenXR .NET bindings (Silk.NET.OpenXR or similar)._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | VR camera projection math tests | `dotnet test --filter "FullyQualifiedName~VR"` |
| L2 | Full test suite | `dotnet test` |
| L3a | VR scene initializes without crash (headset optional) | Headless or mock XR runtime |

## Notes

- Stereo rendering may require changes to Camera.cs and ForwardRenderer.
- Veldrid supports multiple swapchains which can be used for left/right eye.
- This is a large feature; consider phased approach (tracking first, then rendering, then input).
