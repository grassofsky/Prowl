// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

namespace Prowl.Runtime.Rendering;

public abstract class ScriptableRenderer : IDisposable
{
    protected readonly List<RenderPass> _passes = new();
    protected readonly List<RenderFeature> _features = new();
    protected readonly List<RenderPass> _sortedPassesCache = new();
    protected bool _passesDirty = true;

    protected ScriptableRenderContext _context;

    public IReadOnlyList<RenderFeature> Features => _features;
    public IReadOnlyList<RenderPass> Passes => _passes;

    public ScriptableRenderer()
    {
        _context = new ScriptableRenderContext();
    }

    public abstract void Setup(ScriptableRenderContext context, ref SRPRenderingData renderingData);

    public virtual void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (_passesDirty)
        {
            SortPasses();
        }

        List<RenderPass> executedPasses = new();
        try
        {
            foreach (var pass in _sortedPassesCache)
            {
                try
                {
                    pass.Configure(context, ref renderingData);
                    pass.Execute(context, ref renderingData);
                    executedPasses.Add(pass);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SRP] Error executing pass '{pass.Name}': {ex.Message}");
                }
            }
        }
        finally
        {
            foreach (var pass in executedPasses)
            {
                try
                {
                    pass.Cleanup(context, ref renderingData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SRP] Error cleaning up pass '{pass.Name}': {ex.Message}");
                }
            }
        }
    }

    public virtual void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        foreach (var pass in _passes)
        {
            try
            {
                pass.Cleanup(context, ref renderingData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SRP] Error cleaning up pass '{pass.Name}': {ex.Message}");
            }
        }
        _passes.Clear();
        _passesDirty = true;
    }

    protected virtual void SortPasses()
    {
        _sortedPassesCache.Clear();
        _sortedPassesCache.Capacity = _passes.Count;

        for (int i = 0; i < _passes.Count; i++)
        {
            _sortedPassesCache.Add(_passes[i]);
        }

        _sortedPassesCache.Sort((a, b) => a.InjectionPoint.CompareTo(b.InjectionPoint));
        _passesDirty = false;
    }

    public void EnqueuePass(RenderPass pass)
    {
        if (pass == null)
            throw new ArgumentNullException(nameof(pass));
        _passes.Add(pass);
        _passesDirty = true;
    }

    public void ClearPasses()
    {
        _passes.Clear();
        _sortedPassesCache.Clear();
        _passesDirty = true;
    }

    public void AddFeature(RenderFeature feature)
    {
        if (feature == null)
            throw new ArgumentNullException(nameof(feature));
        _features.Add(feature);
        feature.Create();
    }

    public void RemoveFeature(RenderFeature feature)
    {
        if (feature != null && _features.Remove(feature))
        {
            feature.Dispose();
        }
    }

    public void ClearFeatures()
    {
        foreach (var feature in _features)
        {
            feature?.Dispose();
        }
        _features.Clear();
    }

    protected void ExecuteFeatures(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        for (int i = 0; i < _features.Count; i++)
        {
            var feature = _features[i];
            if (feature != null && feature.Enabled)
            {
                try
                {
                    feature.AddRenderPasses(context, ref renderingData);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SRP] Error in feature '{feature.FeatureName}': {ex.Message}");
                }
            }
        }
    }

    public void Render(Camera camera, SRPRenderingData data)
    {
        Render(camera, data, _context);
    }

    public void Render(Camera camera, SRPRenderingData data, ScriptableRenderContext context)
    {
        if (camera == null)
            throw new ArgumentNullException(nameof(camera));
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        context.Setup(camera, data);

        Setup(context, ref data);

        ExecuteFeatures(context, ref data);

        Execute(context, ref data);

        context.Submit();

        Cleanup(context, ref data);
    }

    public virtual void Dispose()
    {
        ClearFeatures();
        ClearPasses();
        _context?.Dispose();
    }

    public virtual void CleanupCamera(Camera camera)
    {
        // Base implementation does nothing
        // Derived classes can override to clean up camera-specific resources
    }
}
