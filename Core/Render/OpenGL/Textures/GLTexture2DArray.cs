using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Helion.Render.OpenGL.Textures;

public class GLTexture2DArray : GLTexture
{
    public readonly string Label;
    public Dimension Dimension { get; protected set; } = (1, 1);
    public int Depth { get; protected set; } = 1;
    public Vec2F UVInverse { get; protected set; } = Vec2F.One;

    public int Name => base.Name;

    public GLTexture2DArray(string label)
    {
        Label = label;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, Name, $"Texture [2D array]: {Label}");
        Unbind();
    }

    public GLTexture2DArray(string label, Dimension dimension, int depth, TextureWrapMode wrapMode, float? anisotropy = 1.0f) : 
        this(label)
    {
        Debug.Assert(dimension.Area > 0, "Should not have a zero or negative 2D texture array area");
        Debug.Assert(depth > 0, "Should not have a zero or negative 2D texture array depth");
        
        Dimension = dimension;
        Depth = depth;
        UVInverse = (1.0f / dimension.Width, 1.0f / dimension.Height);
        
        Bind();
        
        GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.Rgba8, dimension.Width, dimension.Height, depth, 0,
            PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, IntPtr.Zero);
        
        SetParameters(wrapMode);
        SetAnisotropy(anisotropy);
        GenerateMipmaps();

        Unbind();
    }

    public GLTexture2DArray(string label, IEnumerable<Image> images, TextureWrapMode wrapMode, float? anisotropy = 1.0f) : 
        this(label)
    {
        Debug.Assert(images.Any(), "Require at least one image for a texture 2D array");
        Debug.Assert(images.FirstOrDefault().Dimension.Area > 0, "Image must have a non-zero area");
        Debug.Assert(images.Select(i => i.Dimension.Area).Distinct().Count() == 1, "All 2D array textures must be the same dimension");

        Bind();

        UploadImages(images);
        SetParameters(wrapMode);
        SetAnisotropy(anisotropy);
        GenerateMipmaps();

        Unbind();
    }

    // Assumes the user binds first.
    public void UploadImages(IEnumerable<Image> images)
    {
        int numImages = images.Count();
        Image firstImage = images.FirstOrDefault();
        
        Depth = numImages;
        Dimension = firstImage.Dimension;
        UVInverse = (1.0f / firstImage.Width, 1.0f / firstImage.Height);

        foreach ((int i, Image image) in images.Enumerate())
            UploadLayer(image, i);
    }

    // Assumes the user binds first.
    public void UploadLayer(Image image, int layer)
    {
        image.Bitmap.WithLockedBits(ptr =>
        {
            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, layer, image.Width, image.Height, 1, 
                PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, ptr);
        });
    }

    // Assumes the user binds first.
    public void SetParameters(TextureWrapMode wrapMode)
    {
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapR, (int)wrapMode);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)wrapMode);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)wrapMode);
    }

    // Assumes the user binds first.
    public void SetAnisotropy(float? anisotropy)
    {
        if (!GLExtensions.TextureFilterAnisotropic || anisotropy == null)
            return;

        float value = anisotropy.Value.Clamp(1, (int)GLLimits.MaxAnisotropy);
        GL.TexParameter(TextureTarget.Texture2DArray, (TextureParameterName)All.TextureMaxAnisotropy, value);
    }

    // Assumes the user binds first.
    public void GenerateMipmaps()
    {
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
    }

    public override void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2DArray, Name);
    }

    public override void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2DArray, 0);
    }
}
