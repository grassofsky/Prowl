// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Reflection;

using Veldrid;

namespace Prowl.Runtime.Rendering.Passes;

public class PostProcessPass : RenderPass
{
    private RenderTexture _colorTarget;
    private List<MonoBehaviour> _effects;
    private bool _isOpaqueEffects;

    public PostProcessPass()
    {
        Name = "Post Process Pass";
        InjectionPoint = RenderPassEvent.AfterRenderingTransparents;
    }

    public void Setup(RenderTexture colorTarget, List<MonoBehaviour> effects, bool isOpaqueEffects = false)
    {
        _colorTarget = colorTarget;
        _effects = effects;
        _isOpaqueEffects = isOpaqueEffects;

        if (isOpaqueEffects)
            InjectionPoint = RenderPassEvent.AfterRenderingOpaquesPostProcess;
        else
            InjectionPoint = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void Execute(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        if (_colorTarget == null || _effects == null || _effects.Count == 0)
            return;

        ApplyEffects(_colorTarget, _effects);
    }

    private void ApplyEffects(RenderTexture source, List<MonoBehaviour> effects)
    {
        if (effects.Count == 0)
            return;

        bool isHDR = source.ColorBuffers != null && source.ColorBuffers.Length > 0 && 
                     source.ColorBuffers[0].Format == PixelFormat.R16_G16_B16_A16_Float;

        RenderTexture sourceBuffer = source;
        
        bool firstEffectIsLDR = effects.Count > 0 && effects[0] != null && 
                                effects[0].GetType().GetCustomAttributes(typeof(ImageEffectTransformsToLDRAttribute), false).Length > 0;
        RenderTexture destBuffer = RenderTexture.GetTemporaryRT(source.Width, source.Height, [isHDR && !firstEffectIsLDR ? PixelFormat.R16_G16_B16_A16_Float : PixelFormat.R8_G8_B8_A8_UNorm]);
        if (firstEffectIsLDR)
            isHDR = false;

        try
        {
            for (int i = 0; i < effects.Count; i++)
            {
                MonoBehaviour effect = effects[i];
                if (effect == null || !effect.EnabledInHierarchy)
                    continue;

                Type type = effect.GetType();
                MethodInfo method = type.GetMethod("OnRenderImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null || method.DeclaringType == typeof(MonoBehaviour))
                    continue;

                if (isHDR && type.GetCustomAttributes(typeof(ImageEffectTransformsToLDRAttribute), false).Length > 0)
                {
                    isHDR = false;
                    RenderTexture.ReleaseTemporaryRT(destBuffer);
                    destBuffer = RenderTexture.GetTemporaryRT(source.Width, source.Height, [PixelFormat.R8_G8_B8_A8_UNorm]);
                }

                effect.OnRenderImage(sourceBuffer, destBuffer);

                (sourceBuffer, destBuffer) = (destBuffer, sourceBuffer);
            }

            if (sourceBuffer != source)
            {
                Graphics.Blit(sourceBuffer, source.Framebuffer);
                RenderTexture.ReleaseTemporaryRT(sourceBuffer);
            }
            
            if (destBuffer != source && destBuffer != null)
            {
                RenderTexture.ReleaseTemporaryRT(destBuffer);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PostProcessPass] Error applying image effect: {ex.Message}");
            if (sourceBuffer != source && sourceBuffer != null)
                RenderTexture.ReleaseTemporaryRT(sourceBuffer);
            if (destBuffer != null && destBuffer != source)
                RenderTexture.ReleaseTemporaryRT(destBuffer);
        }
    }

    public override void Cleanup(ScriptableRenderContext context, ref SRPRenderingData renderingData)
    {
        _colorTarget = null;
        _effects = null;
    }
}
