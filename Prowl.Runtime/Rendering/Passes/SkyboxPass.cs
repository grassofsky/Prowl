// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Numerics;

using Prowl.Runtime.Rendering;

namespace Prowl.Runtime.Rendering.Passes;

public class SkyboxPass : RenderPass
{
    private static Material? s_skyboxMaterial;
    private static Mesh? s_skyDomeMesh;

    private RenderTexture? _colorTarget;

    public SkyboxPass()
    {
        Name = "Skybox";
        InjectionPoint = RenderPassEvent.AfterRenderingOpaques;
    }

    public void Setup(RenderTexture colorTarget)
    {
        _colorTarget = colorTarget;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        var cameraData = renderingData.CameraData;

        if (cameraData.ClearFlags != CameraClearFlags.Skybox)
            return;

        if (!EnsureResources())
            return;

        var cmd = CommandBufferPool.Get(Name);

        try
        {
            cmd.SetRenderTarget(_colorTarget);
            cmd.SetViewports(0, 0, (int)cameraData.PixelWidth, (int)cameraData.PixelHeight, 0, 1);

            cmd.SetMaterial(s_skyboxMaterial!);
            
            Matrix4x4 vp = cameraData.OriginViewMatrix * cameraData.ProjectionMatrix;
            cmd.SetMatrix("_Matrix_VP", vp.ToFloat());

            cmd.DrawSingle(s_skyDomeMesh!);

            context.ExecuteCommandBuffer(cmd);
        }
        finally
        {
            CommandBufferPool.Release(cmd);
        }
    }

    private static bool EnsureResources()
    {
        // Load skybox material
        if (s_skyboxMaterial == null || s_skyboxMaterial.IsDestroyed)
        {
            var shader = Application.AssetProvider.LoadAsset<Shader>("Defaults/ProceduralSky.shader");
            if (shader.Res != null)
            {
                s_skyboxMaterial = new Material(shader);
            }
            else
            {
                return false;
            }
        }

        // Load sky dome mesh
        if (s_skyDomeMesh == null || s_skyDomeMesh.IsDestroyed)
        {
            var skyDomeRef = Application.AssetProvider.LoadAsset<GameObject>("Defaults/SkyDome.obj");
            if (skyDomeRef.Res != null)
            {
                GameObject skyDomeModel = skyDomeRef.Res;
                MeshRenderer? renderer = skyDomeModel.GetComponentInChildren<MeshRenderer>(true, true);
                if (renderer != null && renderer.Mesh.Res != null)
                {
                    s_skyDomeMesh = renderer.Mesh.Res;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public static void CleanupStaticResources()
    {
        s_skyboxMaterial = null;
        s_skyDomeMesh = null;
    }
}
