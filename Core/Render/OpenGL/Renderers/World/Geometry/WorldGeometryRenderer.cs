using System;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Renderers.World.Geometry.Static;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.World;

namespace Helion.Render.OpenGL.Renderers.World.Geometry
{
    public class WorldGeometryRenderer : IDisposable
    {
        private readonly GLTextureManager m_textureManager;
        private readonly StaticGeometryRenderer m_staticGeometryRenderer;
        private readonly TextureBufferObject<WallData> m_wallDataBuffer;

        public WorldGeometryRenderer(GLCapabilities capabilities, GLTextureManager textureManager)
        {
            m_textureManager = textureManager;
            m_staticGeometryRenderer = new StaticGeometryRenderer(capabilities, textureManager);
            m_wallDataBuffer = new TextureBufferObject<WallData>(capabilities, "WallData Texture Buffer");
        }

        ~WorldGeometryRenderer()
        {
            ReleaseUnmanagedResources();
        }
        
        public void Render(WorldBase world)
        {
            m_staticGeometryRenderer.Render(world);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_staticGeometryRenderer.Dispose();
        }
    }
}