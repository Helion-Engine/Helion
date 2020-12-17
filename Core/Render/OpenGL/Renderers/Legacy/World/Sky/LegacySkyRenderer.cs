using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared;
using Helion.Resource.Archives.Collection;
using Helion.Util.Configuration;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky
{
    public class LegacySkyRenderer : IDisposable
    {
        private const int StencilIndex = 1;
       
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
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }
        
        public void Clear()
        {
            for (int i = 0; i < m_skyComponents.Count; i++)
                m_skyComponents[i].Clear();
        }

        public void Render(RenderInfo renderInfo)
        {
            gl.Enable(EnableType.StencilTest);
            gl.StencilMask(0xFF);
            gl.StencilOp(StencilOpType.Keep, StencilOpType.Keep, StencilOpType.Replace);

            for (int i = 0; i < m_skyComponents.Count; i++)
            {
                ISkyComponent sky = m_skyComponents[i];
                if (!sky.HasGeometry)
                    continue;

                gl.Clear(ClearType.StencilBufferBit);
                gl.ColorMask(false, false, false, false);
                gl.StencilFunc(StencilFuncType.Always, StencilIndex, 0xFF);

                sky.RenderWorldGeometry(renderInfo);

                gl.ColorMask(true, true, true, true);
                gl.StencilFunc(StencilFuncType.Equal, StencilIndex, 0xFF);
                gl.Disable(EnableType.DepthTest);

                sky.RenderSky(renderInfo);

                gl.Enable(EnableType.DepthTest);
            }

            gl.Disable(EnableType.StencilTest);
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