// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Numerics;

namespace Prowl.Runtime.Rendering;

public static class RenderUtils
{
    public static void SetGlobalCameraMatrices(Matrix4x4 view, Matrix4x4 proj)
    {
        PropertyState.SetGlobalMatrix("prowl_MatV", view.ToFloat());
        PropertyState.SetGlobalMatrix("prowl_MatIV", view.Invert().ToFloat());
        PropertyState.SetGlobalMatrix("prowl_MatP", proj.ToFloat());
        PropertyState.SetGlobalMatrix("prowl_MatVP", (view * proj).ToFloat());
    }
}
