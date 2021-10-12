using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Shader.Component;

public class VertexShaderComponent : ShaderComponent
{
    public VertexShaderComponent(IGLFunctions functions, string shaderText) : base(functions, shaderText)
    {
    }

    protected override ShaderComponentType GetShaderComponentType() => ShaderComponentType.Vertex;
}
