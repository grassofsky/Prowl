// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.Rendering;

public readonly struct ShaderTagId
{
    public readonly string Name;

    public ShaderTagId(string name)
    {
        Name = name ?? string.Empty;
    }

    public static implicit operator ShaderTagId(string name) => new(name);

    public override string ToString() => Name;

    public override bool Equals(object obj)
    {
        if (obj is ShaderTagId other)
            return Name == other.Name;
        if (obj is string str)
            return Name == str;
        return false;
    }

    public override int GetHashCode() => Name?.GetHashCode() ?? 0;

    public static bool operator ==(ShaderTagId left, ShaderTagId right) => left.Equals(right);
    public static bool operator !=(ShaderTagId left, ShaderTagId right) => !left.Equals(right);
}
