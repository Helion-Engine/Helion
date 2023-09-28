using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Programs;

public abstract class RenderProgram : Program
{
    protected RenderProgram(string label) : base(label)
    {
    }

    protected IEnumerable<Shader> CreateShadersOrThrow(string label)
    {
        yield return new Shader(label, ShaderType.VertexShader, VertexShader());

        if (GeometryShader() != null)
            yield return new Shader(label, ShaderType.GeometryShader, GeometryShader());
        
        if (FragmentShader() != null)
            yield return new Shader(label, ShaderType.FragmentShader, FragmentShader());
    }

    protected abstract string VertexShader();
    protected abstract string? GeometryShader();
    protected abstract string? FragmentShader();
}