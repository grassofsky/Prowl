// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using Veldrid;

namespace Prowl.Runtime.Rendering;

public readonly struct RenderTargetIdentifier
{
    public readonly RenderTexture RenderTexture;
    public readonly Texture2D Texture2D;
    public readonly Framebuffer Framebuffer;
    public readonly RenderTargetType Type;

    public RenderTargetIdentifier(RenderTexture renderTexture)
    {
        RenderTexture = renderTexture;
        Texture2D = null;
        Framebuffer = null;
        Type = RenderTargetType.RenderTexture;
    }

    public RenderTargetIdentifier(Texture2D texture)
    {
        RenderTexture = null;
        Texture2D = texture;
        Framebuffer = null;
        Type = RenderTargetType.Texture2D;
    }

    public RenderTargetIdentifier(Framebuffer framebuffer)
    {
        RenderTexture = null;
        Texture2D = null;
        Framebuffer = framebuffer;
        Type = RenderTargetType.Framebuffer;
    }

    public static implicit operator RenderTargetIdentifier(RenderTexture rt) => new(rt);
    public static implicit operator RenderTargetIdentifier(Texture2D tex) => new(tex);
    public static implicit operator RenderTargetIdentifier(Framebuffer fb) => new(fb);

    public Framebuffer GetFramebuffer()
    {
        return Type switch
        {
            RenderTargetType.RenderTexture => RenderTexture?.Framebuffer,
            RenderTargetType.Framebuffer => Framebuffer,
            _ => null
        };
    }

    public bool IsValid => Type != RenderTargetType.None;
}

public enum RenderTargetType
{
    None,
    RenderTexture,
    Texture2D,
    Framebuffer
}
