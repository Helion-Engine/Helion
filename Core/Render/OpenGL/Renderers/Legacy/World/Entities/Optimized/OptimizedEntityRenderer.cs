using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources;
using Helion.Util.Extensions;
using Helion.World.Entities;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities.Optimized;

public class OptimizedEntityRenderer : IDisposable
{
    private readonly StreamVertexBuffer<EntityVertex> m_vbo = new("Entity");
    private readonly VertexArrayObject m_vao = new("Entity");
    private readonly OptimizedEntityProgram m_program = new();
    private readonly GLTexture2D m_texture;
    private Vec2F m_viewRightNormal;
    private bool m_disposed;

    public OptimizedEntityRenderer(GLTextureManager textureManager)
    {
        m_texture = textureManager.NullTexture;

        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
    }

    ~OptimizedEntityRenderer()
    {
        Dispose(false);
    }

    public void Clear()
    {
        m_vbo.Clear();
    }

    public void SetViewDirection(Vec2D viewDir)
    {
        m_viewRightNormal = viewDir.RotateRight90().Unit().Float;
    }

    public void Add(Entity entity, RenderInfo renderInfo)
    {
        Vec3F position = entity.PrevPosition.Float.Interpolate(entity.Position.Float, renderInfo.TickFraction);
        byte lightLevel = (byte)entity.Sector.LightLevel.Clamp(0, 255);

        EntityVertex vertex = new(position, lightLevel);
        m_vbo.Add(vertex);
    }

    public void Render(RenderInfo renderInfo)
    {
        m_program.Bind();

        m_texture.Bind();
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.ViewRightNormal(m_viewRightNormal);
        m_program.Mvp(Renderer.CalculateMvpMatrix(renderInfo));

        m_vao.Bind();
        m_vbo.Bind();
        m_vbo.Upload();

        GL.DrawArrays(PrimitiveType.Points, 0, m_vbo.Count);

        m_vbo.Unbind();
        m_vao.Unbind();

        m_program.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_program.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
