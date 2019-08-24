using System;
using System.Drawing;
using GlmSharp;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Vertex;
using Helion.Render.OpenGL.Vertex.Attribute;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.Hud
{
    public class LegacyHudRenderer : HudRenderer
    {
        public static readonly VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexPointerFloatAttribute("pos", 0, 3),
            new VertexPointerFloatAttribute("uv", 1, 2),
            new VertexPointerFloatAttribute("alpha", 2, 1));
        
        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly VertexArrayObject m_vao;
        private readonly StreamVertexBuffer<HudVertex> m_vbo;
        private readonly LegacyHudShader m_shaderProgram;

        public LegacyHudRenderer(GLCapabilities capabilities, IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            gl = functions;
            m_textureManager = textureManager;
            m_vao = new VertexArrayObject(capabilities, functions, Attributes, "VAO: Hud renderer");
            m_vbo = new StreamVertexBuffer<HudVertex>(capabilities, functions, m_vao, "VBO: Hud renderer");
            
            using (ShaderBuilder shaderBuilder = LegacyShader.MakeBuilder(functions))
                m_shaderProgram = new LegacyHudShader(functions, shaderBuilder, Attributes);
        }

        ~LegacyHudRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public override void Clear()
        {
            m_vbo.Clear();
        }

        public override void AddImage(CIString textureName, Rectangle drawArea, float alpha)
        {
            GLTexture texture = m_textureManager.Get(textureName, ResourceNamespace.Global);
            AddImage(texture, drawArea, alpha);
        }

        public override void AddImage(CIString textureName, Vec2I topLeft, float alpha)
        {
            GLTexture texture = m_textureManager.Get(textureName, ResourceNamespace.Global);
            Dimension dimension = texture.Dimension;
            Rectangle drawArea = new Rectangle(topLeft.X, topLeft.Y, dimension.Width, dimension.Height);
            AddImage(texture, drawArea, alpha);
        }

        public override void Render()
        {
            m_vbo.UploadIfNeeded();
            
            m_shaderProgram.Bind();
            
            gl.ActiveTexture(TextureUnitType.Zero);
            m_shaderProgram.BoundTexture.Set(gl, 0);
            m_shaderProgram.Mvp.Set(gl, CreateMvp());
            
            m_vao.Bind();
            m_vbo.DrawArrays();
            m_vao.Unbind();
            
            m_shaderProgram.Unbind();
        }

        public override void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void AddImage(GLTexture texture, Rectangle drawArea, float alpha)
        {
            // TODO
        }

        private mat4 CreateMvp()
        {
            // TODO
            return mat4.Identity;
        }

        private void ReleaseUnmanagedResources()
        {
            m_vao.Dispose();
            m_vbo.Dispose();
            m_shaderProgram.Dispose();
        }
    }
}