// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Prowl.Runtime.Utils;

/// <summary>
/// Resolves native plugin libraries following a Unity-inspired <c>Assets/Plugins/</c> directory convention.
/// Supports platform/architecture-specific subdirectories using .NET Runtime Identifiers (RIDs).
/// </summary>
/// <remarks>
/// <b>Probe order</b> (first match wins):
/// <list type="number">
///   <item><c>{DataPath}/Plugins/{rid}/{name}{ext}</c> — Built player, RID-specific</item>
///   <item><c>{DataPath}/Plugins/{name}{ext}</c> — Built player, platform-agnostic</item>
///   <item><c>{DataPath}/Assets/Plugins/{rid}/{name}{ext}</c> — Editor mode, RID-specific</item>
///   <item><c>{DataPath}/Assets/Plugins/{name}{ext}</c> — Editor mode, platform-agnostic</item>
/// </list>
///
/// <b>RID examples:</b> <c>win-x64</c>, <c>linux-x64</c>, <c>osx-arm64</c>
///
/// On Unix-like platforms, the resolver also tries the <c>lib</c> prefix (e.g. <c>libfoo.so</c>).
/// </remarks>
public static class NativePluginResolver
{
    private static readonly ConcurrentDictionary<string, IntPtr> s_loadedLibraries = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object s_initLock = new();
    private static string? s_currentRid;
    private static string? s_nativeExtension;
    private static bool s_isUnixLike;
    private static int s_initialized; // 0 = not initialized, 1 = initialized
    private static bool s_dllSearchDirsRegistered;
    [ThreadStatic] private static bool s_resolving; // re-entrancy guard

    /// <summary>
    /// Initializes the resolver and registers a global DllImportResolver for the Prowl.Runtime assembly.
    /// Safe to call multiple times — subsequent calls are no-ops.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref s_initialized, 1, 0) != 0)
            return;

        s_currentRid = GetCurrentRid();
        s_nativeExtension = GetNativeExtension();
        s_isUnixLike = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Register for Prowl.Runtime so [DllImport] in engine code also resolves via Plugins/
        NativeLibrary.SetDllImportResolver(typeof(NativePluginResolver).Assembly, ResolverCallback);

        // Add the Plugins directories to the native library search path so that
        // transitive dependencies of plugins are found automatically.
        EnsureDllSearchDirectories();

