# Runtime Task Routing

## Core Files

- Prowl.Runtime/Application.cs
- Prowl.Runtime/SceneManager.cs
- Prowl.Runtime/IAssetProvider.cs
- Prowl.Runtime/AssemblyManager.cs

## Supporting Areas

- Prowl.Runtime/Components/
- Prowl.Runtime/GameObject/
- Prowl.Runtime/Rendering/
- Prowl.Runtime/Math/
- Prowl.Runtime/Audio/

## Typical Task Flow

1. Locate subsystem owner file first.
2. Apply minimal scoped changes.
3. Add or update tests in Prowl.Runtime.Test when behavior is testable.
4. Validate with restore/build/test loop.

## Boundary Rules

- Runtime owns abstractions.
- Editor/Desktop own environment-specific implementations.
- Avoid cross-layer references that break runtime isolation.
