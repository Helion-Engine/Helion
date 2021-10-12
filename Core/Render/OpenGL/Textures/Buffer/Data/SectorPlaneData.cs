using System.Drawing;
using System.Runtime.InteropServices;
using Helion.Geometry.Planes;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Textures.Buffer.Data;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct SectorPlaneData
{
    public static readonly int TexelSize = Marshal.SizeOf<SectorPlaneData>() / (sizeof(float) * GLTextureDataBuffer.FloatsPerTexel);

    public readonly Vec4F Plane;
    public readonly Vec4F Color;
    public readonly float TextureIndex;
    public readonly float LightLevel;
    // Required to align to a power of two and for texels.
    public readonly float Unused2;
    public readonly float Unused3;
    public readonly float Unused4;
    public readonly float Unused5;
    public readonly float Unused6;
    public readonly float Unused7;

    public SectorPlaneData(Plane3D plane, Color color, int textureIndex, byte lightLevel)
    {
        Plane = plane.Vec;
        Color = (color.R, color.G, color.B, color.A);
        TextureIndex = textureIndex;
        LightLevel = lightLevel / 255.0f;
        Unused2 = 0;
        Unused3 = 0;
        Unused4 = 0;
        Unused5 = 0;
        Unused6 = 0;
        Unused7 = 0;
    }
}

