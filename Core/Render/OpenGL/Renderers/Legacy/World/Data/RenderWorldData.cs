using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderWorldData : IDisposable
{
    public GLLegacyTexture Texture;
    public GLLegacyTexture? BrightmapTexture;
    public VertexPipeline<DynamicVertex> Pipeline;
    public int RenderCount;

    public RenderWorldData(RenderProgram program)
    {
        Pipeline = new(program, new DynamicVertexBuffer<DynamicVertex>("DynamicVertex"), "DynamicVertex");
        Texture = null!;
    }

    public RenderWorldData(GLLegacyTexture texture, RenderProgram program, GLLegacyTexture? brightmapTexture = null)
    {
        Pipeline = new(program, new DynamicVertexBuffer<DynamicVertex>("DynamicVertex"), "DynamicVertex");
        Texture = null!;
        Set(texture, brightmapTexture);
    }

    public void Set(GLLegacyTexture texture, GLLegacyTexture? brightmapTexture = null)
    {
        Texture = texture;
        BrightmapTexture = brightmapTexture;
    }

    ~RenderWorldData()
    {
        ReleaseUnmanagedResources();
    }

    public void Clear()
    {
        Pipeline.Clear();
    }

    public void Draw()
    {
        if (Pipeline.Empty)
            return;

        // We are doing binding manually since apparently these are all
        // coming up in the memory profiler as a bunch of new 'actions'.
        // We don't want GC pressure if there's a lot of textures, since
        // this means we get O(N) actions for N used textures.
        GL.ActiveTexture(BindTextures.BoundTexture);
        Texture.Bind();
        GL.ActiveTexture(BindTextures.BrightmapTexture);
        if (BrightmapTexture != null)
            BrightmapTexture.Bind();
        else
            GL.BindTexture(TextureTarget.Texture2D, 0);

        Pipeline.Bind(true);
        Pipeline.Vbo.Upload();
        Pipeline.DrawArrays();
        Pipeline.Unbind();
        Texture.Unbind();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private void ReleaseUnmanagedResources()
    {
        Pipeline.Dispose();
    }

    public override string ToString()
    {
        return Texture.Name;
    }
}
