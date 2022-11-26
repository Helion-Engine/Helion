using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shader.Component;

public class FragmentShaderComponent : ShaderComponent
{
    public FragmentShaderComponent(string shaderText) : base(shaderText)
    {
    }

    protected override ShaderType GetShaderComponentType() => ShaderType.FragmentShader;
}
