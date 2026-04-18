// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;

using Prowl.Runtime.Rendering.Pipelines;
using Veldrid;

namespace Prowl.Runtime.Rendering;

public class ScriptableRenderContext : IDisposable
{
    private readonly List<RenderPass> _passes;
    private Camera _camera;
    private SRPRenderingData _renderingData;
    private CullingResults _cullingResults;
    private Framebuffer _currentFramebuffer;

    public ScriptableRenderContext()
    {
        _passes = new List<RenderPass>();
        _cullingResults = new CullingResults();
    }

    public void Setup(Camera camera, in SRPRenderingData data)
    {
        _camera = camera;
        _renderingData = data;
        _passes.Clear();
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
        
        Graphics.SubmitCommandBuffer(cmd);
    }

    public void Submit()
    {
        // Command buffers are submitted immediately in ExecuteCommandBuffer()
    }

    public void DrawRenderers(DrawingSettings drawingSettings, FilteringSettings filteringSettings, CommandBuffer cmd)
    {
        if (cmd == null)
            throw new ArgumentNullException(nameof(cmd), "CommandBuffer is required for DrawRenderers.");
        
        var cullingResults = GetCullingResults();
        cullingResults.DrawRenderers(this, drawingSettings, filteringSettings, cmd);
    }

    public void DrawRenderers(DrawingSettings drawingSettings, FilteringSettings filteringSettings, RenderStateBlock stateBlock, CommandBuffer cmd)
    {
        // TODO: Apply RenderStateBlock overrides once CommandBuffer supports direct state setters.
        // Currently pipeline state is driven by ShaderPass/Material, so state overrides are not yet applied.
        if (stateBlock.OverrideDepthState || stateBlock.OverrideBlendState || stateBlock.OverrideRasterizerState)
            Debug.LogWarning("[SRP] RenderStateBlock overrides are not yet supported and will be ignored.");

        DrawRenderers(drawingSettings, filteringSettings, cmd);
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

    internal void CleanupPasses()
    {
        foreach (var pass in _passes)
        {
            pass.Cleanup(this, ref _renderingData);
            pass.ReleaseCombinedFramebuffer();
        }
        _passes.Clear();
    }

    public void Dispose()
    {
        _passes.Clear();
        _cullingResults?.Clear();
    }
}
