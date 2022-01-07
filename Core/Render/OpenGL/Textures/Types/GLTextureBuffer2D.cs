using System;
using System.Runtime.InteropServices;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Util;
using NLog;
using OpenTK.Graphics.OpenGL;
using static Helion.Render.OpenGL.Textures.Buffer.GLTextureDataBuffer;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Types;

public class GLTextureBuffer2D : GLTexture
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly Dimension Dimension;

    public int Width => Dimension.Width;
    public int Height => Dimension.Height;

    public GLTextureBuffer2D(string debugName, Dimension dimension) :
        base(debugName, TextureTarget.Texture2D)
    {
        Dimension = dimension;

        Bind();
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Clamp);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, dimension.Width,
            dimension.Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        Unbind();
    }

    public void Write<T>(Vec2I coordinate, T data, int texelWidth, Binding binding) where T : struct
    {
        Precondition(Marshal.SizeOf<T>() == texelWidth * BytesPerTexel, "Texel width of struct is incorrect");

        (int x, int y) = coordinate;
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            string errorMsg = $"Writing out of bounds for texture buffer 2D with {typeof(T).Name} at {coordinate} (dimension: {Dimension})";
            Log.Error(errorMsg);
            Fail(errorMsg);
            return;
        }

        if (binding == Binding.Bind)
            Bind();

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, texelWidth, 1, PixelFormat.Rgba,
            PixelType.Float, ref data);

        if (binding == Binding.Bind)
            Unbind();
    }
}
