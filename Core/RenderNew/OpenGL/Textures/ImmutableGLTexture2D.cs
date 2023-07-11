﻿using System;
using System.Diagnostics;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.RenderNew.OpenGL.Util;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Textures;

public class ImmutableGLTexture2D : GLTexture
{
    public readonly Dimension Dimension;
    public readonly Vec2F UVInverse;
    
    public ImmutableGLTexture2D(string label, Image image, Bindless isBindless, TextureWrapMode wrapMode) :
        base($"[Texture2D] {label}", TextureTarget.Texture2D)
    {
        Debug.Assert(image.Dimension.HasPositiveArea, $"Cannot have a texture with a zero or negative image area: {image.Dimension}");
        
        Dimension = image.Dimension;
        (int w, int h) = Dimension;
        UVInverse = (1.0f / w, 1.0f / h);
        
        Bind();
        GL.TextureStorage2D(Name, GLHelper.CalculateMipmapLevels(Dimension), SizedInternalFormat.Rgba8, w, h);
        unsafe
        {
            fixed (uint* pixelPtr = image.Pixels)
            {
                IntPtr ptr = new(pixelPtr);
                // Because the C# image format is 'ARGB', we can get it into the
                // RGBA format by doing a BGRA format and then reversing it.
                GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, ptr);
            }  
        }
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        SetParameters(wrapMode);
        if (isBindless == Bindless.Yes)
            MakeBindless();
        Unbind();
    }

    public ImmutableGLTexture2D(string label, Dimension dimension, Bindless isBindless, TextureWrapMode wrapMode) :
        base($"[Texture2D] {label}", TextureTarget.Texture2D)
    {
        Debug.Assert(dimension.HasPositiveArea, $"Cannot have a texture with a zero or negative area: {dimension}");
        
        Dimension = dimension;
        (int w, int h) = Dimension;
        UVInverse = (1.0f / w, 1.0f / h);
        
        Bind();
        GL.TextureStorage2D(Name, GLHelper.CalculateMipmapLevels(Dimension), SizedInternalFormat.Rgba8, w, h);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, w, h, PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, IntPtr.Zero);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        SetParameters(wrapMode);
        if (isBindless == Bindless.Yes)
            MakeBindless();
        Unbind();
    }

    private void MakeBindless()
    {
        BindlessHandle = GL.Arb.GetTextureHandle(Name);
        MakeResident();
    }

    // Assumes the user binds first.
    public void SetParameters(TextureWrapMode wrapMode)
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapMode);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapMode);
    }

    // Assumes the user binds first.
    public void SetAnisotropy(float? anisotropy)
    {
        if (!GLInfo.Extensions.TextureFilterAnisotropic || anisotropy == null)
            return;

        float value = anisotropy.Value.Clamp(1, (int)GLInfo.Limits.MaxAnisotropy);
        GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, value);
    }
}