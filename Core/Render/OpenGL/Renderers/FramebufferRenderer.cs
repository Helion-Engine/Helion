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
    private readonly StaticVertexBuffer<FramebufferVertex> m_vbo = new("Framebuffer");
    private readonly VertexArrayObject m_vao = new("Framebuffer");
    private readonly FramebufferProgram m_program = new();
    private bool m_disposed;

    public FramebufferRenderer()
    {
        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
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

        m_vbo.Bind();
        m_vbo.Add(topLeft, bottomLeft, topRight);
        m_vbo.Add(topRight, bottomLeft, bottomRight);
        m_vbo.Upload();
        m_vbo.Unbind();
    }

    public static void ClearWithViewport(Dimension dimension)
    {
        (float a, float r, float g, float b) = Color.Black.Normalized;
        GL.Viewport(0, 0, dimension.Width, dimension.Height);
        GL.ClearColor(r, g, b, a);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
    }

    public void Render(GLFramebuffer buffer, mat4 mvp)
    {
        m_program.Bind();

        GL.ActiveTexture(BindTextures.BoundTexture);
        buffer.ColorAttachment0.Bind();
        m_program.BoundTexture(BindTextures.BoundTexture);
        m_program.Mvp(mvp);

        m_vao.Bind();
        m_vbo.DrawArrays();
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
