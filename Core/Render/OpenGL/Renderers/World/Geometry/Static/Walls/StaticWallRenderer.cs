using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Render.Common.Context;
using Helion.Render.Common.World;
using Helion.Render.OpenGL.Pipeline;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Textures.Buffer;
using Helion.World;
using Helion.World.Geometry.Walls;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls;

public class StaticWallRenderer : IDisposable
{
    private readonly GLTextureManager m_textureManager;
    private readonly GLTextureDataBuffer m_textureDataBuffer;
    private readonly RenderPipeline<StaticWallShader, StaticWallVertex> m_pipeline;
    private readonly Dictionary<Wall, int> m_wallToVboOffset = new();
    private bool m_disposed;

    public StaticWallRenderer(GLTextureManager textureManager, GLTextureDataBuffer textureDataBuffer)
    {
        m_textureManager = textureManager;
        m_textureDataBuffer = textureDataBuffer;
        m_pipeline = new("Static geometry (walls)", BufferUsageHint.DynamicDraw, PrimitiveType.Triangles);
    }

    ~StaticWallRenderer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void UpdateTo(IWorld world)
    {
        m_pipeline.Clear();
        m_wallToVboOffset.Clear();

        foreach (Wall wall in world.Walls)
            AddWall(wall);
    }

    private void AddWall(Wall wall)
    {
        var vbo = m_pipeline.Vbo;
        m_wallToVboOffset[wall] = vbo.Count;

        // Need to convert to using the newer version of textures.
        throw new NotImplementedException("TODO");
        // Texture texture = TextureManager.Instance.GetTexture(wall.TextureHandle);
        // GLTextureHandle textureHandle = m_textureManager.Get(texture);
        // WallTriangulation wallTriangulation = WallTriangulation.From(wall, textureHandle);
        // StaticWallQuad wallQuad = new(wallTriangulation);
        //
        // vbo.AddTriangle(wallQuad.TopLeft, wallQuad.BottomLeft, wallQuad.TopRight);
        // vbo.AddTriangle(wallQuad.TopRight, wallQuad.BottomLeft, wallQuad.BottomRight);
    }

    public void Render(WorldRenderContext context)
    {
        GL.ActiveTexture(TextureUnit.Texture0);
        // Note: For now we assume everything is on one texture atlas.
        m_textureManager.GetAtlas(0).Bind();

        GL.ActiveTexture(TextureUnit.Texture1);
        m_textureDataBuffer.Texture.Bind();

        m_pipeline.Draw(shader =>
        {
            mat4 mvp = ViewMath.Mvp(context);

            shader.Mvp.Set(mvp);
            shader.Tex.Set(TextureUnit.Texture0);
            shader.Data.Set(TextureUnit.Texture1);
        });

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        m_pipeline.Dispose();

        m_disposed = true;
    }
}
