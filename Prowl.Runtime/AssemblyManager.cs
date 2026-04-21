// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

using Prowl.Echo;

using Prowl.Runtime.Utils;

namespace Prowl.Runtime;


[FilePath("ProjectAssemblies.projsetting", FilePathAttribute.Location.Setting)]
public class ProjectAssemblyReferences : ScriptableSingleton<ProjectAssemblyReferences>
{
    [SerializeField, HideInInspector]
    private List<string> _assemblyNames = [];

    public IEnumerable<string> AssemblyNames => _assemblyNames;


    public void AddAssembly(string name)
    {
        if (!_assemblyNames.Contains(name))
            _assemblyNames.Add(name);

        Save();
    }

    public void RemoveAssembly(string name)
    {
        _assemblyNames.Remove(name);
        Save();
    }

}


public static class AssemblyManager
{
#if DEBUG
    private static bool verboseLoadMessages = true;
#else
    private static bool verboseLoadMessages = false;
#endif

    private static ExternalAssemblyLoadContext? _externalAssemblyLoadContext;
    private static List<(WeakReference, MulticastDelegate)> _unloadLifetimeDelegates = new();
    private static List<Action> _unloadDelegates = new();

    public static IEnumerable<Assembly> ExternalAssemblies => _externalAssemblyLoadContext?.Assemblies ?? [];

    public static bool HasExternalAssemblies => _externalAssemblyLoadContext != null;

    public static void Initialize()
    {
        OnAssemblyUnloadAttribute.FindAll();
        OnAssemblyLoadAttribute.FindAll();

        // Initialize native plugin resolver so [DllImport] in engine code also resolves via Plugins/
        Utils.NativePluginResolver.Initialize();
    }


    public static void LoadProjectAssemblies()
    {
        foreach (string assemblyName in ProjectAssemblyReferences.Instance.AssemblyNames)
            Assembly.Load(assemblyName);
    }


