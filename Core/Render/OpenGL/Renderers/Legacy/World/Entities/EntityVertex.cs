using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EntityVertex
{
    public const uint FuzzBit = 0x1; // Should draw with the fuzz effect.
    public const uint FlipUBit = 0x2; // Flip the U coordinate of the texture.
    
    [VertexAttribute]
    public Vec3F Pos;

    [VertexAttribute(isIntegral: true)]
    public short LightLevel;

    [VertexAttribute]
    public byte Alpha;
    
    [VertexAttribute(isIntegral: true)]
    public byte Flags;

    [VertexAttribute]
    public Vec3F PrevPos;

    [VertexAttribute]
    public float OffsetZ;

    public EntityVertex(Vec3F pos, Vec3F prevPos, float offsetZ, short lightLevel, byte alpha, bool isFuzz, bool flipU)
    {
        Pos = pos;
        LightLevel = lightLevel;
        Alpha = alpha;
        Flags = (byte)((flipU ? FlipUBit : 0) | (isFuzz ? FuzzBit : 0));
        PrevPos = prevPos;
        OffsetZ = offsetZ;
    }
}
