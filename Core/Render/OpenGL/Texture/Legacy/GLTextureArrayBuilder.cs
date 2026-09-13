using Helion.Geometry;
using Helion.Resources;
using Helion.Util.Container;
using Helion.Util.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Helion.Render.OpenGL.Texture.Legacy;

public class GLTextureArrayBuilder(TextureManager textureManager, LegacyGLTextureManager glTextureManager)
{
    private readonly TextureManager m_textureManager = textureManager;
    private readonly LegacyGLTextureManager m_glTextureManager = glTextureManager;

    public void BuildLevel(IEnumerable<int> flatTextures, IEnumerable<int> wallTexturesRepeat, IEnumerable<int> wallTexturesClamp)
    {
        m_glTextureManager.DestroyTextureArrays(TextureContext.WorldArray);

        // Animations are excluded. This leaves them reliant on their previous behavior of swapping textures.
        int buildTextures = 0;
        int arrayTextures = 0;
        var textures = new DynamicArray<Resources.Texture>(1024);
        foreach (var index in flatTextures.Where(x => !m_textureManager.IsTextureAnimated(x)))
            AddTexture(textures, index);

        foreach (var index in wallTexturesRepeat.Where(x => !m_textureManager.IsTextureAnimated(x)))
            AddTexture(textures, index);

        buildTextures += textures.Count;
        textures.Sort(SortTexturesByDimensions);
        arrayTextures += BuildTextureArrayFromTextures(textures, TextureContext.WorldArray, true);

        textures.Clear();
        foreach (var index in wallTexturesClamp.Where(x => !m_textureManager.IsTextureAnimated(x)))
            AddTexture(textures, index);

        buildTextures += textures.Count;
        textures.Sort(SortTexturesByDimensions);
        arrayTextures += BuildTextureArrayFromTextures(textures, TextureContext.WorldArray, false);

        var totalTextures = flatTextures.Count() + wallTexturesRepeat.Count() + wallTexturesClamp.Count();
        var animated = totalTextures - buildTextures;
        DebugLog(totalTextures, arrayTextures + animated);
    }

    [Conditional("DEBUG")]
    private static void DebugLog(int totalTextures, int compressed)
    {
        HelionLog.Info($"Compressed textures {totalTextures} -> {compressed}");
    }

    private int BuildTextureArrayFromTextures(DynamicArray<Resources.Texture> textures, TextureContext textureContext, bool repeatY)
    {
        int textureCount = 0;
        var arrayTextures = new DynamicArray<int>();
        var dimension = new Dimension(0, 0);
        foreach (var texture in textures)
        {
            if (texture.Image == null)
                continue;

            if (dimension != texture.Image.Dimension)
            {
                if (arrayTextures.Count > 0)
                    textureCount += BuildTextureArray(arrayTextures.Data.AsSpan(0, arrayTextures.Length), textureContext, repeatY);
                arrayTextures.Clear();
                dimension = texture.Image.Dimension;
            }

            arrayTextures.Add(texture.Index);
        }

        if (arrayTextures.Count > 0)
            textureCount += BuildTextureArray(arrayTextures.Data.AsSpan(0, arrayTextures.Length), textureContext, repeatY);

        return textureCount;
    }

    private void AddTexture(DynamicArray<Resources.Texture> textures, int index)
    {
        // Exclude brightmaps for now. They probably could be made into arrays but like animations the complication may not be worth it.
        var texture = m_textureManager.GetTexture(index);
        if (texture.Image != null && texture.BrightmapImage == null)
            textures.Add(texture);
    }

    private static int SortTexturesByDimensions(Resources.Texture x, Resources.Texture y)
    {
        if (x.Image == null || y.Image == null)
            throw new NullReferenceException("Texture image must not be null");

        if (x.Image.Height == y.Image.Height)
            return x.Image.Width.CompareTo(y.Image.Width);

        return x.Image.Height.CompareTo(y.Image.Height);
    }

    private int BuildTextureArray(Span<int> textures, TextureContext textureContext, bool repeatY)
    {
        var arrayTexture = m_glTextureManager.CreateTextureArray(textures, textureContext, repeatY);
        return arrayTexture == null ? 0 : 1;
    }
}
