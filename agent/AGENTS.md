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

## Task Routing

- Rendering and SRP work: [tasks/rendering.md](tasks/rendering.md)
- Editor workflow and tooling: [tasks/editor.md](tasks/editor.md)
- Runtime systems and core loop: [tasks/runtime.md](tasks/runtime.md)
- Build and release commands: [workflows.md](workflows.md)
- Module map and startup chain: [architecture.md](architecture.md)

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

## Failure Log

Record each failure or exception as one line using this format:

- YYYY-MM-DD | task | error/exception | root cause (or hypothesis) | action taken
- 2025-07-15 | SRP P0-P3 fix execution | NullReferenceException in RenderPipelineAssetTests (3 failures) | `_rendererLock`, `_contextLock`, and `_cameraContexts` fields not initialized inline — test bypasses `OnEnable` so fields stayed null | Added `= new object()` / `= new()` inline initializers to all three fields in RenderPipelineAsset.cs
