---
id: "{area}-{number}"
title: ""
area: ""                # rendering | editor | runtime | player
priority: P2            # P0(blocker) P1(must) P2(should) P3(nice-to-have)
status: spec            # spec → ready → in-progress → in-review → done | blocked
release: backlog        # target version: "v0.5", "v0.6", or "backlog" (unscheduled)
owner: ""               # person name, "agent", or empty
depends-on: []          # list of backlog IDs this item depends on
created: YYYY-MM-DD
github-issue: ""        # optional GitHub issue number, e.g. "#123"
---

## Problem

_What is broken or missing? One paragraph._

## Acceptance Criteria

- [ ] AC1: (specific, verifiable condition)
- [ ] AC2:
- [ ] AC3:

## Scope

**IN:** (files, modules, subsystems this task will touch)

**OUT:** (explicitly excluded areas)

## Design Notes

_Technical approach, alternatives considered, key decisions. Can be filled later._

## Test Plan

Refer to [test-matrix.md](../test-matrix.md) for level definitions.

| Level | What to verify | How |
|-------|---------------|-----|
| L0 | Compiles | `dotnet build` |
| L1 | Unit tests for changed module | `dotnet test --filter "FullyQualifiedName~ModuleName"` |
| L2 | Full test suite | `dotnet test` |
| L3a | (if applicable) Scene loads without crash | Manual or headless run |
| L3b | (if applicable) Rendering sanity assertions | GPU readback + statistical checks |
| L3c | (if applicable) Visual regression | Golden image RMSE comparison |

## Notes

_Risks, follow-up items, open questions._
