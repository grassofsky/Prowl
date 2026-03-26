// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.Rendering;

public enum RenderPassEvent
{
    BeforeRendering = 0,
    BeforeRenderingShadows = 50,
    AfterRenderingShadows = 100,
    BeforeRenderingPrepasses = 150,
    AfterRenderingPrepasses = 200,
    BeforeRenderingOpaques = 250,
    AfterRenderingOpaques = 300,
    BeforeRenderingTransparents = 350,
    AfterRenderingTransparents = 400,
    BeforeRenderingPostProcessing = 450,
    AfterRenderingPostProcessing = 500,
    AfterRendering = 1000
}
