using System;
using System.Linq;
using Helion.Maps;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared.World;
using Helion.Util;
using Helion.Util.Extensions;
using MoreLinq;
using OpenTK;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static
{
    public class StaticGeometryRenderer : IDisposable
    {
        private static VertexArrayAttributes Attributes = new VertexArrayAttributes(
            new VertexFloatAttribute("pos", 0, 3),
            new VertexFloatAttribute("localUV", 1, 2),
            new VertexFloatAttribute("lightLevel", 2, 1),
            new VertexIntAttribute("textureInfoIndex", 3, 1)
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
        
        public void AddSubsector(SubsectorTriangles triangles)
        {
            AddToVbo(triangles.Floor);
            AddToVbo(triangles.Ceiling);
        }

        public void Render(Matrix4 mvp)
        {
            m_shaderProgram.BindAnd(() =>
            {
                m_shaderProgram.Mvp.Set(mvp);
                m_shaderProgram.TextureAtlas.Set(GLConstants.TextureAtlasUnit.ToIndex());
                m_shaderProgram.TextureInfoBuffer.Set(GLConstants.TextureInfoUnit.ToIndex());

                m_textureManager.BindAnd(() =>
                {
                    m_textureManager.TextureDataBuffer.BindAnd(() =>
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
            });
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Checks if the quad should be a sky because it backs onto a floor or
        /// ceiling that has a sky texture for its flat.
        /// </summary>
        /// <param name="quad">The quad to check.</param>
        /// <returns>True if it should be a sky, false if not.</returns>
        private bool ShouldBeSky(WallQuad quad)
        {
            if (quad.Side.Line.OneSided)
                return false;

            if (quad.Side.PartnerSide == null)
                throw new NullReferenceException("Should not have a null partner side with a two sided line");

            Maps.Geometry.Sector otherSector = quad.Side.PartnerSide.Sector;
            if (quad.SideSection == SideSection.Upper)
                return otherSector.Ceiling.Texture == Constants.SkyTexture;
            if (quad.SideSection == SideSection.Lower)
                return otherSector.Floor.Texture == Constants.SkyTexture;

            return false;
        }

        private void AddToVbo(WallQuad quad)
        {
            GLTexture texture = m_textureManager.GetWallTexture(quad.Texture);
            if (ShouldBeSky(quad))
                texture = m_textureManager.GetFlatTexture(Constants.SkyTexture);
            else if (quad.Texture == Constants.NoTexture)
                return;

            int textureInfoIndex = texture.TextureInfoIndex;
            float unitLightLevel = quad.Side.Sector.UnitLightLevel;
            StaticWorldVertex topLeft = new StaticWorldVertex(quad.TopLeft.Position, quad.TopLeft.UV, unitLightLevel, textureInfoIndex);
            StaticWorldVertex topRight = new StaticWorldVertex(quad.TopRight.Position, quad.TopRight.UV, unitLightLevel, textureInfoIndex);
            StaticWorldVertex bottomLeft = new StaticWorldVertex(quad.BottomLeft.Position, quad.BottomLeft.UV, unitLightLevel, textureInfoIndex);
            StaticWorldVertex bottomRight = new StaticWorldVertex(quad.BottomRight.Position, quad.BottomRight.UV, unitLightLevel, textureInfoIndex);

            m_vbo.Add(topLeft, bottomLeft, topRight);
            m_vbo.Add(topRight, bottomLeft, bottomRight);
        }

        private void AddToVbo(SubsectorFlatFan flatFan)
        {
            float unitLightLevel = flatFan.Sector.UnitLightLevel;
            GLTexture texture = m_textureManager.GetFlatTexture(flatFan.Texture);
            int textureInfoIndex = texture.TextureInfoIndex;
            
            StaticWorldVertex root = new StaticWorldVertex(flatFan.Root.Position, flatFan.Root.UV, unitLightLevel, textureInfoIndex);
                
            flatFan.Fan.Window(2).ForEach(edge =>
            {
                Vertex second = edge.ElementAt(0);
                Vertex third = edge.ElementAt(1);
                StaticWorldVertex secondVertex = new StaticWorldVertex(second.Position, second.UV, unitLightLevel, textureInfoIndex);
                StaticWorldVertex thirdVertex = new StaticWorldVertex(third.Position, third.UV, unitLightLevel, textureInfoIndex);
                
                m_vbo.Add(root, secondVertex, thirdVertex);
            });
        }

        private void ReleaseUnmanagedResources()
        {
            m_vbo.Dispose();
            m_vao.Dispose();
            m_shaderProgram.Dispose();
        }
    }
}