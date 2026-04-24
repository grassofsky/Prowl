# Workflows

## Initial Setup

```bash
git submodule update --init --recursive
```

Windows helper:

```bash
UpdateSubmodules.bat
```

## Build and Test

```bash
dotnet restore
dotnet build --no-restore
dotnet test
```

## Tiered Verification

Use these commands for incremental validation. See [test-matrix.md](test-matrix.md) for full definitions.

**L0 — Compile only:**

```bash
dotnet build --no-restore
```

**L1 — Module-scoped tests:**

```bash
dotnet test --filter "FullyQualifiedName~ModuleName"
```

Replace `ModuleName` with the relevant class or namespace (e.g. `Shadow`, `ScriptableRenderContext`, `Color`).

**L2 — Full test suite:**

```bash
dotnet test
```

**L3 — GPU integration tests (requires GPU):**

```bash
dotnet test --filter "Category=GPU"
```

GPU tests use headless OpenGL/Vulkan via `GpuDeviceFixture`. They are skipped automatically on machines without a supported GPU backend.

## Run Editor

Use the root guide in Run.md for launching the editor in debug mode.

## Publish (reference)

```bash
dotnet publish ./Prowl.Editor/Prowl.Editor.csproj --configuration Release --output ./publish/<runtime>/Prowl --runtime <runtime> --framework net9.0
```

Supported runtime examples:

- win-x64
- linux-x64
- osx-x64

## Output Paths

- Runtime debug output: Build/Runtime/Debug/
- Editor debug output: Build/Editor/Debug/
