// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

namespace Prowl.Runtime.Rendering;

public abstract class RenderFeature : EngineObject
{
    public string FeatureName { get; protected set; } = "Unnamed Feature";
    public bool Enabled { get; set; } = true;

    public virtual void Create() { }

    public virtual void Dispose() { }

    public abstract void AddRenderPasses(ScriptableRenderContext context, ref SRPRenderingData renderingData);

    public virtual bool Validate() => true;

    public override void OnValidate() { }
}
