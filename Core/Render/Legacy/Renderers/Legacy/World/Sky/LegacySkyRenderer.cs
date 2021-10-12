using System;
using System.Collections.Generic;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Sky;

public class LegacySkyRenderer : IDisposable
{
    private const int MaxSkyTextures = 255;

    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly GLCapabilities m_capabilities;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly IGLFunctions gl;
    private readonly Dictionary<int, ISkyComponent> m_skyComponents = new();

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
        foreach (ISkyComponent skyComponent in m_skyComponents.Values)
        {
            skyComponent.Clear();
            skyComponent.Dispose();
        }

        m_skyComponents.Clear();
    }

    public void Clear()
    {
        foreach (ISkyComponent skyComponent in m_skyComponents.Values)
            skyComponent.Clear();
    }

    public void Add(SkyGeometryVertex[] data, int? textureHandle)
    {
        if (m_skyComponents.Count >= MaxSkyTextures)
            return;

        textureHandle ??= TextureManager.Instance.GetDefaultSkyTexture().Index;

        if (m_skyComponents.TryGetValue(textureHandle.Value, out ISkyComponent? sky))
        {
            sky.Add(data);
        }
        else
        {
            ISkyComponent newSky = new SkySphereComponent(m_config, m_archiveCollection, m_capabilities, gl,
                m_textureManager, textureHandle.Value);
            m_skyComponents[textureHandle.Value] = newSky;
            newSky.Add(data);
        }
    }

    public void Render(RenderInfo renderInfo)
    {
        gl.Enable(EnableType.StencilTest);
        gl.StencilMask(0xFF);
        gl.StencilOp(StencilOpType.Keep, StencilOpType.Keep, StencilOpType.Replace);

        int index = 1;
        foreach (ISkyComponent sky in m_skyComponents.Values)
        {
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
        foreach (ISkyComponent skyComponent in m_skyComponents.Values)
            skyComponent.Dispose();

        m_skyComponents.Clear();
    }
}
