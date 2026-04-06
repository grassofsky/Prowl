// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Collections.Generic;

using Prowl.Runtime;
using Prowl.Runtime.Utils;
using Prowl.Echo;

namespace Prowl.Editor.ProjectSettings;

[FilePath("NuGetSettings.projsetting", FilePathAttribute.Location.EditorSetting)]
public class NuGetProjectSettings : ScriptableSingleton<NuGetProjectSettings>
{
    private readonly object _lock = new();

    [SerializeField]
    private List<PackageReference> _packages = new();

    public IReadOnlyList<PackageReference> Packages
    {
        get
        {
            lock (_lock)
            {
                return _packages.AsReadOnly();
            }
        }
    }

    public void AddPackage(string packageName, string version)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            throw new System.ArgumentException("Package name cannot be null or empty.", nameof(packageName));
        if (string.IsNullOrWhiteSpace(version))
            throw new System.ArgumentException("Version cannot be null or empty.", nameof(version));

        lock (_lock)
        {
            var existing = _packages.FindIndex(p => p.Name == packageName);
            if (existing >= 0)
            {
                _packages[existing] = new PackageReference(packageName, version);
            }
            else
            {
                _packages.Add(new PackageReference(packageName, version));
            }
            Save();
        }
    }

    public bool RemovePackage(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;

        lock (_lock)
        {
            var index = _packages.FindIndex(p => p.Name == packageName);
            if (index >= 0)
            {
                _packages.RemoveAt(index);
                Save();
                return true;
            }
            return false;
        }
    }

    public void ClearPackages()
    {
        lock (_lock)
        {
            _packages.Clear();
            Save();
        }
    }

    public bool HasPackage(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return false;

        lock (_lock)
        {
            return _packages.Exists(p => p.Name == packageName);
        }
    }

    public override void OnValidate()
    {
        lock (_lock)
        {
            _packages ??= new List<PackageReference>();
        }
    }
}

[System.Serializable]
public readonly struct PackageReference : System.IEquatable<PackageReference>
{
    public string Name { get; init; }
    public string Version { get; init; }

    public PackageReference(string name, string version)
    {
        Name = name ?? throw new System.ArgumentNullException(nameof(name));
        Version = version ?? throw new System.ArgumentNullException(nameof(version));
    }

    public bool Equals(PackageReference other) =>
        Name == other.Name && Version == other.Version;

    public override int GetHashCode() =>
        System.HashCode.Combine(Name, Version);

    public override bool Equals(object? obj) =>
        obj is PackageReference other && Equals(other);

    public static bool operator ==(PackageReference left, PackageReference right) =>
        left.Equals(right);

    public static bool operator !=(PackageReference left, PackageReference right) =>
        !left.Equals(right);
}
