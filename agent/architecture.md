# Architecture Map

## Module Responsibilities

| Project | Responsibility |
| --- | --- |
| Prowl.Runtime | Engine core systems: app loop, scene, rendering, assets, physics/audio integration |
| Prowl.Editor | Editor app, project tooling, play-mode orchestration, asset import tooling |
| Prowl.Players/Prowl.Desktop | Standalone player host |
| Prowl.Runtime.Test | Runtime unit tests |
| External/Prowl.Veldrid | Graphics backend dependency |
| External/Prowl.DotRecast | Navigation dependency |

## Startup Chain

Editor host:

Program.Main -> Application.Initialize -> Application.Update/Application.Render -> Application.Run

Desktop host:

DesktopPlayer.Main -> Application.Update/Application.Render -> Application.Run

Runtime loop root:

Application.Run -> AppInitialize -> AppUpdate

## Rendering Dispatch

Camera decides whether to use SRP asset path or legacy fallback.

Primary files:

- Prowl.Runtime/Components/Camera.cs
- Prowl.Runtime/Rendering/RenderPipelineAsset.cs
- Prowl.Runtime/Rendering/ScriptableRenderContext.cs
- Prowl.Runtime/Rendering/ForwardRenderer.cs
- Prowl.Runtime/Rendering/RenderPipeline/RenderPipeline.cs

## Asset Provider Boundary

IAssetProvider in runtime is implemented by environment-specific providers:

- Editor provider in Prowl.Editor/Assets/
- Standalone provider in Prowl.Players/Prowl.Desktop/

## Hot Reload Boundary

AssemblyManager in runtime handles dynamic load/unload and is the boundary used by editor play mode.
