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

    [VertexAttribute]
    public float LightLevel;

    [VertexAttribute]
    public float Alpha;

    [VertexAttribute]
    public float Fuzz;

    [VertexAttribute]
    public float FlipU;

    [VertexAttribute]
    public Vec3F PrevPos;

    public EntityVertex(Vec3F pos, Vec3F prevPos, short lightLevel, float alpha, float isFuzz, float flipU)
    {
        Pos = pos;
        LightLevel = lightLevel;
        Alpha = alpha;
        Fuzz = isFuzz;
        FlipU = flipU;
        PrevPos = prevPos;
    }
}
