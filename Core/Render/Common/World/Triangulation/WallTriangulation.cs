using System;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Textures;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;

namespace Helion.Render.Common.World.Triangulation;

public readonly record struct WallTriangulation(WallVertex TopLeft, WallVertex TopRight, WallVertex BottomLeft, WallVertex BottomRight)
{    
    private static WallTriangulation FromTwoSided(Wall wall)
    {
        switch (wall.Location)
        {
            case WallLocation.Upper:
                return FromTwoSidedUpper(wall);
            case WallLocation.Middle:
                return FromTwoSidedMiddle(wall);
            case WallLocation.Lower:
                return FromTwoSidedLower(wall);
            default:
                throw new Exception($"Unsupported wall location type when triangulating: {wall.Location}");
        }
    }
    
    private static WallTriangulation FromTwoSidedLower(Wall wall)
    {
        return default;
    }
    
    private static WallTriangulation FromTwoSidedUpper(Wall wall)
    {
        return default;
    }
    
    private static WallTriangulation FromTwoSidedMiddle(Wall wall)
    {
        return default;
    }
}
