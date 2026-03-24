// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;

using Prowl.Runtime.Rendering.Pipelines;
using Veldrid;

namespace Prowl.Runtime.Rendering;

public class ScriptableRenderContext : IDisposable
{
    private readonly List<RenderPass> _passes;
    private readonly List<CommandBuffer> _pendingCommandBuffers;
    private Camera _camera;
    private SRPRenderingData _renderingData;
    private CullingResults _cullingResults;
    private Framebuffer _currentFramebuffer;

    public ScriptableRenderContext()
    {
        _passes = new List<RenderPass>();
        _pendingCommandBuffers = new List<CommandBuffer>();
        _cullingResults = new CullingResults();
    }

    public void Setup(Camera camera, in SRPRenderingData data)
    {
        _camera = camera;
        _renderingData = data;
        _passes.Clear();
        _pendingCommandBuffers.Clear();
    }

    public void EnqueuePass(RenderPass pass)
    {
        if (pass == null)
            throw new ArgumentNullException(nameof(pass));
        _passes.Add(pass);
    }

    public void InsertPass(int index, RenderPass pass)
    {
        if (pass == null)
            throw new ArgumentNullException(nameof(pass));
        _passes.Insert(index, pass);
    }

    public void ClearPasses()
    {
        _passes.Clear();
    }

    public void ExecuteCommandBuffer(CommandBuffer cmd)
    {
        if (cmd == null)
            throw new ArgumentNullException(nameof(cmd));
        _pendingCommandBuffers.Add(cmd);
    }

    public void Submit()
    {
        foreach (var cmd in _pendingCommandBuffers)
        {
            Graphics.SubmitCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        _pendingCommandBuffers.Clear();
    }

    public void DrawRenderers(DrawingSettings drawingSettings, FilteringSettings filteringSettings)
    {
        var cullingResults = GetCullingResults();
        cullingResults.DrawRenderers(this, drawingSettings, filteringSettings);
    }

    public void DrawRenderers(DrawingSettings drawingSettings, FilteringSettings filteringSettings, RenderStateBlock stateBlock)
    {
        DrawRenderers(drawingSettings, filteringSettings);
    }

    public CameraData GetCameraData()
    {
        return _renderingData?.CameraData;
    }

    public CullingResults GetCullingResults()
    {
        return _cullingResults;
    }

    public ref SRPRenderingData GetRenderingData()
    {
        return ref _renderingData;
    }

    internal void SetCullingResults(CullingResults results)
    {
        _cullingResults = results;
    }

    internal void SetRenderTarget(Framebuffer framebuffer)
    {
        _currentFramebuffer = framebuffer;
    }

    internal Framebuffer GetCurrentRenderTarget()
    {
        return _currentFramebuffer;
    }

    internal IReadOnlyList<RenderPass> GetPasses()
    {
        return _passes;
    }

    internal void ExecutePasses()
    {
        var sortedPasses = _passes.OrderBy(p => p.InjectionPoint).ToList();

        foreach (var pass in sortedPasses)
        {
            pass.Configure(this, ref _renderingData);
            pass.Execute(this, ref _renderingData);
        }
    }

    internal void CleanupPasses()
    {
        foreach (var pass in _passes)
        {
            pass.Cleanup(this, ref _renderingData);
        }
        _passes.Clear();
    }

    public void Dispose()
    {
        foreach (var cmd in _pendingCommandBuffers)
        {
            cmd.Dispose();
        }
        _pendingCommandBuffers.Clear();
        _passes.Clear();
        _cullingResults?.Clear();
    }
}
