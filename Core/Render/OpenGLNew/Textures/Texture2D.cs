using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Textures;

public class Texture2D : Texture
{
    public Dimension Dimension { get; private set; }
    public Vec2F InverseUV { get; private set; } = Vec2F.One;
    public Vec2I Offset { get; private set; }

    protected override TextureTarget Target => TextureTarget.Texture2D;

    public Texture2D(string label) : base(label)
    {
    }
    
    public Texture2D(string label, Dimension dimension, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8, 
        PixelFormat format = PixelFormat.Bgra, PixelType type = PixelType.UnsignedInt8888Reversed)
        : this(label)
    {
        Upload(IntPtr.Zero, dimension, internalFormat, format, type);
    }

    private void Upload(IntPtr data, Dimension dimension, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8, 
        PixelFormat format = PixelFormat.Bgra, PixelType type = PixelType.UnsignedInt8888Reversed, int level = 0)
    {
        GL.TexImage2D(TextureTarget.Texture2D, level, internalFormat, dimension.Width, dimension.Height, 0, format, type, data);
        
        Dimension = dimension;
        InverseUV = (1.0f / dimension.Width, 1.0f / dimension.Height);
    }
    
    public unsafe void Upload(Image image, PixelInternalFormat internalFormat = PixelInternalFormat.Rgba8, 
        PixelFormat format = PixelFormat.Bgra, PixelType type = PixelType.UnsignedInt8888Reversed, 
        int level = 0)
    {
        fixed (uint* pixelPtr = image.Pixels)
        {
            Upload(new(pixelPtr), image.Dimension);
        }

        Offset = image.Offset;
    }

    public override void GenerateMipmaps()
    {
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }
}