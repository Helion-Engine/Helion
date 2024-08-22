using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere;

public class SkySphereTexture : IDisposable
{
    record struct SkyTexture(GLLegacyTexture GlTexture, int AnimatedTextureIndex);

    private const int PixelRowsToEvaluate = 24;

    public float ScaleU = 1.0f;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly int m_textureHandleIndex;
    private readonly List<SkyTexture> m_skyTextures = [];
    private readonly bool m_fade;
    private bool m_loadedTextures;

    public SkySphereTexture(ArchiveCollection archiveCollection, LegacyGLTextureManager textureManager, int textureHandle, bool fade)
    {
        m_archiveCollection = archiveCollection;
        m_textureManager = textureManager;
        m_textureHandleIndex = textureHandle;
        m_fade = fade;
    }

    ~SkySphereTexture()
    {
        ReleaseUnmanagedResources();
    }

    public void LoadTextures()
    {
        m_loadedTextures = true;
        InitializeAnimatedTextures();
    }

    public GLLegacyTexture GetTexture()
    {
        if (!m_loadedTextures)
        {
            m_loadedTextures = true;
            InitializeAnimatedTextures();
        }

        // Check if we have generated this sky texture yet. The translation can change if skies are animated.
        int textureIndex = m_archiveCollection.TextureManager.GetTranslationIndex(m_textureHandleIndex);
        for (int i = 0; i < m_skyTextures.Count; i++)
        {
            if (m_skyTextures[i].AnimatedTextureIndex == textureIndex)
                return m_skyTextures[i].GlTexture;
        }

        if (GenerateSkyTextures(textureIndex, out var skyTexture))
        {
            m_skyTextures.Add(new(skyTexture, textureIndex));
            return skyTexture;
        }

        return m_textureManager.NullTexture;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private static float CalculateScale(int imageWidth)
    {
        // If the texture is huge, we'll just assume the user wants a one-
        // to-one scaling. See the bottom return comment on why this is
        // negative.
        if (imageWidth >= 1024)
            return -1.0f;

        // We want to fit either 4 '256 width textures' onto the sphere
        // or 1 '1024 width texture' onto the same area. While we're at
        // it, we can just make it so that the texture scales around to
        // it's nearest power of two.
        //
        // To do so, first find out X when we have width = 2^X. We need
        // to force this to be a whole number so we round. This is likely
        // not correct due to how a value at 0.5 won't do what we think,
        // but we'll deal with this later if the need ever arises.
        double roundedExponent = Math.Round(Math.Log(imageWidth, 2));

        // We want to fit it onto a sky that is 1024 in width. We can now
        // do `1024 / width` where width is a power of two. We can find out
        // the scaling factor with the following rearrangement:
        //      f = 1024 / width
        //        = 2^10 / 2^x       [because x is a whole number now]
        //        = 2^(10 - x)
        float scalingFactor = (float)Math.Pow(2, 10 - roundedExponent);

        // We make the scale negative so that the U coordinate is reversed.
        // The sphere is made in a counter-clockwise direction but drawing
        // the texture in other ports appears visually to be clockwise. By
        // setting the U scaling to be negative, the shader will reverse
        // the direction of the texturing (which is what we want).
        return -scalingFactor;
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
    private Image CreateFadedSky(int rowsToEvaluate, Color bottomFadeColor, Color topFadeColor, Image skyImage)
    {
        float scale = 128 / (float)skyImage.Height * 2.3f;
        int padding = (int)(skyImage.Height * scale);
        Image fadedSky = new(skyImage.Width, skyImage.Height * 2 + padding, ImageType.PaletteWithArgb);
        int middleY = fadedSky.Height / 2;

        // Fill the top and bottom halves with the fade colors, so we can draw
        // everything else on top of it later on.
        fadedSky.FillRows(topFadeColor, 0, middleY);
        fadedSky.FillRows(bottomFadeColor, middleY, fadedSky.Height);

        if (ShaderVars.PaletteColorMode)
        {
            fadedSky.FillRows(m_archiveCollection.Colormap.GetNearestColorIndex(topFadeColor), 0, middleY);
            fadedSky.FillRows(m_archiveCollection.Colormap.GetNearestColorIndex(bottomFadeColor), middleY, fadedSky.Height);
        }

        // Now draw the images on top of them.
        skyImage.DrawOnTopOf(fadedSky, (0, middleY));
        skyImage.DrawOnTopOf(fadedSky, (0, middleY - skyImage.Height));
        
        // Now blend the top of the image into the background.
        if (rowsToEvaluate > 0)
        {
            // Start from the top of the top piece and fade downwards, from the
            // background color into the image.
            Vec4F topColorVec = topFadeColor.Normalized;
            int startY = fadedSky.Height - (skyImage.Height * 2);
            for (int y = 0; y < rowsToEvaluate; y++)
            {
                int targetY = padding / 2 + y;
                if (targetY < 0 || targetY >= fadedSky.Height)
                    break;
                float t = (float)y / rowsToEvaluate;
                FillRow(fadedSky, topColorVec, targetY, t);
            }
            
            // Do the same but start at the top of the bottom transition zone and
            // walk downwards to blend.
            Vec4F bottomColorVec = bottomFadeColor.Normalized;
            startY = (middleY + skyImage.Height - 1);
            for (int y = 0; y < rowsToEvaluate; y++)
            {
                int targetY = startY - y;
                if (targetY < 0 || targetY >= fadedSky.Height)
                    break;
                float t = (float)y / rowsToEvaluate;
                FillRow(fadedSky, bottomColorVec, targetY, t);
            }
        }

        return fadedSky;
    }

    private void FillRow(Image fadedSky, Vec4F normalized, int targetY, float t)
    {
        for (int x = 0; x < fadedSky.Width; x++)
        {
            Color originalColor = fadedSky.GetPixel(x, targetY);
            Color newArgb = Color.Lerp(normalized, originalColor, t);
            if (ShaderVars.PaletteColorMode)
                fadedSky.SetPixel(x, targetY, newArgb, m_archiveCollection.Colormap);
            else
                fadedSky.SetPixel(x, targetY, newArgb);
        }
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
 
                if (GenerateSkyTextures(animatedTextureIndex, out var skyTexture))
                    m_skyTextures.Add(new(skyTexture, animatedTextureIndex));
            }
        }
    }

