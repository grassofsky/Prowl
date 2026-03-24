// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Numerics;

using Veldrid;

namespace Prowl.Runtime.Rendering;

[Flags]
public enum SortingCriteria
{
    None = 0,
    SortingLayer = 1,
    RenderQueue = 2,
    BackToFront = 4,
    FrontToBack = 8,
    OptimizeStateChanges = 16,
    Default = SortingLayer | RenderQueue | OptimizeStateChanges
}

public enum PerObjectData
{
    None = 0,
    MotionVectors = 1,
    LightIndices = 2,
    LightProbe = 4
}

public struct SortingSettings
{
    public SortingCriteria Criteria;
    public Vector3 CameraPosition;

    public SortingSettings(Camera camera)
    {
        Criteria = SortingCriteria.Default;
        CameraPosition = camera?.Transform.position ?? Vector3.zero;
    }

    public SortingSettings(Camera camera, SortingCriteria criteria)
    {
        Criteria = criteria;
        CameraPosition = camera?.Transform.position ?? Vector3.zero;
    }

    public SortingSettings(CameraData cameraData)
    {
        Criteria = SortingCriteria.Default;
        CameraPosition = cameraData?.WorldSpaceCameraPos ?? Vector3.zero;
    }

    public SortingSettings(CameraData cameraData, SortingCriteria criteria)
    {
        Criteria = criteria;
        CameraPosition = cameraData?.WorldSpaceCameraPos ?? Vector3.zero;
    }
}

public struct RenderQueueRange
{
    public int LowerBound;
    public int UpperBound;

    public static RenderQueueRange Opaque => new(0, 2500);
    public static RenderQueueRange Transparent => new(2501, 5000);
    public static RenderQueueRange All => new(0, 5000);

    public RenderQueueRange(int lower, int upper)
    {
        LowerBound = lower;
        UpperBound = upper;
    }

    public bool Contains(int renderQueue) =>
        renderQueue >= LowerBound && renderQueue <= UpperBound;
}

public struct DrawingSettings
{
    public ShaderTagId[] ShaderPassNames;
    public string ShaderPassValue;
    public SortingSettings SortingSettings;
    public PerObjectData PerObjectData;
    public Material OverrideMaterial;

    public DrawingSettings(ShaderTagId shaderPassName, SortingSettings sortingSettings)
    {
        ShaderPassNames = new[] { shaderPassName };
        ShaderPassValue = null;
        SortingSettings = sortingSettings;
        PerObjectData = PerObjectData.None;
        OverrideMaterial = null;
    }

    public DrawingSettings(ShaderTagId[] shaderPassNames, SortingSettings sortingSettings)
    {
        ShaderPassNames = shaderPassNames;
        ShaderPassValue = null;
        SortingSettings = sortingSettings;
        PerObjectData = PerObjectData.None;
        OverrideMaterial = null;
    }

    public void SetShaderPassName(int index, ShaderTagId shaderPassName)
    {
        if (index >= 0 && ShaderPassNames != null && index < ShaderPassNames.Length)
            ShaderPassNames[index] = shaderPassName;
    }

    public void SetShaderPassValue(string value)
    {
        ShaderPassValue = value;
    }

    public void SetOverrideMaterial(Material material)
    {
        OverrideMaterial = material;
    }

    public void SetPerObjectData(PerObjectData perObjectData)
    {
        PerObjectData = perObjectData;
    }
}

public struct FilteringSettings
{
    public RenderQueueRange RenderQueueRange;
    public LayerMask LayerMask;
    public int RenderingLayerMask;
    public SortingCriteria SortingCriteria;

    public FilteringSettings(RenderQueueRange renderQueueRange)
    {
        RenderQueueRange = renderQueueRange;
        LayerMask = LayerMask.Everything;
        RenderingLayerMask = -1;
        SortingCriteria = SortingCriteria.Default;
    }

    public FilteringSettings(RenderQueueRange renderQueueRange, LayerMask layerMask)
    {
        RenderQueueRange = renderQueueRange;
        LayerMask = layerMask;
        RenderingLayerMask = -1;
        SortingCriteria = SortingCriteria.Default;
    }

    public void SetLayerMask(LayerMask layerMask)
    {
        LayerMask = layerMask;
    }

    public void SetRenderingLayerMask(int renderingLayerMask)
    {
        RenderingLayerMask = renderingLayerMask;
    }
}

public struct RenderStateBlock
{
    public bool OverrideBlendState;
    public BlendStateDescription BlendState;

    public bool OverrideDepthState;
    public DepthStencilStateDescription DepthState;

    public bool OverrideRasterizerState;
    public RasterizerStateDescription RasterizerState;

    public static RenderStateBlock Default => new()
    {
        OverrideBlendState = false,
        OverrideDepthState = false,
        OverrideRasterizerState = false
    };
}
