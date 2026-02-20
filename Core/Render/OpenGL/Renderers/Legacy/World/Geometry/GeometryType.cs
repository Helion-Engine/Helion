using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public enum GeometryType
{
    Wall,
    TwoSidedMiddleWall,
    Middle3D,
    Flat3D,
    Flat,

    Fuzzy,
    Translucent,
    TranslucentAdd,
    TranslucentColorAdd,
    Count
}

public static class RenderDataStyleExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GeometryType ToGeometryType(this RenderDataStyle style)
    {
        return (GeometryType)((int)style + (int)(GeometryType.Fuzzy - 1));
    }
}