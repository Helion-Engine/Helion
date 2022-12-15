using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities.Optimized;

public class OptimizedEntityRenderer : IDisposable
{
    private readonly StreamVertexBuffer<EntityVertex> m_vbo = new("Entity");
    private readonly VertexArrayObject m_vao = new("Entity");
    private readonly OptimizedEntityProgram m_program = new();
    private readonly LegacyGLTextureManager m_textureManager;
    private GLLegacyTexture m_texture;
    private Entity m_cameraEntity = null!;
    private float m_tickFraction;
    private Vec2F m_viewRightNormal;
    private bool m_disposed;

    public OptimizedEntityRenderer(LegacyGLTextureManager textureManager)
    {
        m_textureManager = textureManager;

        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
    }

    ~OptimizedEntityRenderer()
    {
        Dispose(false);
    }

    public void Clear(float tickFraction, Entity cameraEntity)
    {
        m_tickFraction = tickFraction;
        m_cameraEntity = cameraEntity;

        m_vbo.Clear();

        m_textureManager.TryGetSprite("POSSA1", out m_texture); // Temporary!
    }

    public void SetViewDirection(Vec2D viewDir)
    {
        m_viewRightNormal = viewDir.RotateRight90().Unit().Float;
    }

    public bool ShouldNotDraw(Entity entity)
    {
        //m_EntityDrawnTracker.HasDrawn(entity)
        return entity.Frame.IsInvisible || entity.Flags.Invisible || entity.Flags.NoSector || ReferenceEquals(m_cameraEntity, entity);
    }

    public void RenderSubsector(Sector viewSector, Subsector subsector, in Vec3D position)
    {
        LinkableNode<Entity>? node = subsector.Sector.Entities.Head;
        while (node != null)
        {
            Entity entity = node.Value;
            node = node.Next;

            if (ShouldNotDraw(entity))
                continue;

            //if (entity.Definition.Properties.Alpha < 1)
            //{
            //    entity.RenderDistance = entity.Position.XY.Distance(position.XY);
            //    AlphaEntities.Add(entity);
            //    continue;
            //}

            //RenderEntity(viewSector, entity, position);
            Add(entity);
        }
    }

    public void Add(Entity entity)
    {
        Vec3F position = entity.PrevPosition.Float.Interpolate(entity.Position.Float, m_tickFraction);
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

        m_vbo.Dispose();
        m_vao.Dispose();
        m_program.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
