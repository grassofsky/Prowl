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
    private int _enqueueCounter;
    private readonly HashSet<RenderPass> _executedAndCleaned = new();
    private readonly HashSet<RenderPass> _executedThisFrame = new();
    private readonly HashSet<RenderPass> _enqueuedPassSet = new();

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

        _executedThisFrame.Clear();
        try
        {
            foreach (var pass in _sortedPassesCache)
            {
                try
                {
                    pass.Configure(context, ref renderingData);
                    pass.Execute(context, ref renderingData);
                    _executedThisFrame.Add(pass);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SRP] Error executing pass '{pass.Name}': {ex.Message}");
                }
            }
        }
        finally
        {
            foreach (var pass in _executedThisFrame)
            {
                try
                {
                    pass.Cleanup(context, ref renderingData);
                    _executedAndCleaned.Add(pass);
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
        // Only clean up passes that were NOT already cleaned in Execute
        foreach (var pass in _passes)
        {
            if (_executedAndCleaned.Contains(pass))
                continue;

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
        _executedAndCleaned.Clear();
        _enqueuedPassSet.Clear();
        _enqueueCounter = 0;

        // Clear global shader properties to prevent data leaking between frames
        PropertyState.ClearGlobalData();
    }

    protected virtual void SortPasses()
    {
        _sortedPassesCache.Clear();
        _sortedPassesCache.Capacity = _passes.Count;

        for (int i = 0; i < _passes.Count; i++)
        {
            _sortedPassesCache.Add(_passes[i]);
        }

        _sortedPassesCache.Sort((a, b) =>
        {
            int cmp = a.InjectionPoint.CompareTo(b.InjectionPoint);
            return cmp != 0 ? cmp : a.EnqueueOrder.CompareTo(b.EnqueueOrder);
        });
        _passesDirty = false;
    }

    public void EnqueuePass(RenderPass pass)
    {
        if (pass == null)
            throw new ArgumentNullException(nameof(pass));
        pass.EnqueueOrder = _enqueueCounter++;
        _passes.Add(pass);
        _enqueuedPassSet.Add(pass);
        _passesDirty = true;
    }

    public void ClearPasses()
    {
        _passes.Clear();
        _sortedPassesCache.Clear();
        _enqueuedPassSet.Clear();
        _passesDirty = true;
        _enqueueCounter = 0;
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

        // Merge feature-injected passes from context into renderer pass list
        foreach (var pass in context.GetPasses())
        {
            if (!_enqueuedPassSet.Contains(pass))
            {
                EnqueuePass(pass);
            }
        }

        Execute(context, ref data);

        Cleanup(context, ref data);

        MotionVectorTracker.CleanupUnusedModelMatrices();
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
