using System;
using GlmSharp;
using Helion.Render.Common;
using Helion.Render.OpenGL.Modern.Renderers.World.Geometry;
using Helion.Render.OpenGL.Modern.Textures;
using Helion.World;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Modern.Renderers.World
{
    public class ModernGLLevelRenderer : IDisposable
    {
        private readonly IWorld m_world;
        private readonly ModernWorldLevelGeometry m_geometry;
        private bool m_disposed;

        public ModernGLLevelRenderer(IWorld world, ModernGLTextureManager textureManager)
        {
            m_world = world;
            m_geometry = new ModernWorldLevelGeometry(world, textureManager);
        }

        ~ModernGLLevelRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void Draw(Camera camera, mat4 mvp)
        {
            if (m_disposed)
                return;

            m_geometry.Draw(camera, mvp);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            m_geometry.Dispose();

            m_disposed = true;
        }
    }
}
