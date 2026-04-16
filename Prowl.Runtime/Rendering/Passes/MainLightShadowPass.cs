// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Prowl.Runtime.Rendering.Pipelines;
using Veldrid;

namespace Prowl.Runtime.Rendering.Passes;

public class MainLightShadowPass : RenderPass
{
    private const bool CAMERA_RELATIVE = true;

    private RenderTexture _shadowAtlas;
    private GraphicsBuffer _lightBuffer;
    private int _lightCount;
    private readonly List<ShadowSliceData> _shadowSlices;

    public MainLightShadowPass()
    {
        Name = "Main Light Shadow Pass";
        InjectionPoint = RenderPassEvent.BeforeRenderingShadows;
        _shadowSlices = new List<ShadowSliceData>();
    }

    public void Setup(RenderTexture colorTarget, RenderTexture depthTarget)
    {
        ConfigureTarget(colorTarget, depthTarget);
        //ConfigureClear(ClearFlag.All, Color.red);
    }

    public override void Configure(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        ShadowAtlas.TryInitialize();
        ShadowAtlas.Clear();
        _shadowSlices.Clear();
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (renderingData?.CameraData == null || renderingData.CullingResults == null)
            return;

        var cameraData = renderingData.CameraData;
        var lights = renderingData.CullingResults.GetVisibleLights();

        _shadowAtlas = ShadowAtlas.GetAtlas();
        ClearShadowAtlas(_shadowAtlas);

        List<GPULight> gpuLights = CreateLightBuffer(context, lights, cameraData.WorldSpaceCameraPos, cameraData.CullingMask);

        unsafe
        {
            if (_lightBuffer == null || gpuLights.Count > _lightCount)
            {
                _lightBuffer?.Dispose();
                _lightBuffer = new GraphicsBuffer((uint)gpuLights.Count, (uint)sizeof(GPULight), false);
            }

            if (gpuLights.Count > 0)
                _lightBuffer.SetData<GPULight>(gpuLights.ToArray(), 0);

            _lightCount = gpuLights.Count;
        }

        SetGlobalLightingUniforms(lights, cameraData.WorldSpaceCameraPos);

        renderingData.LightData.VisibleLights = new List<IRenderableLight>(lights);
        renderingData.LightData.MainLightIndex = GetMainLightIndex(lights);
        renderingData.ShadowData.ShadowAtlas = _shadowAtlas;
        renderingData.ShadowData.ShadowSlices = _shadowSlices;
    }

    private List<GPULight> CreateLightBuffer(ScriptableRenderContext context, IEnumerable<IRenderableLight> lights, Vector3 cameraPosition, LayerMask cullingMask)
    {
        List<GPULight> gpuLights = new();

        foreach (var light in lights)
        {
            int res = CalculateResolution(Vector3.Distance(cameraPosition, light.GetLightPosition()));
            if (light is DirectionalLight dir)
                res = (int)dir.shadowResolution;

            if (light.DoCastShadows())
            {
                Vector3 oldPos = Vector3.zero;
                if (light is DirectionalLight dirLight)
                {
                    oldPos = HandleDirectionalLightSnapping(dirLight, cameraPosition, res);
                }

                GPULight gpu = light.GetGPULight(ShadowAtlas.GetSize(), CAMERA_RELATIVE, cameraPosition);

                Vector2Int? slot = ShadowAtlas.ReserveTiles(res, res, light.GetLightID());

                if (slot != null)
                {
                    gpu.AtlasX = slot.Value.x;
                    gpu.AtlasY = slot.Value.y;
                    gpu.AtlasWidth = res;

                    RenderShadowMap(context, light, slot.Value, res, cameraPosition, cullingMask, out Matrix4x4 view, out Matrix4x4 proj);

                    _shadowSlices.Add(new ShadowSliceData
                    {
                        ViewMatrix = view,
                        ProjectionMatrix = proj,
                        AtlasX = slot.Value.x,
                        AtlasY = slot.Value.y,
                        AtlasWidth = res,
                        AtlasHeight = res
                    });
                }
                else
                {
                    gpu.AtlasX = -1;
                    gpu.AtlasY = -1;
                    gpu.AtlasWidth = 0;
                }

                gpuLights.Add(gpu);

                if (light is DirectionalLight dirLight2)
                {
                    dirLight2.Transform.position = oldPos;
                }
            }
            else
            {
                GPULight gpu = light.GetGPULight(0, CAMERA_RELATIVE, cameraPosition);
                gpu.AtlasX = -1;
                gpu.AtlasY = -1;
                gpu.AtlasWidth = 0;
                gpuLights.Add(gpu);
            }
        }

        return gpuLights;
    }

