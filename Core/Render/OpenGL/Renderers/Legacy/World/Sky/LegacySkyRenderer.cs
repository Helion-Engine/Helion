using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Graphics;
using Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky;

public class LegacySkyRenderer : IDisposable
{
    private const int MaxSkyTextures = 255;

    public static readonly Dictionary<int, Image> GeneratedImages = [];

    private readonly ArchiveCollection m_archiveCollection;
    private readonly TextureManager m_textureManager;
    private readonly LegacyGLTextureManager m_glTextureManager;
    private readonly Dictionary<int, ISkyComponent> m_skyComponents = [];
    private readonly List<ISkyComponent> m_skyComponentsList = [];

    public LegacySkyRenderer(ArchiveCollection archiveCollection, LegacyGLTextureManager glTextureManager)
    {
        m_archiveCollection = archiveCollection;
        m_glTextureManager = glTextureManager;
        m_textureManager = archiveCollection.TextureManager;
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
        GeneratedImages.Clear();
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

        sky = new SkySphereComponent(m_archiveCollection, m_glTextureManager, textureHandle.Value, flipSkyTexture);
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

    private SkyTransform GetSkyTransFormOrDefault(int textureHandle)
    {
        if (m_textureManager.TryGetSkyTransform(textureHandle, out var skyTransform))
            return skyTransform;
        return SkyTransform.Default;
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
