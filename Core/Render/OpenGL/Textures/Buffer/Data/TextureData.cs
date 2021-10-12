using System.Runtime.InteropServices;
using Helion.Geometry.Boxes;

namespace Helion.Render.OpenGL.Textures.Buffer.Data;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct TextureData
{
    public static readonly int TexelSize = Marshal.SizeOf<TextureData>() / (sizeof(float) * GLTextureDataBuffer.FloatsPerTexel);

    public readonly Box2F Bounds;
    public readonly Box2F UV;

    public TextureData(Box2F bounds, Box2F uv)
    {
        Bounds = bounds;
        UV = uv;
    }
}

