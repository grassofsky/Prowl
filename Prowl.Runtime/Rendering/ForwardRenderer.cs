// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

using Prowl.Runtime.Rendering.Passes;
using Veldrid;

namespace Prowl.Runtime.Rendering;

public class ForwardRenderer : ScriptableRenderer
{
    private readonly DepthPrepass _depthPrepass;
    private readonly MainLightShadowPass _shadowPass;
    private readonly MotionVectorPass _motionVectorPass;
    private readonly OpaqueRenderPass _opaquePass;
    private readonly SkyboxPass _skyboxPass;
    private readonly TransparentRenderPass _transparentPass;
    private readonly PostProcessPass _opaquePostProcessPass;
    private readonly PostProcessPass _finalPostProcessPass;

    private readonly PipelineSettings _settings;

    private readonly ConditionalWeakTable<Camera, PerCameraData> _cameraDataCache = new();

    // Store effects and GUI for cleanup callbacks
    private List<MonoBehaviour> _allEffects;
    private GuiLayer _guiLayer;

    public ForwardRenderer(PipelineSettings settings) : base()
    {
        _settings = settings ?? new PipelineSettings();

        _depthPrepass = new DepthPrepass();
        _shadowPass = new MainLightShadowPass();
        _motionVectorPass = new MotionVectorPass();
        _opaquePass = new OpaqueRenderPass();
        _skyboxPass = new SkyboxPass();
        _transparentPass = new TransparentRenderPass();
        _opaquePostProcessPass = new PostProcessPass();
        _finalPostProcessPass = new PostProcessPass();
    }

    public override void Setup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        var cameraData = renderingData.CameraData;

        // Setup global uniforms first
        SetupGlobalUniforms(cameraData);

        if (!_cameraDataCache.TryGetValue(cameraData.Camera, out PerCameraData perCameraData))
        {
            perCameraData = new PerCameraData();
            _cameraDataCache.Add(cameraData.Camera, perCameraData);
        }

        EnsureRenderTargets(cameraData, perCameraData);

        // Gather image effects
        var (opaqueEffects, finalEffects, allEffects, guiLayer) = GatherImageEffects(cameraData.Camera, renderingData.IsSceneViewCamera);
        _allEffects = allEffects;
        _guiLayer = guiLayer;

        if (_settings.UseDepthPrepass && cameraData.DepthTextureMode.HasFlag(DepthTextureMode.Depth))
        {
            _depthPrepass.Setup(perCameraData.ColorTarget);
            EnqueuePass(_depthPrepass);
        }
        EnqueuePass(_shadowPass);

        _opaquePass.Setup(perCameraData.ColorTarget, perCameraData.DepthTarget, cameraData.ClearFlags, cameraData.ClearColor);
        EnqueuePass(_opaquePass);

        // Motion vectors after opaque objects
        if (cameraData.DepthTextureMode.HasFlag(DepthTextureMode.MotionVectors))
        {
            _motionVectorPass.Setup(perCameraData.ColorTarget, cameraData.PreviousViewProjectionMatrix);
            EnqueuePass(_motionVectorPass);
        }

        // Apply opaque image effects (after opaque objects, before transparent)
        if (opaqueEffects.Count > 0)
        {
            _opaquePostProcessPass.Setup(perCameraData.ColorTarget, opaqueEffects, true);
            EnqueuePass(_opaquePostProcessPass);
        }

        // Skybox after opaque objects
        _skyboxPass.Setup(perCameraData.ColorTarget);
        EnqueuePass(_skyboxPass);

        _transparentPass.Setup(perCameraData.ColorTarget, perCameraData.DepthTarget);
        EnqueuePass(_transparentPass);

        // Apply final image effects (after all rendering)
        if (finalEffects.Count > 0)
        {
            _finalPostProcessPass.Setup(perCameraData.ColorTarget, finalEffects, false);
            EnqueuePass(_finalPostProcessPass);
        }

