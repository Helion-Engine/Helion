using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct EntityVertex
{
    public const uint FuzzBit = 0x1; // Should draw with the fuzz effect.
    public const uint FlipUBit = 0x2; // Flip the U coordinate of the texture.
    
    [VertexAttribute]
    public readonly Vec3F Pos;

    [VertexAttribute(normalized: true)]
    public readonly byte LightLevel;

    [VertexAttribute]
    public readonly byte Alpha;
    
    [VertexAttribute(isIntegral: true)]
    public readonly byte Flags;

    public EntityVertex(Vec3F pos, byte lightLevel, byte alpha, bool isFuzz, bool flipU)
    {
        Pos = pos;
        LightLevel = lightLevel;
        Alpha = alpha;
        Flags = (byte)((flipU ? FlipUBit : 0) | (isFuzz ? FuzzBit : 0));
    }
}
