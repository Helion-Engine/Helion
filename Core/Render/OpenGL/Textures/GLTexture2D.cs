using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Textures;

public class GLTexture2D : GLTexture
{
    public readonly string Label;
    public Dimension Dimension = (1, 1);
    public Vec2I Offset { get; protected set; }
    public Vec2F UVInverse { get; protected set; } = Vec2F.One;

    public int Name => base.Name;

    public GLTexture2D(string label)
    {
        Label = label;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Texture, Name, $"Texture: {Label}");
        Unbind();
    }

    public GLTexture2D(string label, Dimension dimension) : this(label)
    {
        Dimension = dimension;
    }

    public GLTexture2D(string label, Image image, TextureWrapMode wrapMode, float? anisotropy = 1.0f) : this(label)
    {
        Bind();

        UploadImage(image);
        SetParameters(wrapMode);
        SetAnisotropy(anisotropy);
        GenerateMipmaps();

        Unbind();
    }

    // Assumes the user binds first.
    public void UploadImage(Image image)
    {
        image.Bitmap.WithLockedBits(ptr =>
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, image.Width, image.Height, 0,
                PixelFormat.Bgra, PixelType.UnsignedInt8888Reversed, ptr);
        });

        Dimension = image.Dimension;
        Offset = image.Offset;
        UVInverse = (1.0f / image.Width, 1.0f / image.Height);
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
        if (!GLExtensions.TextureFilterAnisotropic || anisotropy == null)
            return;

        float value = anisotropy.Value.Clamp(1, (int)GLLimits.MaxAnisotropy);
        GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, value);
    }

    // Assumes the user binds first.
    public void GenerateMipmaps()
    {
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public override void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, Name);
    }

    public override void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}
