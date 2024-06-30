using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Framebuffer;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Vertex;
using Helion.Util.Configs;
using Helion.Window;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Renderers;

public readonly struct FramebufferVertex
{
    [VertexAttribute]
    public readonly Vec2F Pos;
    
    [VertexAttribute]
    public readonly Vec2F UV;

    public FramebufferVertex(Vec2F pos, Vec2F uv)
    {
        Pos = pos;
        UV = uv;
    }
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

    public void BoundTexture(TextureUnit unit) => Uniforms.Set(unit, m_boundTextureLocation);
    public void Mvp(mat4 mvp) => Uniforms.Set(mvp, m_mvpLocation);

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
    public GLFramebuffer Framebuffer { get; private set; }
    private readonly IConfig m_config;
    private readonly IWindow m_window;
    private readonly StaticVertexBuffer<FramebufferVertex> m_vbo = new("Framebuffer");
    private readonly VertexArrayObject m_vao = new("Framebuffer");
    private readonly FramebufferProgram m_program = new();
    private bool m_disposed;

    public FramebufferRenderer(IConfig config, IWindow window, Dimension dimension)
    {
        m_config = config;
        m_window = window;

        Framebuffer = CreateFramebuffer(dimension);
        Attributes.BindAndApply(m_vbo, m_vao, m_program.Attributes);
        UploadVertices();
    }

    ~FramebufferRenderer()
    {
        Dispose(false);
    }

    private static GLFramebuffer CreateFramebuffer(in Dimension dimension) =>
        new("Virtual", dimension, 1, RenderbufferStorage.Depth32fStencil8);

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

    public void UpdateToDimensionIfNeeded(Dimension dimension)
    {
        if (Framebuffer.Dimension == dimension || !dimension.HasPositiveArea)
            return;

        Framebuffer.Dispose();
        Framebuffer = CreateFramebuffer(dimension);
    }

    private mat4 CalculateMvp()
    {
        // We already draw to the unit plane, which means instead of doing a bunch
        // of orthographic stuff, we can instead scale the X axis to add black bars
        // depending on whether we want stretched or widescreen.
        if (m_config.Window.Virtual.Stretch)
            return mat4.Identity;

        // How much we stretch depends on the window resolution, and the virtual
        // dimension's resolution. Also don't let it be larger than the NDC box.
        // Since our vertices are in NDC coordinates, 1.0 is the max we can go.
        Dimension windowDim = m_window.Dimension;
        Dimension textureDim = Framebuffer.Textures[0].Dimension;
        float scaleX = Math.Min(textureDim.AspectRatio / windowDim.AspectRatio, 1.0f);
        
        return mat4.Scale(scaleX, 1.0f, 1.0f);
    }

    public void Render()
    {
        mat4 mvp = CalculateMvp();
        (float a, float r, float g, float b) = Color.Black.Normalized;

        GL.Viewport(0, 0, m_window.Dimension.Width, m_window.Dimension.Height);
        GL.ClearColor(r, g, b, a);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

        m_program.Bind();

        GL.ActiveTexture(TextureUnit.Texture0);
        Framebuffer.Textures[0].Bind();
        m_program.BoundTexture(TextureUnit.Texture0);
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
        Framebuffer.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
