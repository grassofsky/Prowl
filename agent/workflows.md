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
