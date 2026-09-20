using Helion.Geometry;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Textures;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Assertion;
using Helion.Util.Container;
using Helion.Util.Loggers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;

namespace Helion.Render.OpenGL.Texture.Legacy;

public interface IArrayTexture
{
    string FetchTextureName { get; }
    IRenderableTextureHandle? Handle { get; set; }
    int ResolvedHeight { get; set; }
}

public record struct TextureBuckets(int MaxTextureIndex, TextureBucket[] Buckets);
public record struct TextureBucket(Dimension Dimension, DynamicArray<Resources.Texture> Textures);

public static class GLTextureArrayBuilder
{
    public static void BuildLevel(TextureManager textureManager, LegacyGLTextureManager glTextureManager, IEnumerable<int> flatTextures, IEnumerable<int> wallTexturesRepeat, IEnumerable<int> wallTexturesClamp)
    {
        glTextureManager.DestroyTextureArrays(TextureContext.WorldArray);

        // Animations are excluded. This leaves them reliant on their previous behavior of swapping textures.
        int buildTextures = 0;
        int arrayTextures = 0;
        var textures = new DynamicArray<Resources.Texture>(1024);
        foreach (var index in flatTextures.Where(x => !textureManager.IsTextureAnimated(x)))
            AddTexture(textureManager, textures, index);

        foreach (var index in wallTexturesRepeat.Where(x => !textureManager.IsTextureAnimated(x)))
            AddTexture(textureManager, textures, index);

        buildTextures += textures.Count;
        arrayTextures += BuildTextureArrayFromTextures(glTextureManager, textures, TextureContext.WorldArray, TextureFlags.Default);

        textures.Clear();
        foreach (var index in wallTexturesClamp.Where(x => !textureManager.IsTextureAnimated(x)))
            AddTexture(textureManager, textures, index);

        buildTextures += textures.Count;
        arrayTextures += BuildTextureArrayFromTextures(glTextureManager, textures, TextureContext.WorldArray, TextureFlags.ClampY);

        var totalTextures = flatTextures.Count() + wallTexturesRepeat.Count() + wallTexturesClamp.Count();
        var animated = totalTextures - buildTextures;
        DebugLog(totalTextures, arrayTextures + animated);
    }

