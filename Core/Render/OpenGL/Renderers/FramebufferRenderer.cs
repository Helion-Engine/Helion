using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Framebuffer;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers;

public readonly struct FramebufferVertex(Vec2F pos, Vec2F uv)
{
    [VertexAttribute]
    public readonly Vec2F Pos = pos;
    
    [VertexAttribute]
    public readonly Vec2F UV = uv;
}

public class FramebufferProgram : RenderProgram
{
    private readonly int m_boundTextureLocation;
    private readonly int m_mvpLocation;

    public FramebufferProgram() : base("Framebuffer")
    {
        m_boundTextureLocation = Uniforms.GetLocation("boundTexture");
        m_mvpLocation = Uniforms.GetLocation("mvp");
    }

    public void BoundTexture(TextureUnit unit) => ProgramUniforms.Set(unit, m_boundTextureLocation);
    public void Mvp(mat4 mvp) => ProgramUniforms.Set(mvp, m_mvpLocation);

    protected override string VertexShader() => @"
        #version 330

        layout(location = 0) in vec2 pos;
        layout(location = 1) in vec2 uv;

        out vec2 uvFrag;

        uniform mat4 mvp;

        void main()
        {
            uvFrag = uv;

            gl_Position = mvp * vec4(pos, 0, 1);
        }
    ";

    protected override string FragmentShader() => @"
        #version 330

        in vec2 uvFrag;

        out vec4 fragColor;

        uniform sampler2D boundTexture;

        void main()
        {
            fragColor = texture(boundTexture, uvFrag.st);
        }
    ";
}

public class FramebufferRenderer : IDisposable
{
    private readonly VertexPipeline<FramebufferVertex> m_pipeline;
    private readonly FramebufferProgram m_program = new();
    private bool m_disposed;

    public FramebufferRenderer()
    {
        m_pipeline = new(m_program, new StaticVertexBuffer<FramebufferVertex>("Framebuffer"), "Framebuffer");
        UploadVertices();
    }

    ~FramebufferRenderer()
    {
        Dispose(false);
    }

    private void UploadVertices()
    {
        FramebufferVertex topLeft = new((-1, 1), (0, 1));
        FramebufferVertex topRight = new((1, 1), (1, 1));
        FramebufferVertex bottomLeft = new((-1, -1), (0, 0));
        FramebufferVertex bottomRight = new((1, -1), (1, 0));

        m_pipeline.Vbo.Bind();
        m_pipeline.Vbo.Add(topLeft, bottomLeft, topRight);
        m_pipeline.Vbo.Add(topRight, bottomLeft, bottomRight);
        m_pipeline.Vbo.Upload();
        m_pipeline.Vbo.Unbind();
    }

    public void Render(GLFramebuffer buffer, mat4 mvp)
    {
        m_program.Bind();

        GL.ActiveTexture(BindTextures.BoundTexture);
        buffer.ColorAttachment0.Bind();
        m_program.BoundTexture(BindTextures.BoundTexture);
        m_program.Mvp(mvp);

        m_pipeline.Bind();
        m_pipeline.DrawArrays();
        m_pipeline.Unbind();

        m_program.Unbind();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_pipeline.Dispose();
        m_program.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
