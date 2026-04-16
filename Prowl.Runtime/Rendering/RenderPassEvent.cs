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
    OnRenderingOpaques = 275,
    AfterRenderingOpaques = 300,
    AfterRenderingOpaquesPostProcess = 310,
    BeforeRenderingSkybox = 350,
    OnRenderingSkybox = 375,
    AfterRenderingSkybox = 400,
    BeforeRenderingTransparents = 450,
    OnRenderingTransparents = 475,
    AfterRenderingTransparents = 500,
    BeforeRenderingPostProcessing = 550,
    AfterRenderingPostProcessing = 600,
    AfterRendering = 1000
}