        if (renderingData.IsSceneViewCamera && renderingData.DisplayGrid)
        {
            EnqueueGridPass(renderingData, perCameraData.ColorTarget);
        }
    }

    private void EnsureRenderTargets(CameraData cameraData, PerCameraData perCameraData)
    {
        PixelFormat colorFormat = _settings.UseHDR && cameraData.HDR
            ? PixelFormat.R16_G16_B16_A16_Float
            : PixelFormat.R8_G8_B8_A8_UNorm;

        bool needsNewTarget = perCameraData.ColorTarget == null ||
                              perCameraData.Width != cameraData.PixelWidth ||
                              perCameraData.Height != cameraData.PixelHeight ||
                              perCameraData.Format != colorFormat;

        if (needsNewTarget)
        {
            if (perCameraData.ColorTarget != null)
            {
                RenderTexture.ReleaseTemporaryRT(perCameraData.ColorTarget);
            }

            // Create a RenderTexture with both color and depth buffers
            var desc = new RenderTextureDescription(
                (uint)cameraData.PixelWidth,
                (uint)cameraData.PixelHeight,
                TextureUtility.GetBestSupportedDepthFormat(),
                new[] { colorFormat },
                true, false,
                TextureSampleCount.Count1);

            perCameraData.ColorTarget = RenderTexture.GetTemporaryRT(desc);
            perCameraData.DepthTarget = perCameraData.ColorTarget;
            perCameraData.Width = (int)cameraData.PixelWidth;
            perCameraData.Height = (int)cameraData.PixelHeight;
            perCameraData.Format = colorFormat;
        }
    }

    private void EnqueueGridPass(SRPRenderingData renderingData, RenderTexture target)
    {
        var gridPass = new GridPass();
        gridPass.Setup(target, renderingData.GridMatrix, renderingData.GridColor, renderingData.GridSizes);
        EnqueuePass(gridPass);
    }

    private (List<MonoBehaviour> opaqueEffects, List<MonoBehaviour> finalEffects, List<MonoBehaviour> allEffects, GuiLayer guiLayer) GatherImageEffects(Camera camera, bool isSceneView)
    {
        var opaqueEffects = new List<MonoBehaviour>();
        var finalEffects = new List<MonoBehaviour>();
        var allEffects = new List<MonoBehaviour>();
        GuiLayer guiLayer = null;

        if (camera == null)
            return (opaqueEffects, finalEffects, allEffects, guiLayer);

        IEnumerable<MonoBehaviour> components = camera.GetComponents<MonoBehaviour>();

        // If this is Scene view camera, also include effects from the Main camera
        if (isSceneView && Camera.Main != null && Camera.Main != camera)
        {
            var mainComponents = Camera.Main.GetComponents<MonoBehaviour>();
            components = System.Linq.Enumerable.Concat(components, mainComponents);
        }

        foreach (MonoBehaviour component in components)
        {
            if (component == null || !component.EnabledInHierarchy)
                continue;

            Type type = component.GetType();

            // Check for GuiLayer
            if (component is GuiLayer gui)
            {
                guiLayer = gui;
                continue;
            }

            // If this is Scene view camera, the effect needs the ImageEffectAllowedInSceneView attribute
            if (isSceneView)
            {
                if (type.GetCustomAttributes(typeof(ImageEffectAllowedInSceneViewAttribute), false).Length == 0)
                    continue;
            }

            // Check if they have OnRenderImage method
            MethodInfo method = type.GetMethod("OnRenderImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null || method.DeclaringType == typeof(MonoBehaviour))
                continue;

            allEffects.Add(component);

            // Check if this is an opaque effect
            if (type.GetCustomAttributes(typeof(ImageEffectOpaqueAttribute), false).Length > 0)
                opaqueEffects.Add(component);
            else
                finalEffects.Add(component);
        }

        return (opaqueEffects, finalEffects, allEffects, guiLayer);
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        var camera = renderingData.CameraData.Camera;
        
        if (camera != null && _cameraDataCache.TryGetValue(camera, out PerCameraData perCameraData))
        {
            if (perCameraData.ColorTarget != null)
            {
                Framebuffer finalTarget = renderingData.CameraTarget ?? Graphics.ScreenTarget;

                if (finalTarget != perCameraData.ColorTarget.Framebuffer)
                {
                    var cmd = CommandBufferPool.Get("Final Blit");
                    try
                    {
                        cmd.SetRenderTarget(finalTarget);
                        cmd.SetViewports(0, 0, perCameraData.Width, perCameraData.Height, 0, 1);
                        cmd.Blit(perCameraData.ColorTarget, finalTarget);
                        context.ExecuteCommandBuffer(cmd);
                    }
                    finally
                    {
                        CommandBufferPool.Release(cmd);
                    }
                }
            }
        }

        // Execute GUI rendering
        if (_guiLayer != null)
        {
            Framebuffer target = renderingData.CameraTarget ?? Graphics.ScreenTarget;
            _guiLayer.ExecuteGUI(target);
        }

        // Call OnPostRender for all effects
        if (_allEffects != null)
        {
            foreach (MonoBehaviour effect in _allEffects)
            {
                if (effect != null)
                    effect.OnPostRender(camera);
            }
        }

        // Clear stored references
        _allEffects = null;
        _guiLayer = null;

        base.Cleanup(context, ref renderingData);
    }

    public override void Dispose()
    {
        CleanupStaticResources();
        base.Dispose();
    }

    public override void CleanupCamera(Camera camera)
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
        SkyboxPass.CleanupStaticResources();
    }

    private static void SetupGlobalUniforms(CameraData cameraData)
    {
        // Camera
        PropertyState.SetGlobalVector("_WorldSpaceCameraPos", cameraData.WorldSpaceCameraPos);
        bool flippedy = !Graphics.IsOpenGL && !Graphics.IsVulkan;
        PropertyState.SetGlobalVector("_ProjectionParams", new Vector4(flippedy ? -1.0f : 1.0f, cameraData.NearClipPlane, cameraData.FarClipPlane, 1.0f / cameraData.FarClipPlane));
        PropertyState.SetGlobalVector("_ScreenParams", new Vector4(cameraData.PixelWidth, cameraData.PixelHeight, 1.0f + 1.0f / cameraData.PixelWidth, 1.0f + 1.0f / cameraData.PixelHeight));

        // Time
        PropertyState.SetGlobalVector("_Time", new Vector4(Time.time / 20, Time.time, Time.time * 2, Time.time * 3));
        PropertyState.SetGlobalVector("_SinTime", new Vector4((float)Math.Sin(Time.time / 8), (float)Math.Sin(Time.time / 4), (float)Math.Sin(Time.time / 2), (float)Math.Sin(Time.time)));
        PropertyState.SetGlobalVector("_CosTime", new Vector4((float)Math.Cos(Time.time / 8), (float)Math.Cos(Time.time / 4), (float)Math.Cos(Time.time / 2), (float)Math.Cos(Time.time)));
        float invDt = Time.deltaTime > 0 ? 1.0f / (float)Time.deltaTime : 0f;
        float invSdt = Time.smoothDeltaTime > 0 ? 1.0f / (float)Time.smoothDeltaTime : 0f;
        PropertyState.SetGlobalVector("prowl_DeltaTime", new Vector4((float)Time.deltaTime, invDt, (float)Time.smoothDeltaTime, invSdt));

        // Fog & Ambient — guard against null scene
        var scene = SceneManagement.SceneManager.Scene;
        if (scene == null)
            return;

        Scene.FogParams fog = scene.Fog;
        float fogRange = (float)(fog.End - fog.Start);
        float invFogRange = Math.Abs(fogRange) > 1e-6f ? -1.0f / fogRange : 0f;
        float fogEndFactor = Math.Abs(fogRange) > 1e-6f ? (float)fog.End / fogRange : 0f;
        Vector4 fogParams = new Vector4(
            fog.Density / (float)Math.Sqrt(0.693147181), // ln(2)
            fog.Density / 0.693147181f, // ln(2)
            invFogRange,
            fogEndFactor
        );
        PropertyState.SetGlobalVector("prowl_FogColor", fog.Color);
        PropertyState.SetGlobalVector("prowl_FogParams", fogParams);
        PropertyState.SetGlobalVector("prowl_FogStates", new System.Numerics.Vector3(
            fog.Mode == Scene.FogParams.FogMode.Linear ? 1 : 0,
            fog.Mode == Scene.FogParams.FogMode.Exponential ? 1 : 0,
            fog.Mode == Scene.FogParams.FogMode.ExponentialSquared ? 1 : 0
        ));

        // Ambient Lighting
        Scene.AmbientLightParams ambient = scene.Ambient;
        PropertyState.SetGlobalVector("prowl_AmbientMode", new Vector2(
            ambient.Mode == Scene.AmbientLightParams.AmbientMode.Uniform ? 1 : 0,
            ambient.Mode == Scene.AmbientLightParams.AmbientMode.Hemisphere ? 1 : 0
        ));

        PropertyState.SetGlobalVector("prowl_AmbientColor", ambient.Color);
        PropertyState.SetGlobalVector("prowl_AmbientSkyColor", ambient.SkyColor);
        PropertyState.SetGlobalVector("prowl_AmbientGroundColor", ambient.GroundColor);
    }

    private class PerCameraData
    {
        public RenderTexture ColorTarget;
        public RenderTexture DepthTarget;
        public int Width;
        public int Height;
        public PixelFormat Format;
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
