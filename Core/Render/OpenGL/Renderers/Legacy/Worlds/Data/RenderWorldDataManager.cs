using System;
using System.Collections.Generic;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Textures.Legacy;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.Worlds.Data
{
    public class RenderWorldDataManager : IDisposable
    {
        private readonly GLCapabilities m_capabilities;
        private readonly IGLFunctions gl;
        private readonly Dictionary<GLLegacyTexture, RenderWorldData> m_textureToWorldData = new Dictionary<GLLegacyTexture, RenderWorldData>();

        public RenderWorldDataManager(GLCapabilities capabilities, IGLFunctions functions)
        {
            m_capabilities = capabilities;
            gl = functions;
        }

        ~RenderWorldDataManager()
        {
            FailedToDispose(this);
            ReleaseUnmanagedResources();
        }

        public RenderWorldData this[GLLegacyTexture texture]
        {
            get
            {
                if (m_textureToWorldData.TryGetValue(texture, out RenderWorldData? data))
                    return data;

                RenderWorldData newData = new RenderWorldData(m_capabilities, gl, texture);
                m_textureToWorldData[texture] = newData;
                return newData;
            }
        }

        public void Clear()
        {
            m_textureToWorldData.Values.ForEach(geometryData => geometryData.Clear());
        }

        public void Draw()
        {
            m_textureToWorldData.Values.ForEach(geometryData => geometryData.Draw());
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            m_textureToWorldData.Values.ForEach(geometryData => geometryData.Dispose());
        }
    }
}