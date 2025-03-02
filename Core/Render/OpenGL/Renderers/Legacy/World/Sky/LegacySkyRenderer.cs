using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky;

public class LegacySkyRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager) : IDisposable
{
    private const int MaxSkyTextures = 255;

    public static readonly Dictionary<int, Image> GeneratedImages = [];

    private readonly ArchiveCollection m_archiveCollection = archiveCollection;
    private readonly LegacyGLTextureManager m_glTextureManager = glTextureManager;
    private readonly Dictionary<SkyKey, ISkyComponent> m_skyComponents = [];
    private readonly List<ISkyComponent> m_skyComponentsList = [];

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
        GeneratedImages.Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < m_skyComponentsList.Count; i++)
            m_skyComponentsList[i].Clear();
    }

    public bool GetOrCreateSky(int? textureHandle, SkyOptions options, Vec2F offset, [NotNullWhen(true)] out ISkyComponent? sky)
    {
        if (m_skyComponents.Count >= MaxSkyTextures)
        {
            sky = null;
            return false;
        }

        textureHandle ??= m_archiveCollection.TextureManager.GetDefaultSkyTexture().Index;

        var key = new SkyKey(textureHandle.Value, options, offset);
        if (m_skyComponents.TryGetValue(key, out sky))
            return true;

        sky = new SkySphereComponent(m_archiveCollection, m_glTextureManager, textureHandle.Value, options, offset);
        m_skyComponents[key] = sky;
        m_skyComponentsList.Add(sky);
        return true;
    }

    public void Add(SkyGeometryVertex[] data, int length, int? textureHandle, SkyOptions options, Vec2F offset)
    {
        if (!GetOrCreateSky(textureHandle, options, offset, out var sky))
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
