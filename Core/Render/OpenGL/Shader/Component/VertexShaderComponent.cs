using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shader.Component;

public class VertexShaderComponent : ShaderComponent
{
    public VertexShaderComponent(string shaderText) : base(shaderText)
    {
    }

    protected override ShaderType GetShaderComponentType() => ShaderType.VertexShader;
}
