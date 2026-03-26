// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Prowl.Runtime.Rendering.Pipelines;

namespace Prowl.Runtime.Rendering;

public class SRPRenderingData
{
    public CameraData CameraData;

    public CullingResults CullingResults;

    public SRPLightData LightData;

    public SRPShadowData ShadowData;

    public SRPPostProcessingData PostProcessingData;

    public bool IsSceneViewCamera;
    public bool DisplayGrid;
    public bool DisplayGizmo;
    public Matrix4x4 GridMatrix;
    public Color GridColor;
    public Vector3 GridSizes;

    public static SRPRenderingData Create(Camera camera, bool isSceneView = false)
    {
        var data = new SRPRenderingData
        {
            CameraData = CameraData.Create(camera),
            IsSceneViewCamera = isSceneView,
            LightData = new SRPLightData(),
            ShadowData = new SRPShadowData(),
            PostProcessingData = new SRPPostProcessingData()
        };
        return data;
    }
}

public class SRPLightData
{
    public List<IRenderableLight> VisibleLights;
    public Vector3 SunDirection;
    public int MainLightIndex;

    public SRPLightData()
    {
        VisibleLights = new List<IRenderableLight>();
        SunDirection = Vector3.up;
        MainLightIndex = -1;
    }
}

public class SRPShadowData
{
    public RenderTexture ShadowAtlas;
    public int ShadowAtlasSize;
    public List<ShadowSliceData> ShadowSlices;

    public SRPShadowData()
    {
        ShadowAtlasSize = 4096;
        ShadowSlices = new List<ShadowSliceData>();
    }
}

public class ShadowSliceData
{
    public Matrix4x4 ViewMatrix;
    public Matrix4x4 ProjectionMatrix;
    public int AtlasX;
    public int AtlasY;
    public int AtlasWidth;
    public int AtlasHeight;
}

public class SRPPostProcessingData
{
    public List<MonoBehaviour> OpaqueEffects;
    public List<MonoBehaviour> FinalEffects;
    public bool IsHDR;

    public SRPPostProcessingData()
    {
        OpaqueEffects = new List<MonoBehaviour>();
        FinalEffects = new List<MonoBehaviour>();
        IsHDR = true;
    }
}
