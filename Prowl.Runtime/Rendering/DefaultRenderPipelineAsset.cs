// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Prowl.Echo;
using Prowl.Runtime.Rendering;
using Prowl.Runtime.Utils;

namespace Prowl.Runtime.Rendering;

[CreateAssetMenu("Rendering/Default Render Pipeline")]
public class DefaultRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool _useDepthPrepass = true;

    [SerializeField]
    private bool _useHDR = true;

    [SerializeField]
    private int _maxLights = 16;

    [SerializeField]
    private int _shadowAtlasSize = 4096;

    [SerializeField]
    private bool _useMotionVectors = false;

    public bool UseDepthPrepass => _useDepthPrepass;
    public bool UseHDR => _useHDR;
    public int MaxLights => _maxLights;
    public int ShadowAtlasSize => _shadowAtlasSize;
    public bool UseMotionVectors => _useMotionVectors;

    public override ForwardRenderer CreateRenderer()
    {
        var settings = new PipelineSettings
        {
            UseHDR = _useHDR,
            MaxLights = _maxLights,
            ShadowAtlasSize = _shadowAtlasSize,
            UseDepthPrepass = _useDepthPrepass,
            UseMotionVectors = _useMotionVectors
        };

        return new ForwardRenderer(settings);
    }

    public override PipelineSettings GetDefaultSettings()
    {
        return new PipelineSettings
        {
            UseHDR = _useHDR,
            MaxLights = _maxLights,
            ShadowAtlasSize = _shadowAtlasSize,
            UseDepthPrepass = _useDepthPrepass,
            UseMotionVectors = _useMotionVectors
        };
    }

    public override void OnValidate()
    {
        base.OnValidate();

        bool changed = _settings.UseHDR != _useHDR ||
                       _settings.MaxLights != _maxLights ||
                       _settings.ShadowAtlasSize != _shadowAtlasSize ||
                       _settings.UseDepthPrepass != _useDepthPrepass ||
                       _settings.UseMotionVectors != _useMotionVectors;

        _settings.UseHDR = _useHDR;
        _settings.MaxLights = _maxLights;
        _settings.ShadowAtlasSize = _shadowAtlasSize;
        _settings.UseDepthPrepass = _useDepthPrepass;
        _settings.UseMotionVectors = _useMotionVectors;

        if (changed)
            InvalidateSharedRenderer();
    }
}
