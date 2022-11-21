using System;
using System.Drawing;
using Helion;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.Common.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;

namespace Helion.Render.OpenGL.Texture.Legacy;

public class GLLegacyImageDrawInfoProvider : IImageDrawInfoProvider
{
    private readonly LegacyGLTextureManager m_textureManager;

    public GLLegacyImageDrawInfoProvider(LegacyGLTextureManager textureManager)
    {
        m_textureManager = textureManager;
    }

    public bool ImageExists(string image)
    {
        return m_textureManager.Contains(image);
    }

    public Dimension GetImageDimension(string image, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
    {
        m_textureManager.TryGet(image, resourceNamespace, out GLLegacyTexture texture);
        return texture.Dimension;
    }

    public Vec2I GetImageOffset(string image, ResourceNamespace resourceNamespace = ResourceNamespace.Global)
    {
        m_textureManager.TryGet(image, resourceNamespace, out GLLegacyTexture texture);
        return texture.Offset;
    }

    public int GetFontHeight(string font) => m_textureManager.GetFont(font).Height;
}
