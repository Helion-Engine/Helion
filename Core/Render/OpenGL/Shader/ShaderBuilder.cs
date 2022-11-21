using System;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Shader.Component;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shader;

public class ShaderBuilder : IDisposable
{
    public readonly VertexShaderComponent Vertex;
    public readonly FragmentShaderComponent Fragment;

    public ShaderBuilder(VertexShaderComponent vertex, FragmentShaderComponent fragment)
    {
        Vertex = vertex;
        Fragment = fragment;
    }

    ~ShaderBuilder()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        Vertex.Dispose();
        Fragment.Dispose();
    }
}
