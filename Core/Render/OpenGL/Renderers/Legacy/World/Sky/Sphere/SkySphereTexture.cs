using System;
using System.Diagnostics.CodeAnalysis;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions;
using Helion.Util.Configs.Components;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public record struct SkyTexture(GLLegacyTexture GlTexture, int AnimatedTextureIndex, Vec4F TopColor, Vec4F BottomColor, int TopColorIndex, int BottomColorIndex);

// The sky texture looks like this (p = padding):
//
//      0  o----------o
//         |Fade color|
//     1/p o..........o  <- Blending
//         |          |
//         | Texture  |
//     1/2 o----------o
//         |          |
//         | Texture  |
// 1 - 1/p o..........o  <- Blending
//         |Fade color|
//      1  o----------o
//
// This is why we multiply by four. Note that there is no blending
// at the horizon (middle line).
//
public class SkySphereTexture(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, int textureHandle) : IDisposable
{
    private const float StaticSkySize = 0.25f;
    private const float StandardWidth = 256;
    private const float StandardHeight = 128;
    private const float StandardAspectRatio = StandardWidth / StandardHeight;
    private const int PixelRowsToEvaluate = 24;
    private readonly ArchiveCollection m_archiveCollection = archiveCollection;
    private readonly LegacyGLTextureManager m_textureManager = textureManager;
    private readonly int m_textureHandleIndex = textureHandle;
    private readonly DynamicArray<SkyTexture> m_skyTextures = new();
    private bool m_loadedTextures;

    ~SkySphereTexture()
    {
        ReleaseUnmanagedResources();
    }

    public void LoadTextures()
    {
        m_loadedTextures = true;
        InitializeAnimatedTextures();
    }

    public SkyTexture GetSkyTexture(out SkyTransform skyTransform)
    {
        if (!m_loadedTextures)
        {
            m_loadedTextures = true;
            InitializeAnimatedTextures();
        }

        skyTransform = SkyTransform.Default;
        skyTransform.Sky.Offset = default;
        // Check if we have generated this sky texture yet. The translation can change if skies are animated.
        int animationIndex = m_archiveCollection.TextureManager.GetTranslationIndex(m_textureHandleIndex);
        if (m_archiveCollection.TextureManager.TryGetSkyTransform(animationIndex, out var findTransform))
            skyTransform = findTransform;
        else if (m_archiveCollection.TextureManager.TryGetSkyTransform(m_textureHandleIndex, out findTransform))
            skyTransform = findTransform;
        return GetSkyTextureFromTextureIndex(animationIndex, m_textureHandleIndex);
    }

    private SkyTexture GetSkyTextureFromTextureIndex(int animationIndex, int textureIndex)
    {
        SkyTexture? findSkyTexture = null;
        var skyArray = m_skyTextures.Data;
        for (int i = 0; i < m_skyTextures.Length; i++)
        {
            ref var checkSkyTexture = ref skyArray[i];
            if (checkSkyTexture.AnimatedTextureIndex == animationIndex)
            {
                findSkyTexture = checkSkyTexture;
                break;
            }
        }

        if (findSkyTexture == null && GenerateSkyTexture(textureIndex, out var skyTexture))
        {
            m_skyTextures.Add(skyTexture.Value);
            findSkyTexture = skyTexture;
        }

        if (findSkyTexture != null)
            CheckSkyFireUpdate(findSkyTexture.Value.GlTexture, textureIndex);

        return findSkyTexture ?? new SkyTexture(m_textureManager.NullTexture, 0, Vec4F.Zero, Vec4F.Zero, 0, 0);
    }

    private void CheckSkyFireUpdate(GLLegacyTexture skyTexture, int textureIndex)
    {
        var skyFireTextures = m_archiveCollection.TextureManager.GetSkyFireTextures();
        for (int i = 0; i < skyFireTextures.Count; i++)
        {
            var skyFire = skyFireTextures[i];
            var texture = skyFire.Texture;
            if (!skyFire.RenderUpdate || texture.Image == null || texture.Index != textureIndex)
                continue;

            skyFire.RenderUpdate = false;

            m_textureManager.ReUpload(skyTexture, texture.Image, texture.Image.m_pixels);
        }
    }

