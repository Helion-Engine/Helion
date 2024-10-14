﻿using System;
using System.Diagnostics.CodeAnalysis;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Data;

public class RenderData<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex> : IDisposable where TVertex : struct
{
    public DynamicVertexBuffer<TVertex> Vbo;
    public VertexArrayObject Vao;
    public GLLegacyTexture Texture;
    public DynamicArray<TVertex> ArrayData;
    public int RenderCount;
    private bool m_disposed;
    
    public RenderData(GLLegacyTexture texture, RenderProgram program)
    {
        Texture = texture;
        Vao = new($"Attributes for {texture.Name}");
        Vbo = new($"Vertices for {texture.Name}");
        ArrayData = Vbo.Data;

        Attributes.BindAndApply(Vbo, Vao, program.Attributes);
    }

    ~RenderData()
    {
        Dispose(false);
    }
    
    public void Clear()
    {
        Vbo.Clear();
    }
    
    public void Draw(PrimitiveType primitive)
    {
        if (Vbo.Empty)
            return;
        
        Texture.Bind();
        Vao.Bind();
        Vbo.Bind();

        Vbo.Upload();
        Vbo.DrawArrays(primitive);

        Vbo.Unbind();
        Vao.Unbind();
        Texture.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        Vao.Dispose();
        Vbo.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}