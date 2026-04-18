# Rendering Task Routing

## Entry Files

1. Prowl.Runtime/Components/Camera.cs
2. Prowl.Runtime/Rendering/RenderPipelineAsset.cs
3. Prowl.Runtime/Rendering/ScriptableRenderContext.cs
4. Prowl.Runtime/Rendering/ForwardRenderer.cs
5. Prowl.Runtime/Rendering/DefaultRenderPipelineAsset.cs
6. Prowl.Runtime/Rendering/RenderPipeline/RenderPipeline.cs

## Typical Task Flow

1. Confirm whether camera uses SRP asset or legacy fallback.
2. Apply minimal changes in selected path.
3. Validate pass ordering and context usage.
4. Run runtime tests and include SRP tests in summary.

## Test Focus

- Prowl.Runtime.Test/SRP/ScriptableRenderContextTests.cs
- Prowl.Runtime.Test/SRP/ShaderTagIdTests.cs

## Pitfalls

- Editor view and runtime camera may use different pipeline selection paths.
- New and legacy rendering systems coexist.