    private bool GenerateSkyTextures(int textureIndex, [NotNullWhen(true)] out GLLegacyTexture? texture)
    {
        Image? skyImage = m_archiveCollection.TextureManager.GetNonAnimatedTexture(textureIndex).Image;
        if (skyImage == null)
        {
            texture = null;
            return false;
        }

        ScaleU = CalculateScale(skyImage.Width);
        texture = CreateSkyTexture(textureIndex, skyImage);
        return true;
    }

    private GLLegacyTexture CreateSkyTexture(int textureIndex, Image skyImage)
    {
        return CreateTexture(GetFadedSkyImage(textureIndex, skyImage), $"[SKY][{textureIndex}] {m_archiveCollection.TextureManager.SkyTextureName}");
    }

    private Image GetFadedSkyImage(int textureIndex, Image skyImage)
    {
        if (LegacySkyRenderer.GeneratedImages.TryGetValue(textureIndex, out var existingImage))
            return existingImage;

        // Most (all?) skies are tall enough that we don't have to worry
        // about this, but if we run into a sky that is small then we
        // don't want to consume more than half of it. We also need to
        // make sure that we don't get a zero value if someone tries to
        // provide a single pixel sky (since Height(1) / 2 would be 0, so
        // we clamp it to be at least 1).
        int rowsToEvaluate = Math.Min(Math.Max(skyImage.Height / 2, 1), PixelRowsToEvaluate);

        int bottomStartY = skyImage.Height - rowsToEvaluate;
        int bottomExclusiveEndY = skyImage.Height;
        Color topFadeColor = CalculateAverageRowColor(0, rowsToEvaluate, skyImage);
        Color bottomFadeColor = CalculateAverageRowColor(bottomStartY, bottomExclusiveEndY, skyImage);

        Image fadedSkyImage = CreateFadedSky(m_fade ? rowsToEvaluate : 0, bottomFadeColor, topFadeColor, skyImage);
        LegacySkyRenderer.GeneratedImages[textureIndex] = fadedSkyImage;
        return fadedSkyImage;
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
        for (int i = 0; i < m_skyTextures.Count; i++)
        {
            m_textureManager.UnRegisterTexture(m_skyTextures[i].GlTexture);
            m_skyTextures[i].GlTexture.Dispose();
        }
    }
}
