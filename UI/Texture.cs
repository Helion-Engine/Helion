using OpenTK.Graphics.OpenGL;
using StbImageSharp;

namespace Helion.UI;

public class Texture : IDisposable
{
    public readonly int Handle;
    private bool m_disposed;

    public Texture(string path)
    {
        Handle = GL.GenTexture();
        Bind();
        UploadTexture(path);
        SetParameters();
        GenerateMipmaps();
        Unbind();
    }

    ~Texture()
    {
        ReleaseUnmanagedResources();
    }

    private static void UploadTexture(string path)
    {
        StbImage.stbi_set_flip_vertically_on_load(1);

        using Stream stream = File.OpenRead(path);
        ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
    }

    private void SetParameters()
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)All.TextureMaxAnisotropy, GL.GetFloat((GetPName)All.MaxTextureMaxAnisotropy));
    }

    private void GenerateMipmaps()
    {
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }
    
    public void Unbind()
    {
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    private void ReleaseUnmanagedResources()
    {
        if (!m_disposed)
            return;
        
        GL.DeleteTexture(Handle);

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}