    public SkyTexture GetForegroundTexture(SkyTransformTexture skyTexture)
    {
        int animationIndex = m_archiveCollection.TextureManager.GetTranslationIndex(skyTexture.TextureIndex);
        return GetSkyTextureFromTextureIndex(animationIndex, skyTexture.TextureIndex);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public static Vec2F CalcOffset(in SkyTexture skyTexture, SkyTransformTexture transform, SkyRenderMode mode, Vec2F scaleUV, SkyOptions options = SkyOptions.None)
    {
        var offset = transform.Offset + transform.CurrentScroll;

        if (mode == SkyRenderMode.Vanilla)
        {
            offset.X += StandardWidth;
            offset.Y += skyTexture.GlTexture.Height - StandardHeight;

            // Calculate the offset so that the midtexel is in the center of the sphere projection
            if (transform.MidTexel.HasValue)
                offset.Y += transform.MidTexel.Value + 28;

            // TODO something for options & SkyOptions.SkyTransfer ?
        }
        else
        {
            var adjustY = skyTexture.GlTexture.Height - StandardHeight;
            offset.Y += adjustY / 2;
            offset.X += GetTextureAdjustmentX(skyTexture.GlTexture.Width, skyTexture.GlTexture.Height);
        }

        if (transform.Scale.Y != 1)
            offset.Y += (StandardHeight * transform.Scale.Y - StandardHeight) * StandardAspectRatio;

        // Offset needs to be in texture coordinates
        offset.X /= skyTexture.GlTexture.Width;
        offset.Y /= skyTexture.GlTexture.Height;
        return offset;
    }

    private static float GetTextureAdjustmentX(int textureWidth, int textureHeight)
    {
        // Adjust texture position in the sphere with dynamic as it moves with the height because of the aspect ratio change.
        float aspectRatio = (float)textureWidth / textureHeight;
        float offsetX = StandardWidth - (StandardHeight * aspectRatio);
        if (aspectRatio >= 2)
            offsetX /= (aspectRatio / StandardAspectRatio);
        return -offsetX;
    }

    public static Vec2F CalcFireOffset(in SkyTexture skyTexture, SkyTransformTexture transform)
    {
        // This can't be right. The kex port has some weird offset stuff going on with fire textures specifically.
        var offset = transform.Offset + transform.CurrentScroll;

        const float fireTextureHeight = 200f;
        offset.Y += fireTextureHeight - StandardHeight;

        if (transform.MidTexel.HasValue)
        {
            var midOffset = skyTexture.GlTexture.Height * transform.MidTexel.Value / fireTextureHeight;
            offset.Y += 32 + midOffset;
        }

        // Offset needs to be in texture coordinates
        offset.X /= skyTexture.GlTexture.Width;
        offset.Y /= skyTexture.GlTexture.Height;
        return offset;
    }

    public static Vec2F CalcScale(in SkyTexture skyTexture, SkyTransformTexture skyTransform)
    {
        double roundedExponent = Math.Round(Math.Log(skyTexture.GlTexture.Dimension.Width, 2));
        float scalingFactor = (float)Math.Pow(2, 10 - roundedExponent);
        float u = 1 / scalingFactor;
        float v = (skyTexture.GlTexture.Dimension.Height / StandardHeight) * StaticSkySize;
        return (u, v) * skyTransform.Scale;
    }

    public static float CalcSkyHeight(float textureHeight, SkyRenderMode mode)
    {
        if (mode == SkyRenderMode.Vanilla)
            return StaticSkySize;

        float pad = StandardHeight / textureHeight * StaticSkySize;
        return (1 - (pad * 2)) / 2;
    }

    private static Color CalculateAverageRowColor(int startY, int exclusiveEndY, Image skyImage)
    {
        int r = 0;
        int g = 0;
        int b = 0;

        for (int y = startY; y < exclusiveEndY; y++)
        {
            for (int x = 0; x < skyImage.Width; x++)
            {
                Color color = skyImage.GetPixel(x, y);
                r += color.R;
                g += color.G;
                b += color.B;
            }
        }

        int totalPixels = (exclusiveEndY - startY) * skyImage.Width;
        r /= totalPixels;
        g /= totalPixels;
        b /= totalPixels;

        return Color.FromInts(255, r, g, b);
    }

    private void InitializeAnimatedTextures()
    {
        var animations = m_archiveCollection.TextureManager.GetAnimations();
        for (int i = 0; i < animations.Count; i++)
        {
            Animation anim = animations[i];
            if (anim.TranslationIndex != m_textureHandleIndex)
                continue;

            var components = anim.AnimatedTexture.Components;
            for (int j = 0; j < components.Count; j++)
            {
                int animatedTextureIndex = components[j].TextureIndex;
                if (GenerateSkyTexture(animatedTextureIndex, out var skyTexture))
                    m_skyTextures.Add(skyTexture.Value);
            }
        }
    }

    private bool GenerateSkyTexture(int textureIndex, [NotNullWhen(true)] out SkyTexture? texture)
    {
        Image? skyImage = m_archiveCollection.TextureManager.GetNonAnimatedTexture(textureIndex).Image;
        if (skyImage == null)
        {
            texture = null;
            return false;
        }

        GetAverageColors(skyImage, out var topColor, out var bottomColor);
        var palette = m_archiveCollection.Palette;
        var glTexture = CreateTexture(skyImage, $"[SKY][{textureIndex}] {m_archiveCollection.TextureManager.SkyTextureName}");
        texture = new(glTexture, textureIndex, topColor, bottomColor,
            palette.GetNearestColorIndex(FromRgba(topColor)), palette.GetNearestColorIndex(FromRgba(bottomColor)));
        return true;
    }

    private static Color FromRgba(Vec4F rgba)
    {
        return new Color((byte)(rgba.W * 255), (byte)(rgba.X * 255), (byte)(rgba.Y * 255), (byte)(rgba.Z * 255));
    }

    private void GetAverageColors(Image skyImage, out Vec4F topColor, out Vec4F bottomColor)
    {
        // Most (all?) skies are tall enough that we don't have to worry
        // about this, but if we run into a sky that is small then we
        // don't want to consume more than half of it. We also need to
        // make sure that we don't get a zero value if someone tries to
        // provide a single pixel sky (since Height(1) / 2 would be 0, so
        // we clamp it to be at least 1).
        int rowsToEvaluate = Math.Min(Math.Max(skyImage.Height / 2, 1), PixelRowsToEvaluate);

        int bottomStartY = skyImage.Height - rowsToEvaluate;
        int bottomExclusiveEndY = skyImage.Height;
        topColor = CalculateAverageRowColor(0, rowsToEvaluate, skyImage).Normalized3.To4D(1);
        bottomColor = CalculateAverageRowColor(bottomStartY, bottomExclusiveEndY, skyImage).Normalized3.To4D(1);
    }

    private GLLegacyTexture CreateTexture(Image fadedSkyImage, string debugName = "")
    {
        int textureId = GL.GenTexture();
        GLLegacyTexture texture = new(textureId, debugName, fadedSkyImage.Dimension, fadedSkyImage.Offset, fadedSkyImage.Namespace, TextureTarget.Texture2D, 0);

        m_textureManager.UploadAndSetParameters(texture, fadedSkyImage, debugName, ResourceNamespace.Global, TextureFlags.Default);
        m_textureManager.RegisterTexture(texture);

        return texture;
    }

    private void ReleaseUnmanagedResources()
    {
        for (int i = 0; i < m_skyTextures.Length; i++)
        {
            m_textureManager.UnRegisterTexture(m_skyTextures[i].GlTexture);
            m_skyTextures[i].GlTexture.Dispose();
        }
    }
}
