# Prowl Agent Entry

Read this file before making changes.

## Scope

| Area | Path | Default mode |
| --- | --- | --- |
| Engine runtime | Prowl.Runtime/ | writable |
| Editor | Prowl.Editor/ | writable |
| Desktop player | Prowl.Players/Prowl.Desktop/ | writable |
| Runtime tests | Prowl.Runtime.Test/ | writable |
| External dependencies | External/ | read-only |
| Build artifacts | Build/ | read-only |

Only edit External/ when the task explicitly asks for third-party dependency changes.

## Verify Loop

Use the same command sequence as CI:

```bash
dotnet restore
dotnet build --no-restore
dotnet test
```

## Context Loading Order

Load these files in order before starting any task:

1. This file (AGENTS.md)
2. [architecture.md](architecture.md) — module map and startup chain
3. [plan.md](plan.md) — current milestone and task priorities
4. The relevant [tasks/*.md](tasks/) file for the affected area
5. The specific [backlog/{id}.md](backlog/) file for the task being worked on
6. [test-matrix.md](test-matrix.md) — verification levels and coverage gaps

## Task Routing

- Rendering and SRP work: [tasks/rendering.md](tasks/rendering.md)
- Editor workflow and tooling: [tasks/editor.md](tasks/editor.md)
- Runtime systems and core loop: [tasks/runtime.md](tasks/runtime.md)
- Build and release commands: [workflows.md](workflows.md)
- Module map and startup chain: [architecture.md](architecture.md)
- Development plan and priorities: [plan.md](plan.md)
- Backlog items (requirements): [backlog/](backlog/)
- Test coverage and verification: [test-matrix.md](test-matrix.md)

## Change Output Format

Every change summary should include:

1. What changed (files and symbols)
2. Why it changed (issue/spec reference)
3. How it was verified (tests/commands)
4. Risks or follow-up notes

## Guardrails

- Keep changes minimal and scoped.
- Prefer editing existing files over broad refactors.
- Do not move project structure unless requested.
- If unsure about ownership between runtime/editor/player, check architecture.md first.
- When a task fails or throws an exception, append one entry to this file under `## Failure Log` before continuing.

## Task Lifecycle Protocol

Follow these six steps for every backlog item. Do not skip steps.

### 1. Pick

- Read [plan.md](plan.md) → find the highest-priority item with `status=ready` and all `depends-on` items `done`.
- If no item is `ready`, report to the user and stop.
- If the user specifies a task directly, use that instead.

### 2. Spec

- Read the backlog file: `backlog/{id}.md`.
- Verify that **Acceptance Criteria** are specific and testable. If not, ask the user to clarify before proceeding.
- Verify that **Scope IN/OUT** boundaries are clear.
- Fill in **Design Notes** if empty (technical approach, key decisions).

### 3. Plan

- List the files to modify and the expected behavior changes.
- Write a session note to `/memories/session/` with the execution plan.
- Update the backlog file: `status: in-progress`, set `owner`.
- Update [plan.md](plan.md) table row to match.

### 4. Implement

- Follow all rules in [Guardrails](#guardrails) above.
- Keep changes minimal and scoped to one logical unit.
- Prefer editing existing files over creating new ones.

### 5. Verify

- Run verification levels from [test-matrix.md](test-matrix.md) in order: L0 → L1 → L2.
- For rendering changes, also run L3a/L3b/L3c as specified in the backlog's Test Plan.
- Update the backlog file: `status: in-review`. Update [plan.md](plan.md) table row to match.
- **If any level fails:** record in Failure Log → diagnose → set status back to `in-progress` → return to Step 4.
- **If new tests are needed:** add them, then update the test-matrix.md coverage table.

### 6. Complete

- Update the backlog file: `status: done`.
- Move the row in [plan.md](plan.md) to the **Done** section with completion date.
- Output a change summary using the [Change Output Format](#change-output-format).
- If new issues were discovered during implementation, create new backlog files with `status: spec`.

## Failure Log

Record each failure or exception as one line using this format:

- YYYY-MM-DD | task | error/exception | root cause (or hypothesis) | action taken
- 2025-07-15 | SRP P0-P3 fix execution | NullReferenceException in RenderPipelineAssetTests (3 failures) | `_rendererLock`, `_contextLock`, and `_cameraContexts` fields not initialized inline — test bypasses `OnEnable` so fields stayed null | Added `= new object()` / `= new()` inline initializers to all three fields in RenderPipelineAsset.cs
