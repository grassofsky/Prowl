# Test Matrix

## Verification Levels

| Level | Name | What it verifies | Command | When to run |
|-------|------|-----------------|---------|-------------|
| L0 | Compile | Code compiles without errors | `dotnet build --no-restore` | Every change |
| L1 | Module tests | Unit tests for the changed module | `dotnet test --filter "FullyQualifiedName~ModuleName"` | Every change |
| L2 | Full tests | All unit tests pass | `dotnet test` | Before commit |
| L3a | Smoke test | Scene loads, runs N frames, no exceptions | Headless run or editor launch | Rendering/scene/asset changes |
| L3b | Render sanity | Frame buffer is non-trivial (not all-black/all-white, variance > threshold, depth continuity) | GPU readback + statistical assertions | Rendering pipeline changes |
| L3c | Visual regression | Rendered frame matches golden image within RMSE ≤ 2.0, diff pixels ≤ 3% | GPU readback + RMSE comparison against `Prowl.Runtime.Test/GPU/GoldenImages/` | Rendering pipeline changes |

### L3b Detail: Render Sanity Assertions

After rendering a test scene to an off-screen `RenderTexture`, read back pixels via `Texture2D.CopyData<Color32>()` and assert:

1. **Not all-black:** ≥ 5% of pixels have brightness > 5
2. **Not all-white:** ≥ 5% of pixels have brightness < 250
3. **Color variance:** Per-channel variance > 100 (indicates geometry/lighting produced shading variation)
4. **Depth continuity** (if depth buffer readable): Near objects have smaller depth values than far objects

### L3c Detail: Visual Regression Protocol

1. Render a deterministic test scene at fixed resolution (e.g. 256×256)
2. Read back pixels via `Texture2D.CopyData<Color32>()`
3. Compare against golden image stored in `Prowl.Runtime.Test/GPU/GoldenImages/`
4. Compute RMSE: `sqrt(sum((actual[i] - expected[i])^2) / (N * 3))` per RGB channel
5. Pass if RMSE ≤ 2.0 AND diff pixels (any channel delta > 10) ≤ 3%
6. On first run or intentional change: record new golden image, requires human approval

Golden images are committed to the repository. Updating them requires explicit human review.

### GPU Test Infrastructure

- Fixture: `Prowl.Runtime.Test/GPU/GpuDeviceFixture.cs` — headless OpenGL/Vulkan device
- xUnit trait for GPU tests: `[Trait("Category", "GPU")]`
- Filter command: `dotnet test --filter "Category=GPU"`

## Module Coverage Matrix

| Module | Area | Test Files | Current Level | Coverage Gaps |
|--------|------|-----------|---------------|---------------|
| Bool3 | Math | `Bool3Tests.cs` | L2 | — |
| Boolean32Matrix | Math | `Boolean32MatrixTests.cs` | L2 | — |
| Color | Core | `ColorTests.cs` | L2 | — |
| ScriptableRenderContext | SRP | `SRP/ScriptableRenderContextTests.cs` | L2 | — |
| ShaderTagId | SRP | `SRP/ShaderTagIdTests.cs` | L2 | — |
| DrawingSettings | SRP | `SRP/DrawingSettingsTests.cs` | L2 | — |
| ClearFlags | SRP | `SRP/ClearFlagTests.cs` | L2 | — |
| RenderPassEvent | SRP | `SRP/RenderPassEventTests.cs` | L2 | — |
| RenderQueueRange | SRP | `SRP/RenderQueueRangeTests.cs` | L2 | — |
| NativeRendering | SRP | `SRP/NativeRenderingTests.cs` | L2 | — |
| SRP Integration | SRP | `SRP/SRPIntegrationTests.cs` | L2 | Needs L3b render sanity |
| GPU Device | GPU | `GPU/GpuDeviceFixture.cs` | L2 | Fixture only |
| GPU Interop | GPU | `GPU/GpuInteropTests.cs` | L3b | — |
| GPU NativeRender | GPU | `GPU/GpuNativeRenderTests.cs` | L3b | — |
| GPU Texture | GPU | `GPU/GpuTextureTests.cs` | L3b | — |
| **SceneManager** | Runtime | _(none)_ | **L0 only** | Needs serialization, load/unload tests |
| **GameObject** | Runtime | _(none)_ | **L0 only** | Needs lifecycle, hierarchy, component tests |
| **AssetRef** | Runtime | _(none)_ | **L0 only** | Needs reference resolution, null handling tests |
| **Application** | Runtime | _(none)_ | **L0 only** | Needs init/update order, lifecycle tests |
| **ForwardRenderer** | Rendering | `SRP/SRPIntegrationTests.cs` (partial) | **L2 partial** | Needs per-pass L3b sanity tests |
| **Editor PlayMode** | Editor | _(none)_ | **L0 only** | Needs state transition tests |

## Priority: Test Gaps to Fill

1. **SceneManager** — core system, zero tests
2. **GameObject lifecycle** — core system, zero tests
3. **AssetRef** — frequently used, zero tests
4. **ForwardRenderer per-pass** — rendering pipeline, only partial integration tests
5. **Application lifecycle** — startup chain, zero tests
6. **Editor PlayMode** — play/stop transitions, zero tests
