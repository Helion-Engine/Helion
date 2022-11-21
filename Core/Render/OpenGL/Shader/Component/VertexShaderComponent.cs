using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Shader.Component;

namespace Helion.Render.OpenGL.Shader.Component;

public class VertexShaderComponent : ShaderComponent
{
    public VertexShaderComponent(IGLFunctions functions, string shaderText) : base(functions, shaderText)
    {
    }

    protected override ShaderComponentType GetShaderComponentType() => ShaderComponentType.Vertex;
}
