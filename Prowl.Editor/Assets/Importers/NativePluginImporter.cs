// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Runtime.InteropServices;

using Prowl.Echo;
using Prowl.Runtime;
using Prowl.Runtime.Utils;

namespace Prowl.Editor.Assets;

/// <summary>
/// Importer for native plugin libraries (.dll, .so, .dylib) placed under <c>Assets/Plugins/</c>.
/// Stores platform and architecture metadata in the <c>.meta</c> file so the build system
/// can copy only the correct variant to the output.
/// </summary>
/// <remarks>
/// Files with these extensions outside <c>Assets/Plugins/</c> are ignored (no import).
/// </remarks>
[Importer("FileIcon.png", typeof(TextAsset), ".dll", ".so", ".dylib")]
public class NativePluginImporter : ScriptedImporter
{
    /// <summary>If true, the plugin matches any platform (ignore <see cref="TargetPlatform"/>).</summary>
    public bool AnyPlatform = true;

    /// <summary>Target platform. Only used when <see cref="AnyPlatform"/> is false.</summary>
    public Platform TargetPlatform = RuntimeUtils.GetOSPlatform();

    /// <summary>If true, the plugin matches any architecture (ignore <see cref="TargetArchitecture"/>).</summary>
    public bool AnyArchitecture = true;

    /// <summary>Target architecture. Only used when <see cref="AnyArchitecture"/> is false.</summary>
    public Architecture TargetArchitecture = RuntimeInformation.OSArchitecture;

    public override void Import(SerializedAsset ctx, FileInfo assetPath)
    {
        // Only import files that reside under an Assets/Plugins/ directory tree.
        // Files elsewhere with .dll/.so/.dylib extensions are left to other importers or ignored.
        if (!IsUnderPluginsDirectory(assetPath))
            return;

        // Store a lightweight TextAsset with the plugin file name as content.
        // The actual binary is NOT embedded — it is copied directly by the build system.
        TextAsset metaAsset = new()
        {
            Text = assetPath.Name,
            Name = Path.GetFileNameWithoutExtension(assetPath.Name)
        };
        ctx.SetMainObject(metaAsset);
    }

    /// <summary>
    /// Checks whether this plugin should be included in a build targeting the given platform and architecture.
    /// </summary>
    public bool MatchesBuildTarget(Platform platform, Architecture architecture)
    {
        if (!AnyPlatform && TargetPlatform != platform)
            return false;

        if (!AnyArchitecture && TargetArchitecture != architecture)
            return false;

        return true;
    }

    private static bool IsUnderPluginsDirectory(FileInfo assetPath)
    {
        // Walk up the directory tree looking for a "Plugins" folder that is a child of "Assets"
        DirectoryInfo? dir = assetPath.Directory;
        while (dir != null)
        {
            if (string.Equals(dir.Name, "Plugins", StringComparison.OrdinalIgnoreCase))
            {
                // Verify parent is "Assets"
                if (dir.Parent != null &&
                    string.Equals(dir.Parent.Name, "Assets", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            dir = dir.Parent;
        }

        return false;
    }
}
