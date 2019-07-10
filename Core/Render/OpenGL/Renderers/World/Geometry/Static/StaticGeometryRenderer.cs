using System;
using System.Linq;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared.World;
using Helion.Util.Extensions;
using MoreLinq;
using OpenTK;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class StaticGeometryRenderer : IDisposable
    {
        private static VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexFloatAttribute("pos", 0, 3),
            new VertexFloatAttribute("uv", 1, 2),
            new VertexFloatAttribute("lightLevel", 2, 1)
        );
        
        private readonly GLTextureManager m_textureManager;
        private readonly VertexArrayObject m_vao;
        private readonly DynamicVertexBufferObject<StaticWorldVertex> m_vbo;
        private readonly StaticWorldShaderProgram m_shaderProgram;

        public StaticGeometryRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
            m_textureManager = textureManager;
            m_vao = new VertexArrayObject(capabilities, Attributes, "Static World VAO");
            m_vbo = new DynamicVertexBufferObject<StaticWorldVertex>(capabilities, m_vao, "Static World VBO");
            m_shaderProgram = StaticWorldShaderProgram.MakeShaderProgram(Attributes);
        }

        ~StaticGeometryRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void AddLine(LineTriangles triangles)
        {
            triangles.SideTriangles.SelectMany(side => side.Walls).ForEach(AddToVbo);
        }

        public void Render(Matrix4 mvp)
        {
            m_shaderProgram.BindAnd(() =>
            {
                m_shaderProgram.Mvp.Set(mvp);
                m_shaderProgram.TextureAtlas.Set(GLConstants.TextureAtlasUnit.ToIndex());

                m_textureManager.BindAnd(() =>
                {
                    m_vao.BindAnd(() =>
                    {
                        m_vbo.BindAnd(() =>
                        {
                            if (m_vbo.NeedsUpload)
                                m_vbo.Upload();
                            
                            m_vbo.DrawArrays();
                        });
                    });
                });
            });
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void AddToVbo(WallQuad quad)
        {
            float unitLightLevel = quad.Side.Sector.UnitLightLevel;
            
            StaticWorldVertex topLeft = new StaticWorldVertex(quad.TopLeft.Position, quad.TopLeft.UV, unitLightLevel);
            StaticWorldVertex topRight = new StaticWorldVertex(quad.TopRight.Position, quad.TopRight.UV, unitLightLevel);
            StaticWorldVertex bottomLeft = new StaticWorldVertex(quad.BottomLeft.Position, quad.BottomLeft.UV, unitLightLevel);
            StaticWorldVertex bottomRight = new StaticWorldVertex(quad.BottomRight.Position, quad.BottomRight.UV, unitLightLevel);

            m_vbo.Add(topLeft, bottomLeft, topRight);
            m_vbo.Add(topRight, bottomLeft, bottomRight);
        }
        
        private void ReleaseUnmanagedResources()
        {
            m_vbo.Dispose();
            m_vao.Dispose();
            m_shaderProgram.Dispose();
        }
    }
}