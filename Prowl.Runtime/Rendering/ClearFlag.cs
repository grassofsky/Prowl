// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System;

namespace Prowl.Runtime.Rendering;

[Flags]
public enum ClearFlag
{
    None = 0,
    Color = 1,
    Depth = 2,
    Stencil = 4,
    All = Color | Depth | Stencil
}
