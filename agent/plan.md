# Development Plan

## Status Lifecycle

```
spec → ready → in-progress → in-review → done
                    ↓
                 blocked
```

| Status | Meaning | Who moves it here |
|--------|---------|-------------------|
| spec | Requirements being written, AC incomplete | Creator |
| ready | AC complete, design reviewed, can be picked up | Human or architect agent |
| in-progress | Actively being worked on | Developer or agent (Step 3 of Lifecycle) |
| in-review | Code complete, awaiting review/verification | Developer or agent (Step 5 of Lifecycle) |
| done | Merged, verified, all AC checked | Reviewer or agent (Step 6 of Lifecycle) |
| blocked | Cannot proceed, dependency or external issue | Anyone who identifies the blocker |

## Priority Definitions

| Level | Meaning | Agent auto-pick? |
|-------|---------|-----------------|
| P0 | Blocks other features or causes crashes | Yes — highest |
| P1 | Must complete in target release | Yes |
| P2 | Should complete in target release | Yes |
| P3 | Nice-to-have, do when capacity allows | Yes — lowest |

**Agent pick rule:** Select the highest-priority item where `status=ready` and all `depends-on` items are `done`. If multiple items share the same priority, pick the one created earliest.

## Release Versions

| Version | Theme | Target Date | Status |
|---------|-------|-------------|--------|
| v0.5 | Rendering Foundation | TBD | planning |
| v0.6 | Editor & Platform | TBD | future |
| backlog | Unscheduled | — | — |

## Roadmap Board

### v0.5 — Rendering Foundation

| ID | Title | Priority | Status | Owner | Release | Deps |
|----|-------|----------|--------|-------|---------|------|
| rendering-001 | Cascaded Shadow Mapping | P1 | spec | — | v0.5 | — |
| rendering-002 | Particle System | P2 | spec | — | v0.5 | — |
| rendering-003 | Terrain Engine | P2 | spec | — | v0.5 | — |
| rendering-004 | Realtime Global Illumination | P3 | spec | — | v0.5 | — |
| rendering-005 | Lightmaps and Light Probes | P3 | spec | — | v0.5 | — |

### v0.6 — Editor & Platform

| ID | Title | Priority | Status | Owner | Release | Deps |
|----|-------|----------|--------|-------|---------|------|
| editor-001 | Animation Tools | P2 | spec | — | v0.6 | — |
| editor-002 | Material Node Editor | P2 | spec | — | v0.6 | — |
| editor-003 | 2D Support | P2 | spec | — | v0.6 | — |
| runtime-001 | VR Support | P3 | spec | — | v0.6 | — |

### Backlog (unscheduled)

| ID | Title | Priority | Status | Owner | Release | Deps |
|----|-------|----------|--------|-------|---------|------|
| _(empty)_ | | | | | | |

## Done

| ID | Title | Release | Completed |
|----|-------|---------|-----------|
| _(none yet)_ | | | |

## Status Summary

Use `/sprint-review` prompt or manually count:

| Status | v0.5 | v0.6 | Backlog |
|--------|------|------|---------|
| spec | 5 | 4 | 0 |
| ready | 0 | 0 | 0 |
| in-progress | 0 | 0 | 0 |
| in-review | 0 | 0 | 0 |
| done | 0 | 0 | 0 |
| blocked | 0 | 0 | 0 |

## How to Update

### Status transitions
- When a backlog item's `status` changes, update both the backlog file's YAML frontmatter AND the matching row in this table.
- Verify the status transition is valid per the lifecycle above.

### Release planning
- New items default to `Release: backlog` (unscheduled).
- To schedule an item, move its row from Backlog to the target release table and set `Release: vX.Y` in both this file and the backlog YAML.
- To reschedule, move the row between release tables and update the backlog file.

### Completing items
- When moving an item to `done`, move it from its release table to the **Done** section with completion date.
- Update the Status Summary counts.

### New releases
- To create a new release, add a row to **Release Versions** and a new section under **Roadmap Board**.
- Items from Backlog or deferred from prior releases can be moved in.
