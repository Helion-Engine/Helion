using System;
using System.Collections.Generic;
using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World.Sky;
using Helion.Render.Legacy.Renderers.World.Sky.Sphere;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.World.Sky;

public class LegacySkyRenderer : IDisposable
{
    private const int MaxSkyTextures = 255;

    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly GLCapabilities m_capabilities;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly IGLFunctions gl;
    private readonly Dictionary<int, ISkyComponent> m_skyComponents = new();
    private readonly List<ISkyComponent> m_skyComponentsList = new();

    public LegacySkyRenderer(IConfig config, ArchiveCollection archiveCollection, GLCapabilities capabilities,
        IGLFunctions functions, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_capabilities = capabilities;
        m_textureManager = textureManager;
        gl = functions;
    }

    ~LegacySkyRenderer()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public void Reset()
    {
        for (int i = 0; i < m_skyComponentsList.Count; i++)
        {
            m_skyComponentsList[i].Clear();
            m_skyComponentsList[i].Dispose();
        }

        m_skyComponents.Clear();
        m_skyComponentsList.Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < m_skyComponentsList.Count; i++)
            m_skyComponentsList[i].Clear();
    }

    public void Add(SkyGeometryVertex[] data, int length, int? textureHandle, bool flipSkyTexture)
    {
        if (m_skyComponents.Count >= MaxSkyTextures)
            return;

        textureHandle ??= m_archiveCollection.TextureManager.GetDefaultSkyTexture().Index;

        // This is a hack that is used to make us never have collisions. We will
        // eventually do this right in the new renderer.
        int textureHandleLookup = textureHandle.Value;
        if (flipSkyTexture)
            textureHandleLookup += short.MaxValue;

        if (m_skyComponents.TryGetValue(textureHandleLookup, out ISkyComponent? sky))
        {
            sky.Add(data, length);
        }
        else
        {
            ISkyComponent newSky = new SkySphereComponent(m_config, m_archiveCollection, m_capabilities, gl,
                m_textureManager, textureHandle.Value, flipSkyTexture);
            m_skyComponents[textureHandleLookup] = newSky;
            m_skyComponentsList.Add(newSky);
            newSky.Add(data, length);
        }
    }

    public void Render(RenderInfo renderInfo)
    {
        gl.Enable(EnableType.StencilTest);
        gl.StencilMask(0xFF);
        gl.StencilOp(StencilOpType.Keep, StencilOpType.Keep, StencilOpType.Replace);

        int index = 1;
        for (int i = 0; i < m_skyComponentsList.Count; i++)
        {
            ISkyComponent sky = m_skyComponentsList[i];
            if (!sky.HasGeometry)
                continue;

            int stencilIndex = index++;

            gl.Clear(ClearType.StencilBufferBit);
            gl.ColorMask(false, false, false, false);
            gl.StencilFunc(StencilFuncType.Always, stencilIndex, 0xFF);

            sky.RenderWorldGeometry(renderInfo);

            gl.ColorMask(true, true, true, true);
            gl.StencilFunc(StencilFuncType.Equal, stencilIndex, 0xFF);
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
        for (int i = 0; i < m_skyComponentsList.Count; i++)
            m_skyComponentsList[i].Dispose();

        m_skyComponents.Clear();
    }
}
