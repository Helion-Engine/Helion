using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Vertex;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FloodFillVertex(Vec3F pos, float prevZ, float planeZ, float prevPlaneZ, float minPlaneZ, float maxPlaneZ, float options, float ColorMapAndLightLevel, int mapId)
{
    [VertexAttribute("pos", size: 3)]
    public Vec3F Pos = pos;
    
    [VertexAttribute("planeZ", size: 1)]
    public float PlaneZ = planeZ;
    
    [VertexAttribute("minViewZ", size: 1)]
    public float MinViewZ = minPlaneZ;
    
    [VertexAttribute("maxViewZ", size: 1)]
    public float MaxViewZ = maxPlaneZ;

    [VertexAttribute("prevZ", size: 1)]
    public float PrevZ = prevZ;

    [VertexAttribute("prevPlaneZ", size: 1)]
    public float PrevPlaneZ = prevPlaneZ;

    [VertexAttribute("options", size: 1)]
    public float Options = options;

    [VertexAttribute("colorMapIndex", size: 1, required: false)]
    public float ColorMapIndex = ColorMapAndLightLevel;

    [VertexAttribute("mapId", size: 1, required: false)]
    public float MapId = mapId;
}