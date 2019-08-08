using System;
using Helion.Maps.Geometry.Lines;
using Helion.Render.OpenGL.Old.Renderers.World.Geometry.Dynamic.Flats;
using Helion.Render.OpenGL.Old.Renderers.World.Geometry.Dynamic.Walls;
using Helion.Render.OpenGL.Old.Texture;
using Helion.Render.OpenGL.Old.Util;
using Helion.Resources.Images;
using Helion.World.Bsp;
using OpenTK;

namespace Helion.Render.OpenGL.Old.Renderers.World.Geometry.Dynamic
{
    public class DynamicWorldGeometryRenderer : IDisposable
    {
//        private readonly DynamicWallRenderer m_wallRenderer;
//        private readonly DynamicFlatRenderer m_flatRenderer;

        public DynamicWorldGeometryRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
//            m_wallRenderer = new DynamicWallRenderer(capabilities, textureManager);
//            m_flatRenderer = new DynamicFlatRenderer(capabilities, textureManager);
        }

        ~DynamicWorldGeometryRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void AddLine(Line line)
        {
//            m_wallRenderer.AddLine(line);
        }

        public void AddSubsector(Subsector subsector, IImageRetriever imageRetriever)
        {
//            m_flatRenderer.AddSubsector(subsector, imageRetriever);
        }

        public void Render(Matrix4 mvp)
        {
//            m_wallRenderer.Render(mvp);
//            m_flatRenderer.Render(mvp);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
//            m_wallRenderer.Dispose();
//            m_flatRenderer.Dispose();
        }
    }
}