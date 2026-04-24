---
id: "editor-001"
title: "Animation Tools"
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

No animation editing tools exist in the editor. Users cannot create, edit, or preview animation clips for skeletal or property animations.

## Acceptance Criteria

- [ ] Animation clip asset creation and editing in editor
- [ ] Timeline/dopesheet view for keyframe editing
- [ ] Support for transform and property animation curves
- [ ] Preview playback in scene view
- [ ] Integration with existing AnimationCurve.cs

## Scope

**IN:** Editor animation window, animation clip asset format, preview system

**OUT:** Animation state machine / blend tree editor (future work), IK system

## Design Notes

_To be filled during spec→ready transition._

## Test Plan

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Animation curve evaluation tests | `dotnet test --filter "FullyQualifiedName~Animation"` |
| L2 | Full test suite | `dotnet test` |
| L3a | Editor animation window opens without crash | Manual verification |

## Notes

- AnimationCurve.cs already exists in runtime; reuse for keyframe interpolation.
- Editor-only code should stay in Prowl.Editor/.
