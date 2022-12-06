using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky;

public class LegacySkyRenderer : IDisposable
{
    private const int MaxSkyTextures = 255;

    private readonly IConfig m_config;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly Dictionary<int, ISkyComponent> m_skyComponents = new();
    private readonly List<ISkyComponent> m_skyComponentsList = new();

    public LegacySkyRenderer(IConfig config, ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_textureManager = textureManager;
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

    public bool GetOrCreateSky(int? textureHandle, bool flipSkyTexture, [NotNullWhen(true)] out ISkyComponent? sky)
    {
        if (m_skyComponents.Count >= MaxSkyTextures)
        {
            sky = null;
            return false;
        }

        textureHandle ??= m_archiveCollection.TextureManager.GetDefaultSkyTexture().Index;

        // This is a hack that is used to make us never have collisions. We will
        // eventually do this right in the new renderer.
        int textureHandleLookup = textureHandle.Value;
        if (flipSkyTexture)
            textureHandleLookup += short.MaxValue;

        if (m_skyComponents.TryGetValue(textureHandleLookup, out sky))
            return true;

        sky = new SkySphereComponent(m_config, m_archiveCollection, m_textureManager, textureHandle.Value, flipSkyTexture);
        m_skyComponents[textureHandleLookup] = sky;
        m_skyComponentsList.Add(sky);
        return true;
    }

    public void Add(SkyGeometryVertex[] data, int length, int? textureHandle, bool flipSkyTexture)
    {
        if (!GetOrCreateSky(textureHandle, flipSkyTexture, out var sky))
            return;

        sky.Add(data, length);
    }

    public void Render(RenderInfo renderInfo)
    {
        if (m_skyComponentsList.Count == 0)
            return;

        GL.Enable(EnableCap.StencilTest);
        GL.StencilMask(0xFF);
        GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);

        int index = 1;
        for (int i = 0; i < m_skyComponentsList.Count; i++)
        {
            ISkyComponent sky = m_skyComponentsList[i];
            if (!sky.HasGeometry)
                continue;

            int stencilIndex = index++;

            GL.Clear(ClearBufferMask.StencilBufferBit);
            GL.ColorMask(false, false, false, false);
            GL.StencilFunc(StencilFunction.Always, stencilIndex, 0xFF);

            sky.RenderWorldGeometry(renderInfo);

            GL.ColorMask(true, true, true, true);
            GL.StencilFunc(StencilFunction.Equal, stencilIndex, 0xFF);
            GL.Disable(EnableCap.DepthTest);

            sky.RenderSky(renderInfo);
                
            GL.Enable(EnableCap.DepthTest);
        }

        GL.Disable(EnableCap.StencilTest);
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
