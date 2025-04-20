
using OpenTK.Graphics.OpenGL;

namespace Helion.Render;

public static class BindTextures
{
    public const TextureUnit BoundTexture = TextureUnit.Texture0;
    public const TextureUnit SectorLight = TextureUnit.Texture1;
    public const TextureUnit Colormap = TextureUnit.Texture2;
    public const TextureUnit SectorColormap = TextureUnit.Texture3;
    public const TextureUnit AccumTexture = TextureUnit.Texture4;
    public const TextureUnit AccumCountTexture = TextureUnit.Texture5;
    public const TextureUnit FuzzTexture = TextureUnit.Texture6;
    public const TextureUnit OpaqueTexture = TextureUnit.Texture7;
    public const TextureUnit WallClipTexture = TextureUnit.Texture8;
    public const TextureUnit MapLineData = TextureUnit.Texture9;
    public const TextureUnit PlaneClipTexture = TextureUnit.Texture10;
}
