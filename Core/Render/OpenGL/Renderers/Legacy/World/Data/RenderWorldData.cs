using System;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldData : IDisposable
{
    public readonly GLLegacyTexture Texture;
    public readonly StreamVertexBuffer<DynamicVertex> Vbo;
    public readonly VertexArrayObject Vao;
    public int RenderCount;

    public RenderWorldData(GLLegacyTexture texture, RenderProgram program)
    {
        Texture = texture;
        Vao = new($"Attributes for {texture.Name}");
        Vbo = new($"Vertices for {texture.Name}");

        Attributes.BindAndApply(Vbo, Vao, program.Attributes);
    }

    ~RenderWorldData()
    {
        ReleaseUnmanagedResources();
    }

    public void Clear()
    {
        Vbo.Clear();
    }

    public void Draw()
    {
        if (Vbo.Empty)
            return;

        // We are doing binding manually since apparently these are all
        // coming up in the memory profiler as a bunch of new 'actions'.
        // We don't want GC pressure if there's a lot of textures, since
        // this means we get O(N) actions for N used textures.
        Texture.Bind();
        Vao.Bind();
        Vbo.Bind();

        Vbo.Upload();
        Vbo.DrawArrays();

        Vbo.Unbind();
        Vao.Unbind();
        Texture.Unbind();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        Vbo.Dispose();
        Vao.Dispose();
    }

    public override string ToString()
    {
        return Texture.Name;
    }
}
