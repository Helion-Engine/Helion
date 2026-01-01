using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;

namespace Helion.World.Static;

public struct StaticGeometryData(GeometryData? geometryData, int index, int length)
{
    public GeometryData? GeometryData = geometryData;
    public int Index = index;
    public int Length = length;
}
