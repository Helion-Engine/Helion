using Helion.Geometry;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Container;
using Helion.Util.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Helion.Render.OpenGL.Texture.Legacy;

public record struct TextureBucket(Dimension Dimension, DynamicArray<Resources.Texture> Textures);

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
        arrayTextures += BuildTextureArrayFromTextures(textures, TextureContext.WorldArray, TextureFlags.Default);

        textures.Clear();
        foreach (var index in wallTexturesClamp.Where(x => !m_textureManager.IsTextureAnimated(x)))
            AddTexture(textures, index);

        buildTextures += textures.Count;
        arrayTextures += BuildTextureArrayFromTextures(textures, TextureContext.WorldArray, TextureFlags.ClampY);

        var totalTextures = flatTextures.Count() + wallTexturesRepeat.Count() + wallTexturesClamp.Count();
        var animated = totalTextures - buildTextures;
        DebugLog(totalTextures, arrayTextures + animated);
    }

    public TextureBucket[] BuildSprites(DynamicArray<SpriteDefinition> spriteDefinitions)
    {
        var spriteTextures = new DynamicArray<Resources.Texture>();
        var spriteTextureHandles = new HashSet<int>();
        foreach (var spriteDefinition in spriteDefinitions)
        {
            if (spriteDefinition == null)
                continue;

            for (int i = 0; i < SpriteDefinition.MaxFrames; i++)
            {
                for (int j = 0; j < SpriteDefinition.MaxRotations; j++)
                {
                    var rotation = spriteDefinition.Rotations[i, j];
                    if (rotation == null)
                        continue;

                    if (rotation.Texture.RenderStore != null)
                        continue;

                    if (spriteTextureHandles.Add(rotation.Texture.Index))
                        spriteTextures.Add(rotation.Texture);

                }
            }
        }

        const int BaseSize = 32;
        const int BucketCount = 5;
        var buckets = CreateTextureBuckets(spriteTextures, BaseSize, BucketCount);
        for (int i = 0; i < buckets.Length - 1; i++)
        {
            var bucket = buckets[i];
            BuildTextureArray(bucket.Textures.Data.AsSpan(0, bucket.Textures.Length), TextureContext.WorldSprites, TextureFlags.ClampX | TextureFlags.ClampY, bucket.Dimension);
        }

        // TODO overflow bucket ^1

        foreach (var spriteDefinition in spriteDefinitions)
        {
            if (spriteDefinition == null)
                continue;

            for (int i = 0; i < SpriteDefinition.MaxFrames; i++)
            {
                for (int j = 0; j < SpriteDefinition.MaxRotations; j++)
                {
                    var rotation = spriteDefinition.Rotations[i, j];
                    if (rotation == null)
                        continue;

                    if (rotation.RenderStore != null)
                        continue;

                    if (rotation.Texture.Image != null)
                        rotation.TextureBucket = GetBucketIndex(rotation.Texture.Image.Dimension, BaseSize, BucketCount);

                    rotation.BrightmapRenderStore = m_glTextureManager.CreateBrightMapTexture(rotation.Texture.BrightmapImage, rotation.Texture.Name, ResourceNamespace.Brightmaps);

                    if (m_glTextureManager.TryGetTexture(rotation.Texture.Index, TextureContext.WorldSprites, out var texture))
                        rotation.RenderStore = texture.RenderStoreClamp ?? texture.RenderStore;

                    Assert.Postcondition(rotation.RenderStore != null, $"Failed to find generated array image for sprite");
                }
            }
        }

        return buckets;
    }

    [Conditional("DEBUG")]
    private static void DebugLog(int totalTextures, int compressed)
    {
        HelionLog.Info($"Compressed textures {totalTextures} -> {compressed}");
    }

    private int BuildTextureArrayFromTextures(DynamicArray<Resources.Texture> textures, TextureContext textureContext, TextureFlags textureFlags)
    {
        textures.Sort(SortTexturesByDimensions);

        int textureCount = 0;
        var arrayTextures = new DynamicArray<Resources.Texture>();
        var dimension = new Dimension(0, 0);
        foreach (var texture in textures)
        {
            if (texture.Image == null)
                continue;

            if (dimension != texture.Image.Dimension)
            {
                if (arrayTextures.Count > 0)
                    textureCount += BuildTextureArray(arrayTextures.Data.AsSpan(0, arrayTextures.Length), textureContext, textureFlags, dimension);
                arrayTextures.Clear();
                dimension = texture.Image.Dimension;
            }

            arrayTextures.Add(texture);
        }

        if (arrayTextures.Count > 0)
            textureCount += BuildTextureArray(arrayTextures.Data.AsSpan(0, arrayTextures.Length), textureContext, textureFlags, dimension);

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

    private int BuildTextureArray(Span<Resources.Texture> textures, TextureContext textureContext, TextureFlags textureFlags, Dimension dimension)
    {
        var arrayTexture = m_glTextureManager.CreateTextureArray(textures, textureContext, textureFlags, dimension);
        return arrayTexture == null ? 0 : 1;
    }

    private static TextureBucket[] CreateTextureBuckets(DynamicArray<Resources.Texture> textures, int baseSize, int bucketCount)
    {
        if (baseSize <= 0)
            throw new ArgumentException($"Invalid baseSize {baseSize}");
        if (bucketCount <= 0)
            throw new ArgumentException($"Invalid bucketCount {bucketCount}");

        var buckets = new TextureBucket[bucketCount + 1];

        for (int i = 0; i < bucketCount; i++)
        {
            var size = baseSize * (int)Math.Pow(2, i);
            buckets[i] = new TextureBucket(new Dimension(size, size), new(128));
        }

        buckets[^1] = new TextureBucket(new Dimension(0, 0), []);

        foreach (var texture in textures)
        {
            // This shouldn't happen
            if (texture.Image == null)
                continue;

            var index = GetBucketIndex(texture.Image.Dimension, baseSize, bucketCount);
            buckets[index].Textures.Add(texture);
        }

        return buckets;
    }

    public static int GetBucketIndex(Dimension dimension, int baseSize, int bucketCount)
    {
        int size = MathHelper.Max(dimension.Width, dimension.Height);
        // Fits in the smallest bucket
        if (size <= baseSize)
            return 0;

        int index = 0;
        int current = baseSize;

        while (index < bucketCount - 1 && size > current)
        {
            current *= 2;
            index++;
        }

        return index;
    }
}