    public static TextureBuckets BuildSprites(LegacyGLTextureManager glTextureManager, DynamicArray<SpriteDefinition> spriteDefinitions)
    {
        var maxIndex = 0;
        var spriteTextures = new DynamicArray<Resources.Texture>();
        var spriteTextureHandles = new HashSet<int>(spriteDefinitions.Length);
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

                    if (spriteTextureHandles.Add(rotation.Texture.Index))
                    {
                        spriteTextures.Add(rotation.Texture);
                        maxIndex = MathHelper.Max(rotation.Texture.Index, maxIndex);
                    }
                }
            }
        }

        const int BaseSize = 32;
        const int BucketCount = 5;
        var buckets = CreateTextureBuckets(spriteTextures, BaseSize, BucketCount);
        for (int i = 0; i < buckets.Length - 1; i++)
        {
            var bucket = buckets[i];
            BuildTextureArray(glTextureManager, bucket.Textures.Data.AsSpan(0, bucket.Textures.Length), TextureContext.WorldSprites, TextureFlags.ClampX | TextureFlags.ClampY, bucket.Dimension, false);
        }

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

                    rotation.BrightmapRenderStore = glTextureManager.CreateBrightMapTexture(rotation.Texture.BrightmapImage, rotation.Texture.Name, ResourceNamespace.Brightmaps);

                    if (!CanTextureArray(rotation.Texture))
                    {
                        rotation.RenderStore = glTextureManager.CreateTexture(rotation.Texture.Image, rotation.Texture.Name, ResourceNamespace.Sprites);
                        continue;
                    }

                    if (glTextureManager.TryGetTexture(rotation.Texture.Index, TextureContext.WorldSprites, out var texture))
                        rotation.RenderStore = texture.RenderStoreClamp ?? texture.RenderStore;

                    Assert.Postcondition(rotation.RenderStore != null, $"Failed to find generated array image for sprite");
                }
            }
        }

        return new(maxIndex, buckets);
    }

    public static void BuildHud(IRendererTextureManager renderTextureManager, DynamicArray<IArrayTexture> arrayTextures)
    {
        var textures = new DynamicArray<Resources.Texture>(arrayTextures.Length);

        var textureLookup = new Dictionary<int, IArrayTexture>();
        int index = 0;
        foreach (var arrayTexture in arrayTextures)
        {
            var texture = new Resources.Texture(arrayTexture.FetchTextureName, ResourceNamespace.Sprites, index++);
            if (renderTextureManager.TryGetImage(arrayTexture.FetchTextureName, out var image) ||
                renderTextureManager.TryGetImage(arrayTexture.FetchTextureName, out image, ResourceNamespace.Sprites))
            {
                arrayTexture.ResolvedHeight = image.Dimension.Height;
                texture.Image = image;
                textures.Add(texture);
                textureLookup[texture.Index] = arrayTexture;
            }
        }

        BuildTextureArrayFromTextures(renderTextureManager, textures, TextureContext.Hud, TextureFlags.ClampX | TextureFlags.ClampY, true);

        foreach (var texture in textures)
        {
            if (texture.RenderStoreClamp is not IRenderableTextureHandle handle)
                continue;

            if (textureLookup.TryGetValue(texture.Index, out var findArrayTexture))
            {
                findArrayTexture.Handle = handle;
                renderTextureManager.RegisterTexture(findArrayTexture.FetchTextureName, handle, ResourceNamespace.Undefined);
            }
        }
    }

    [Conditional("DEBUG")]
    private static void DebugLog(int totalTextures, int compressed)
    {
        HelionLog.Info($"Compressed textures {totalTextures} -> {compressed}");
    }

    private static int BuildTextureArrayFromTextures(IRendererTextureManager renderTextureManager, DynamicArray<Resources.Texture> textures, TextureContext textureContext, TextureFlags textureFlags, bool addToTextureTracker = false)
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
                    textureCount += BuildTextureArray(renderTextureManager, arrayTextures.Data.AsSpan(0, arrayTextures.Length), textureContext, textureFlags, dimension, addToTextureTracker);
                arrayTextures.Clear();
                dimension = texture.Image.Dimension;
            }

            arrayTextures.Add(texture);
        }

        if (arrayTextures.Count > 0)
            textureCount += BuildTextureArray(renderTextureManager, arrayTextures.Data.AsSpan(0, arrayTextures.Length), textureContext, textureFlags, dimension, addToTextureTracker);

        return textureCount;
    }

    private static void AddTexture(TextureManager textureManager, DynamicArray<Resources.Texture> textures, int index)
    {
        var texture = textureManager.GetTexture(index);
        if (CanTextureArray(texture))
            textures.Add(texture);
    }

    private static bool CanTextureArray(Resources.Texture texture)
    {
        // Exclude brightmaps for now. They probably could be made into arrays but like animations the complication may not be worth it.
        return texture.Image != null && texture.BrightmapImage == null;
    }

    private static int SortTexturesByDimensions(Resources.Texture x, Resources.Texture y)
    {
        if (x.Image == null || y.Image == null)
            throw new NullReferenceException("Texture image must not be null");

        if (x.Image.Height == y.Image.Height)
            return x.Image.Width.CompareTo(y.Image.Width);

        return x.Image.Height.CompareTo(y.Image.Height);
    }

    private static int BuildTextureArray(IRendererTextureManager textureManager, Span<Resources.Texture> textures, TextureContext textureContext, TextureFlags textureFlags, Dimension dimension, bool addToTextureTracker)
    {
        if (textures.Length > GLInfo.MaxArrayLayers)
        {
            var finalTextures = new DynamicArray<Resources.Texture>(GLInfo.MaxArrayLayers);
            var remaining = textures.Length;
            var offset = 0;
            var total = 0;

            while (remaining > 0)
            {
                var count = MathHelper.Min(remaining, GLInfo.MaxArrayLayers);
                for (int i = 0; i < count; i++)
                    finalTextures.Add(textures[offset + i]);

                var createSubArray = textureManager.CreateTextureArray(finalTextures.Data.AsSpan(0, finalTextures.Length), textureContext, textureFlags, dimension, addToTextureTracker);
                total += createSubArray ? 0 : 1;
                finalTextures.Clear();

                offset += count;
                remaining -= count;
            }

            return total;
        }

        var arrayTexture = textureManager.CreateTextureArray(textures, textureContext, textureFlags, dimension, addToTextureTracker);
        return arrayTexture ? 0 : 1;
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

        var overflowBucketIndex = buckets.Length - 1;
        buckets[overflowBucketIndex] = new TextureBucket(new Dimension(0, 0), []);

        foreach (var texture in textures)
        {
            var index = CanTextureArray(texture) ? GetBucketIndex(texture.Image!.Dimension, baseSize, bucketCount) : overflowBucketIndex;
            buckets[index].Textures.Add(texture);
        }

        // Split buckets that overflow GLInfo.MaxArrayLayers
        return FinalizeTextureBuckets(buckets);
    }

    private static TextureBucket[] FinalizeTextureBuckets(TextureBucket[] buckets)
    {
        var finalizedBuckets = new List<TextureBucket>();
        foreach (var bucket in buckets)
        {
            if (bucket.Textures.Length <= GLInfo.MaxArrayLayers)
            {
                finalizedBuckets.Add(bucket);
                continue;
            }

            var remaining = bucket.Textures.Length;
            var offset = 0;

            while (remaining > 0)
            {
                var count = MathHelper.Min(remaining, GLInfo.MaxArrayLayers);
                var newBucket = new TextureBucket(bucket.Dimension, new(count));
                for (int i = 0; i < count; i++)
                    newBucket.Textures.Add(bucket.Textures[offset + i]);

                finalizedBuckets.Add(newBucket);

                offset += count;
                remaining -= count;
            }
        }

        return [.. finalizedBuckets];
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