    public static Assembly? LoadExternalAssembly(string assemblyPath, bool isDependency)
    {
        int attempts = 0;
        const int MAX_ATTEMPTS = 3;
        
        while (attempts < MAX_ATTEMPTS)
        {
            attempts++;
            
            try
            {
                // Create new context if null or if previous context was unloading
                if (_externalAssemblyLoadContext == null)
                {
                    _externalAssemblyLoadContext = new ExternalAssemblyLoadContext();
                }
                
                Assembly asm = _externalAssemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

                if (isDependency)
                    _externalAssemblyLoadContext.AddDependency(assemblyPath);

                Debug.LogSuccess($"[AssemblyManager] Successfully loaded external assembly from {assemblyPath}");

                return asm;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("unloading") || ex.Message.Contains("unloaded"))
            {
                Debug.LogWarning($"[AssemblyManager] Context was unloading, creating new context (attempt {attempts}/{MAX_ATTEMPTS})");
                _externalAssemblyLoadContext = null;
                
                // Force GC to clean up the old context
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (Exception ex)
            {
                Debug.LogException(new AssemblyLoadException($"Failed to load External Assembly: {assemblyPath}", ex));
                return null;
            }
        }
        
        Debug.LogError($"[AssemblyManager] Failed to load assembly after {MAX_ATTEMPTS} attempts");
        return null;
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Unload(Action? onFail = null)
    {
        if (_externalAssemblyLoadContext is null)
            return;

        OnAssemblyUnloadAttribute.Invoke();

        AssemblyMethodAttributeBase.Clear();

        Echo.Serializer.ClearCache();

        Utils.NativePluginResolver.Invalidate();

        InvokeUnloadDelegate();

        UnloadInternal(out WeakReference externalAssemblyLoadContextRef);

        const int MAX_GC_ATTEMPTS = 10;

        for (int i = 0; externalAssemblyLoadContextRef.IsAlive; i++)
        {
            if (i >= MAX_GC_ATTEMPTS)
            {
                Debug.LogException(new AssemblyUnloadException($"Failed to unload external assemblies."));

                onFail?.Invoke();
                _externalAssemblyLoadContext = externalAssemblyLoadContextRef.Target as ExternalAssemblyLoadContext;

                return;
            }

            foreach (Assembly assembly in ExternalAssemblies)
                TypeDescriptor.Refresh(assembly);

            if (verboseLoadMessages)
                Debug.Log($"GC Attempt ({i + 1}/{MAX_GC_ATTEMPTS})...");

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        if (verboseLoadMessages)
            Debug.LogSuccess($"Successfully unloaded external assemblies.");
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UnloadInternal(out WeakReference externalAssemblyLoadContextRef)
    {
        foreach (Assembly assembly in ExternalAssemblies)
            Debug.Log($"Unloading external assembly from: '{assembly.Location}'...");

        // crashes after recovery and attempted unloading for the second time
        if (_externalAssemblyLoadContext != null)
            _externalAssemblyLoadContext.Unload();

        externalAssemblyLoadContextRef = new WeakReference(_externalAssemblyLoadContext);
        _externalAssemblyLoadContext = null;
    }


    public static void AddUnloadTask(Action onUnload)
    {
        _unloadDelegates.Add(onUnload);
    }


    public static void AddUnloadTaskWithLifetime<T>(T lifetimeDependency, Action<T> onUnload)
    {
        _unloadLifetimeDelegates.Add((new WeakReference(lifetimeDependency), onUnload));
    }


    private static void InvokeUnloadDelegate()
    {
        foreach ((WeakReference lifetimeDependency, MulticastDelegate unloadDelegate) in _unloadLifetimeDelegates)
        {
            if (!lifetimeDependency.IsAlive)
                continue;

            try
            {
                _ = unloadDelegate.DynamicInvoke([lifetimeDependency.Target])!;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        _unloadLifetimeDelegates.Clear();

        foreach (Action unloadDelegate in _unloadDelegates)
        {
            try
            {
                unloadDelegate.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        _unloadDelegates.Clear();
    }


    public static void Dispose()
    {
        UnloadInternal(out WeakReference _);
    }


    private class ExternalAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly List<AssemblyDependencyResolver> _assemblyDependencyResolvers;


        public ExternalAssemblyLoadContext() : base(true)
        {
            _assemblyDependencyResolvers = new List<AssemblyDependencyResolver>();
        }


        public void AddDependency(string assemblyPath)
        {
            _assemblyDependencyResolvers.Add(new AssemblyDependencyResolver(assemblyPath));
        }


        protected override Assembly? Load(AssemblyName assemblyName)
        {
            foreach (AssemblyDependencyResolver assemblyDependencyResolver in _assemblyDependencyResolvers)
            {
                string? resolvedPath = assemblyDependencyResolver.ResolveAssemblyToPath(assemblyName);

                if (resolvedPath != null)
                    return LoadFromAssemblyPath(resolvedPath);
            }

            return null;
        }


        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            // Try to resolve from Plugins/ directories via the centralized resolver
            if (Utils.NativePluginResolver.TryLoadLibrary(unmanagedDllName, out IntPtr handle))
                return handle;

            // Also try dependency resolvers (e.g. deps.json entries)
            foreach (var resolver in _assemblyDependencyResolvers)
            {
                string? resolvedPath = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                if (resolvedPath != null && NativeLibrary.TryLoad(resolvedPath, out handle))
                    return handle;
            }

            return IntPtr.Zero;
        }
    }
}


public class AssemblyLoadException : Exception
{
    public AssemblyLoadException(string message) : base(message) { }
    public AssemblyLoadException(string message, Exception? innerException) : base(message, innerException) { }
}


public class AssemblyUnloadException : Exception
{
    public AssemblyUnloadException(string message) : base(message) { }
    public AssemblyUnloadException(string message, Exception? innerException) : base(message, innerException) { }
}
