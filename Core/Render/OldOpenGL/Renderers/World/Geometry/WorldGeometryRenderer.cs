using System;
using Helion.Maps.Geometry.Lines;
using Helion.Render.OldOpenGL.Renderers.World.Geometry.Dynamic;
using Helion.Render.OldOpenGL.Renderers.World.Geometry.Static;
using Helion.Render.OldOpenGL.Texture;
using Helion.Render.OldOpenGL.Util;
using Helion.Render.Shared;
using Helion.Render.Shared.World;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Images;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
using MoreLinq;
using OpenTK;

namespace Helion.Render.OldOpenGL.Renderers.World.Geometry
{
    public class WorldGeometryRenderer : IDisposable
    {
        private readonly Config m_config;
        private readonly GLTextureManager m_textureManager;
        private readonly StaticGeometryRenderer m_staticGeometryRenderer;
        private readonly DynamicWorldGeometryRenderer m_dynamicGeometryRenderer;
        private readonly ArchiveCollection m_archiveCollection;

        public WorldGeometryRenderer(Config config, GLCapabilities capabilities, ArchiveCollection archiveCollection,
            GLTextureManager textureManager)
        {
            m_config = config;
            m_textureManager = textureManager;
            m_staticGeometryRenderer = new StaticGeometryRenderer(capabilities, textureManager);
            m_dynamicGeometryRenderer = new DynamicWorldGeometryRenderer(capabilities, textureManager);
            m_archiveCollection = archiveCollection;
        }

        ~WorldGeometryRenderer()
        {
            ReleaseUnmanagedResources();
        }
        
        public void Render(RenderInfo renderInfo)
        {
            float fovX = OpenTK.MathHelper.DegreesToRadians((float)m_config.Engine.Render.FieldOfView);
            Matrix4 mvp = GLRenderer.CreateMVP(renderInfo, fovX);
            
            m_staticGeometryRenderer.Render(mvp);
            m_dynamicGeometryRenderer.Render(mvp);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        internal void UpdateToWorld(WorldBase world)
        {
            // When updating to a new world, we want to batch image retrieval
            // such that we toss out any intermediate textures in the process
            // of making new ones. By limiting this to the current scope, any
            // such textures will be GC'd shortly after and save us memory.
            IImageRetriever imageRetriever = new ArchiveImageRetriever(m_archiveCollection);

            foreach (Subsector subsector in world.BspTree.Subsectors)
                m_staticGeometryRenderer.AddSubsector(WorldTriangulator.Triangulate(subsector, TextureFlatFinder));
            foreach (Line line in world.Map.Lines)
                m_staticGeometryRenderer.AddLine(WorldTriangulator.Triangulate(line, TextureWallFinder));

            Dimension TextureWallFinder(CIString name)
            {
                return m_textureManager.GetWallTexture(name, imageRetriever).Dimension;
            }

            Dimension TextureFlatFinder(CIString name)
            {
                return m_textureManager.GetFlatTexture(name, imageRetriever).Dimension;
            }
        }

        private void ReleaseUnmanagedResources()
        {
            m_staticGeometryRenderer.Dispose();
            m_dynamicGeometryRenderer.Dispose();
        }
    }
}