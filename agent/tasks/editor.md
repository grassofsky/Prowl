# Editor Task Routing

## Key Areas

- Prowl.Editor/Program.cs (host entry and lifecycle wiring)
- Prowl.Editor/EditorGuiManager.cs (main editor gui loop)
- Prowl.Editor/PlayMode.cs (play/stop transitions)
- Prowl.Editor/Assets/ (asset provider and import path)
- Prowl.Editor/Project/ (project-level settings)

## Typical Task Flow

1. Identify whether change is UI, asset pipeline, or play mode.
2. Check runtime boundary impact (IAssetProvider, AssemblyManager interactions).
3. Keep editor-only logic out of runtime where possible.
4. Verify startup and play-mode transitions after changes.

## Notes

Editor project can package desktop player files based on csproj conditions; keep this behavior intact unless task requests packaging changes.