        Debug.Log($"[NativePluginResolver] Initialized (RID={s_currentRid}, ext={s_nativeExtension})");
    }

    /// <summary>
    /// Clears the loaded library cache. Call on hot-reload so that re-resolved names
    /// pick up updated binaries (note: OS may still keep the old file locked).
    /// </summary>
    public static void Invalidate()
    {
        s_loadedLibraries.Clear();
        s_dllSearchDirsRegistered = false;
        Debug.Log("[NativePluginResolver] Cache invalidated.");
    }

    /// <summary>
    /// Tries to resolve a native library by name from the Plugins directories.
    /// </summary>
    /// <param name="unmanagedDllName">The library name as specified in [DllImport].</param>
    /// <param name="fullPath">On success, the absolute path to the resolved library file.</param>
    /// <returns><c>true</c> if the library was found on disk.</returns>
    public static bool TryResolve(string unmanagedDllName, out string fullPath)
    {
        fullPath = string.Empty;
        string? dataPath = Application.DataPath;
        if (string.IsNullOrEmpty(dataPath))
            return false;

        if (Volatile.Read(ref s_initialized) == 0)
            Initialize();

        // Lazily register DLL search dirs once DataPath becomes available (M5 fix)
        EnsureDllSearchDirectories();

        // Normalize: strip path separators, extension, and "lib" prefix
        string baseName = Path.GetFileNameWithoutExtension(unmanagedDllName);
        if (string.IsNullOrEmpty(baseName))
            return false;

        // Build candidate names: original name + with lib prefix on Unix
        string[] candidateNames = s_isUnixLike && !baseName.StartsWith("lib", StringComparison.Ordinal)
            ? [baseName + s_nativeExtension, "lib" + baseName + s_nativeExtension]
            : [baseName + s_nativeExtension];

        // Probe directories in priority order
        string[] probeDirs =
        [
            Path.Combine(dataPath, "Plugins", s_currentRid!),          // {DataPath}/Plugins/{rid}/
            Path.Combine(dataPath, "Plugins"),                          // {DataPath}/Plugins/
            Path.Combine(dataPath, "Assets", "Plugins", s_currentRid!),// {DataPath}/Assets/Plugins/{rid}/ (editor)
            Path.Combine(dataPath, "Assets", "Plugins"),                // {DataPath}/Assets/Plugins/ (editor)
        ];

        foreach (string dir in probeDirs)
        {
            if (!Directory.Exists(dir))
                continue;

            foreach (string candidate in candidateNames)
            {
                string path = Path.Combine(dir, candidate);
                if (File.Exists(path))
                {
                    fullPath = path;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to resolve and load a native library, returning the OS handle.
    /// Caches successfully loaded handles to avoid duplicate loads.
    /// </summary>
    /// <param name="unmanagedDllName">The library name as specified in [DllImport].</param>
    /// <param name="handle">On success, the native library handle.</param>
    /// <returns><c>true</c> if the library was loaded successfully.</returns>
    public static bool TryLoadLibrary(string unmanagedDllName, out IntPtr handle)
    {
        // Check cache first (ConcurrentDictionary — safe for concurrent reads)
        if (s_loadedLibraries.TryGetValue(unmanagedDllName, out handle))
            return true;

        if (TryResolve(unmanagedDllName, out string fullPath))
        {
            if (NativeLibrary.TryLoad(fullPath, out handle))
            {
                // GetOrAdd ensures only one handle per name is stored
                handle = s_loadedLibraries.GetOrAdd(unmanagedDllName, handle);
                Debug.Log($"[NativePluginResolver] Loaded: {fullPath}");
                return true;
            }

            Debug.LogWarning($"[NativePluginResolver] Found but failed to load: {fullPath}");
        }

        handle = IntPtr.Zero;
        return false;
    }

    /// <summary>
    /// Returns the RID-specific subdirectory name for the Plugins folder.
    /// Used by the build system to determine where to copy native plugins.
    /// </summary>
    /// <param name="platform">Target platform.</param>
    /// <param name="architecture">Target architecture.</param>
    /// <returns>RID string like <c>win-x64</c>, <c>linux-arm64</c>, <c>osx-x64</c>.</returns>
    public static string GetRid(Platform platform, Architecture architecture)
    {
        string os = platform switch
        {
            Platform.Windows => "win",
            Platform.Linux => "linux",
            Platform.MacOS => "osx",
            Platform.FreeBSD => "freebsd",
            Platform.Android => "android",
            Platform.iOS => "ios",
            Platform.Browser => "browser",
            _ => "unknown",
        };

        string arch = architecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "x64",
        };

        return $"{os}-{arch}";
    }

    // ---- Private helpers ----

    private static string GetCurrentRid()
    {
        Platform platform = RuntimeUtils.GetOSPlatform();
        Architecture arch = RuntimeInformation.OSArchitecture;
        return GetRid(platform, arch);
    }

    private static string GetNativeExtension()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return ".dll";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return ".dylib";
        return ".so";
    }

    private static IntPtr ResolverCallback(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        // Prevent infinite recursion: EnsureDllSearchDirectories calls [DllImport("kernel32")]
        // AddDllDirectory, which re-enters this callback.
        if (s_resolving)
            return IntPtr.Zero;

        s_resolving = true;
        try
        {
            if (TryLoadLibrary(libraryName, out IntPtr handle))
                return handle;

            // Fall back to default resolution
            return IntPtr.Zero;
        }
        finally
        {
            s_resolving = false;
        }
    }

    /// <summary>
    /// Lazily registers DLL search directories. Retries on each TryResolve call
    /// until DataPath is available (handles editor startup where DataPath is set
    /// after AssemblyManager.Initialize).
    /// </summary>
    private static void EnsureDllSearchDirectories()
    {
        if (s_dllSearchDirsRegistered) return;

        string? dataPath = Application.DataPath;
        if (string.IsNullOrEmpty(dataPath)) return;

        // Register both possible Plugins directories so that inter-DLL dependencies resolve
        string[] dirs =
        [
            Path.Combine(dataPath, "Plugins", s_currentRid!),
            Path.Combine(dataPath, "Plugins"),
            Path.Combine(dataPath, "Assets", "Plugins", s_currentRid!),
            Path.Combine(dataPath, "Assets", "Plugins"),
        ];

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (string dir in dirs)
            {
                if (Directory.Exists(dir))
                    AddDllDirectory(dir);
            }
        }
        else
        {
            // On Linux/macOS, prepend to the native library search path env var.
            // glibc's dlopen re-reads LD_LIBRARY_PATH on each call.
            string envVar = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "DYLD_LIBRARY_PATH"
                : "LD_LIBRARY_PATH";

            string existing = Environment.GetEnvironmentVariable(envVar) ?? string.Empty;
            var newDirs = new List<string>();
            foreach (string dir in dirs)
            {
                if (Directory.Exists(dir) && !existing.Contains(dir))
                    newDirs.Add(dir);
            }

            if (newDirs.Count > 0)
            {
                string prefix = string.Join(':', newDirs);
                string updated = string.IsNullOrEmpty(existing) ? prefix : $"{prefix}:{existing}";
                Environment.SetEnvironmentVariable(envVar, updated);
            }
        }

        s_dllSearchDirsRegistered = true;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr AddDllDirectory(string newDirectory);
}