    private Vector3 HandleDirectionalLightSnapping(DirectionalLight dirLight, Vector3 cameraPosition, int res)
    {
        Vector3 lightDir = dirLight.Transform.forward;
        Vector3 lightUp = dirLight.Transform.up;
        Vector3 lightRight = Vector3.Cross(lightUp, lightDir).normalized;
        lightUp = Vector3.Cross(lightDir, lightRight).normalized;

        Matrix4x4 worldToLight = new Matrix4x4(
            new Vector4(lightRight.x, lightUp.x, lightDir.x, 0),
            new Vector4(lightRight.y, lightUp.y, lightDir.y, 0),
            new Vector4(lightRight.z, lightUp.z, lightDir.z, 0),
            new Vector4(0, 0, 0, 1));

        Vector3 lightSpacePos = Vector3.Transform(cameraPosition, worldToLight);

        float texelSize = (dirLight.shadowDistance * 2) / res;

        Vector3 snappedLightPos = new Vector3(
            MathD.Round(lightSpacePos.x / texelSize) * texelSize,
            MathD.Round(lightSpacePos.y / texelSize) * texelSize,
            lightSpacePos.z);

        Matrix4x4 lightToWorld = worldToLight.Invert();
        Vector3 snappedWorldPos = Vector3.Transform(snappedLightPos, lightToWorld);

        Vector3 oldPos = dirLight.Transform.position;
        dirLight.Transform.position = snappedWorldPos;

        return oldPos;
    }

    private void ClearShadowAtlas(RenderTexture atlasTexture)
    {
        CommandBuffer atlasClear = CommandBufferPool.Get("Shadow Atlas Clear");
        atlasClear.SetRenderTarget(atlasTexture);
        atlasClear.ClearRenderTarget(true, false, Color.black);

        Graphics.SubmitCommandBuffer(atlasClear);
        CommandBufferPool.Release(atlasClear);
    }

    private void RenderShadowMap(ScriptableRenderContext context, IRenderableLight light, Vector2Int slot, int res, Vector3 cameraPosition, LayerMask cullingMask, out Matrix4x4 viewOut, out Matrix4x4 projOut)
    {
        var cmd = CommandBufferPool.Get(Name + " Shadow");

        cmd.SetRenderTarget(_shadowAtlas);
        cmd.SetViewports(slot.x, slot.y, res, res, 0, 1000);

        light.GetShadowMatrix(out Matrix4x4 view, out Matrix4x4 proj);

        viewOut = view;
        projOut = proj;

        BoundingFrustum frustum = new(view * proj);
        if (CAMERA_RELATIVE)
            view.Translation = Vector3.zero;

        var shadowCullingResults = CullingResults.PerformCulling(
            new CameraData { WorldFrustum = frustum, CullingMask = cullingMask },
            RenderPipeline.GetRenderables(),
            null);

        SetGlobalCameraMatrices(cmd, view, proj);

        var tempCameraData = new CameraData { WorldSpaceCameraPos = cameraPosition };
        var drawingSettings = new DrawingSettings(
            new ShaderTagId("LightMode"),
            new SortingSettings(tempCameraData));
        drawingSettings.SetShaderPassValue("ShadowCaster");

        var filteringSettings = new FilteringSettings(RenderQueueRange.All, cullingMask);

        context.DrawRenderers(drawingSettings, filteringSettings, cmd);
        //cmd.SetFullViewports();

        Graphics.SubmitCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    private void SetGlobalCameraMatrices(CommandBuffer cmd, Matrix4x4 view, Matrix4x4 proj)
    {
        PropertyState.SetGlobalMatrix("prowl_MatV", view.ToFloat());
        PropertyState.SetGlobalMatrix("prowl_MatIV", view.Invert().ToFloat());
        PropertyState.SetGlobalMatrix("prowl_MatP", proj.ToFloat());
        PropertyState.SetGlobalMatrix("prowl_MatVP", (view * proj).ToFloat());
    }

    private void SetGlobalLightingUniforms(IEnumerable<IRenderableLight> lights, Vector3 cameraPosition)
    {
        Vector3 sunDirection = GetSunDirection(lights);

        if (_shadowAtlas != null)
            PropertyState.SetGlobalRawTexture("_ShadowAtlas", _shadowAtlas.ColorBuffers[0].InternalTexture, _shadowAtlas.ColorBuffers[0].Sampler.InternalSampler);
        PropertyState.SetGlobalBuffer("_Lights", _lightBuffer);
        PropertyState.SetGlobalInt("_LightCount", _lightCount);
        PropertyState.SetGlobalVector("_CameraWorldPos", cameraPosition);
        PropertyState.SetGlobalVector("_SunDir", sunDirection);
        PropertyState.SetGlobalVector("prowl_ShadowAtlasSize", new Vector2(ShadowAtlas.GetSize(), ShadowAtlas.GetSize()));
    }

    private int CalculateResolution(double distance)
    {
        double t = MathD.Clamp(distance / 16f, 0, 1);
        int tileSize = ShadowAtlas.GetTileSize();
        int resolution = MathD.RoundToInt(MathD.Lerp(ShadowAtlas.GetMaxShadowSize(), tileSize, t));

        return MathD.Max(tileSize, (resolution / tileSize) * tileSize);
    }

    private Vector3 GetSunDirection(IEnumerable<IRenderableLight> lights)
    {
        foreach (var light in lights)
        {
            if (light.GetLightType() == LightType.Directional)
                return light.GetLightDirection();
        }
        return Vector3.up;
    }

    private int GetMainLightIndex(IEnumerable<IRenderableLight> lights)
    {
        int index = 0;
        foreach (var light in lights)
        {
            if (light.GetLightType() == LightType.Directional)
                return index;
            index++;
        }
        return -1;
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        _shadowSlices.Clear();
        _lightBuffer?.Dispose();
        _lightBuffer = null;
    }
}
