using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using System.Runtime.CompilerServices;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry;

public enum GeometryType
{
    Wall,
    Middle3D,
    Flat,
    TwoSidedMiddleWall,

    Fuzzy,
    Translucent,
    TranslucentAdd,
    TranslucentColorAdd,
    Count
}

public static class GeometryTypeExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NeedsCoverWall(this GeometryType style)
    {
        return (int)style < (int)GeometryType.TwoSidedMiddleWall;
    }
}

public static class RenderDataStyleExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static GeometryType ToGeometryType(this RenderDataStyle style)
    {
        return (GeometryType)((int)style + (int)(GeometryType.Fuzzy - 1));
    }
}