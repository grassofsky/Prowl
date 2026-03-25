// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Prowl.Runtime.Rendering.Passes;
using Veldrid;

namespace Prowl.Runtime.Rendering;

public class ForwardRenderer : ScriptableRenderer
{
    private readonly DepthPrepass _depthPrepass;
    private readonly MainLightShadowPass _shadowPass;
    private readonly OpaqueRenderPass _opaquePass;
    private readonly TransparentRenderPass _transparentPass;

    private readonly PipelineSettings _settings;

    private readonly ConditionalWeakTable<Camera, PerCameraData> _cameraDataCache = new();

    public ForwardRenderer(PipelineSettings settings) : base()
    {
        _settings = settings ?? new PipelineSettings();

        _depthPrepass = new DepthPrepass();
        _shadowPass = new MainLightShadowPass();
        _opaquePass = new OpaqueRenderPass();
        _transparentPass = new TransparentRenderPass();
    }

    public override void Setup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        var cameraData = renderingData.CameraData;

        if (!_cameraDataCache.TryGetValue(cameraData.Camera, out PerCameraData perCameraData))
        {
            perCameraData = new PerCameraData();
            _cameraDataCache.Add(cameraData.Camera, perCameraData);
        }

        EnsureRenderTargets(cameraData, perCameraData);

        if (_settings.UseDepthPrepass && cameraData.DepthTextureMode.HasFlag(DepthTextureMode.Depth))
        {
            _depthPrepass.Setup(perCameraData.ColorTarget);
            EnqueuePass(_depthPrepass);
        }

        EnqueuePass(_shadowPass);

        _opaquePass.Setup(perCameraData.ColorTarget, perCameraData.DepthTarget);
        EnqueuePass(_opaquePass);

        _transparentPass.Setup(perCameraData.ColorTarget, perCameraData.DepthTarget);
        EnqueuePass(_transparentPass);

        if (renderingData.IsSceneViewCamera && renderingData.DisplayGrid)
        {
            EnqueueGridPass(renderingData, perCameraData.ColorTarget);
        }
    }

    private void EnsureRenderTargets(CameraData cameraData, PerCameraData perCameraData)
    {
        bool needsNewTarget = perCameraData.ColorTarget == null ||
                              perCameraData.Width != cameraData.PixelWidth ||
                              perCameraData.Height != cameraData.PixelHeight;

        if (needsNewTarget)
        {
            if (perCameraData.ColorTarget != null)
            {
                RenderTexture.ReleaseTemporaryRT(perCameraData.ColorTarget);
            }

            PixelFormat colorFormat = _settings.UseHDR && cameraData.HDR
                ? PixelFormat.R16_G16_B16_A16_Float
                : PixelFormat.R8_G8_B8_A8_UNorm;

            var desc = new RenderTextureDescription(
                (uint)cameraData.PixelWidth,
                (uint)cameraData.PixelHeight,
                null,
                new[] { colorFormat },
                true, false,
                TextureSampleCount.Count1);

            perCameraData.ColorTarget = RenderTexture.GetTemporaryRT(desc);
            perCameraData.DepthTarget = perCameraData.ColorTarget;
            perCameraData.Width = (int)cameraData.PixelWidth;
            perCameraData.Height = (int)cameraData.PixelHeight;
        }
    }

    private void EnqueueGridPass(SRPRenderingData renderingData, RenderTexture target)
    {
        var gridPass = new GridPass();
        gridPass.Setup(target, renderingData.GridMatrix, renderingData.GridColor, renderingData.GridSizes);
        EnqueuePass(gridPass);
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        base.Cleanup(context, ref renderingData);
    }

    public override void Dispose()
    {
        CleanupStaticResources();
    }

    public void CleanupCamera(Camera camera)
    {
        if (_cameraDataCache.TryGetValue(camera, out PerCameraData perCameraData))
        {
            if (perCameraData.ColorTarget != null)
            {
                RenderTexture.ReleaseTemporaryRT(perCameraData.ColorTarget);
            }
            _cameraDataCache.Remove(camera);
        }
    }

    public RenderTexture GetCameraTarget(Camera camera)
    {
        if (_cameraDataCache.TryGetValue(camera, out PerCameraData perCameraData))
            return perCameraData.ColorTarget;
        return null;
    }

    internal static void CleanupStaticResources()
    {
        GridPass.CleanupStaticResources();
    }

    private class PerCameraData
    {
        public RenderTexture ColorTarget;
        public RenderTexture DepthTarget;
        public int Width;
        public int Height;
    }
}

internal class GridPass : RenderPass
{
    private RenderTexture _target;
    private Matrix4x4 _gridMatrix;
    private Color _gridColor;
    private Vector3 _gridSizes;

    private static Material s_gridMaterial;
    private static Mesh s_quadMesh;

    public GridPass()
    {
        Name = "Grid Pass";
        InjectionPoint = RenderPassEvent.AfterRenderingTransparents;
    }

    public void Setup(RenderTexture target, Matrix4x4 gridMatrix, Color gridColor, Vector3 gridSizes)
    {
        _target = target;
        _gridMatrix = gridMatrix;
        _gridColor = gridColor;
        _gridSizes = gridSizes;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (_target == null)
            return;

        var cmd = CommandBufferPool.Get(Name);

        cmd.SetRenderTarget(_target);

        EnsureResources();

        const double GRID_SCALE = 10000.0;

        Matrix4x4 grid = Matrix4x4.CreateScale(GRID_SCALE);
        grid *= _gridMatrix;

        grid.Translation -= renderingData.CameraData.WorldSpaceCameraPos;

        cmd.SetMaterial(s_gridMaterial);
        cmd.SetMatrix("prowl_ObjectToWorld", grid.ToFloat());
        cmd.UpdateBuffer("_PerDraw");

        cmd.SetColor("_GridColor", _gridColor);
        cmd.SetFloat("_LineWidth", (float)_gridSizes.z);
        cmd.SetFloat("_PrimaryGridSize", 1f / (float)_gridSizes.x * (float)GRID_SCALE * 2);
        cmd.SetFloat("_SecondaryGridSize", 1f / (float)_gridSizes.y * (float)GRID_SCALE * 2);
        cmd.SetFloat("_Falloff", 15.0f);
        cmd.SetFloat("_MaxDist", (float)System.Math.Min(renderingData.CameraData.FarClipPlane, GRID_SCALE));

        cmd.DrawSingle(s_quadMesh);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private static void EnsureResources()
    {
        if (s_gridMaterial == null)
        {
            var shaderRef = Application.AssetProvider?.LoadAsset<Shader>("Defaults/Grid.shader");
            if (shaderRef.HasValue)
            {
                s_gridMaterial = new Material(shaderRef.Value);
            }
            else
            {
                Debug.LogWarning("[SRP] Failed to load Grid.shader. Grid rendering will be disabled.");
            }
        }

        if (s_quadMesh == null)
        {
            s_quadMesh = Mesh.CreateQuad(Vector2.one);
        }
    }

    internal static void CleanupStaticResources()
    {
        if (s_gridMaterial != null)
        {
            s_gridMaterial = null;
        }
        if (s_quadMesh != null)
        {
            s_quadMesh = null;
        }
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        _target = null;
    }
}
