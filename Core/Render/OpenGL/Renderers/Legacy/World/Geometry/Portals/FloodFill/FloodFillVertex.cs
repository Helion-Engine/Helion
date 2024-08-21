using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FloodFillVertex
{
    [VertexAttribute("pos", size: 3)]
    public Vec3F Pos;
    
    [VertexAttribute("planeZ", size: 1)]
    public float PlaneZ;
    
    [VertexAttribute("minViewZ", size: 1)]
    public float MinViewZ;
    
    [VertexAttribute("maxViewZ", size: 1)]
    public float MaxViewZ;

    [VertexAttribute("prevZ", size: 1)]
    public float PrevZ;

    [VertexAttribute("prevPlaneZ", size: 1)]
    public float PrevPlaneZ;

    [VertexAttribute("lightLevelBufferIndex", size: 1)]
    public float LightLevelBufferIndex;

    [VertexAttribute("sectorIndex", size: 1, required: false)]
    public float SectorIndex;

    public FloodFillVertex(Vec3F pos, float prevZ, float planeZ, float prevPlaneZ, float minPlaneZ, float maxPlaneZ, int lightIndex, int sectorIndex)
    {
        Pos = pos;
        PrevZ = prevZ;
        PlaneZ = planeZ;
        PrevPlaneZ = prevPlaneZ;
        MinViewZ = minPlaneZ;
        MaxViewZ = maxPlaneZ;
        LightLevelBufferIndex = lightIndex;
        SectorIndex = sectorIndex;
    }
}