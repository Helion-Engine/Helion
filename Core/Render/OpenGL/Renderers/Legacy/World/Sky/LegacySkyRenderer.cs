using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky
{
    public class LegacySkyRenderer : IDisposable
    {
        public readonly ISkyComponent DefaultSky;
        private readonly IGLFunctions gl;
        private readonly List<ISkyComponent> m_skyComponents;
        
        public LegacySkyRenderer(Config config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
            IGLFunctions functions, LegacyGLTextureManager textureManager)
        {
            gl = functions;
            
            DefaultSky = new SkySphereComponent(config, archiveCollection, capabilities, functions, textureManager);
            m_skyComponents = new List<ISkyComponent> { DefaultSky };
        }

        ~LegacySkyRenderer()
        {
            Fail($"Did not dispose of {GetType().FullName}, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }
        
        public void Clear()
        {
            for (int i = 0; i < m_skyComponents.Count; i++)
                m_skyComponents[i].Clear();
        }

        public void Render(RenderInfo renderInfo)
        {
//            glEnable(GL_STENCIL_TEST);
//            glClearStencil(0x00);
//            glStencilMask(0xFF);
//            glStencilOp(GL_KEEP, GL_KEEP, GL_REPLACE);

            for (int i = 0; i < m_skyComponents.Count; i++)
            {
                ISkyComponent sky = m_skyComponents[i];
                if (!sky.HasGeometry)
                    continue;

//                glColorMask(false, false, false, false);
//                glStencilFunc(GL_ALWAYS, skyIndex, 0xFF);

                sky.RenderWorldGeometry(renderInfo);

//                glColorMask(true, true, true, true);
//                glStencilFunc(GL_EQUAL, skyIndex, 0xFF);
//                glDisable(GL_DEPTH_TEST);

                sky.RenderSky(renderInfo);

//                glEnable(GL_DEPTH_TEST);
            }

//            glDisable(GL_STENCIL_TEST);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_skyComponents.ForEach(sky => sky.Dispose());
        }
    }
}