using System;
using System.Runtime.InteropServices;
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
    public static WallTriangulation From(Wall wall, IRenderableTextureHandle textureHandle)
    {
        return wall.Line.OneSided ? FromOneSided(wall, textureHandle) : FromTwoSided(wall);
    }

    private static WallTriangulation FromOneSided(Wall wall, IRenderableTextureHandle textureHandle)
    {
        Side side = wall.Side;
        Line line = side.Line;
        Sector sector = side.Sector;
        SectorPlane floor = sector.Floor;
        SectorPlane ceiling = sector.Ceiling;
        bool isFront = side.IsFront;

        Vec2F left = isFront ? line.Segment.Start.Float : line.Segment.End.Float;
        Vec2F right = isFront ? line.Segment.End.Float : line.Segment.Start.Float;
        float topZ = (float)ceiling.Z;
        float bottomZ = (float)floor.Z;

        float length = (float)line.Segment.Length;
        float spanZ = topZ - bottomZ;
        Box2F uv = WallTextureMapper.OneSidedWallUV(line, side, length, spanZ, textureHandle);

        WallVertex topLeft = new((left.X, left.Y, topZ), uv.TopLeft);
        WallVertex topRight = new((right.X, right.Y, topZ), uv.TopRight);
        WallVertex bottomLeft = new((left.X, left.Y, bottomZ), uv.BottomLeft);
        WallVertex bottomRight = new((right.X, right.Y, bottomZ), uv.BottomRight);

        return new WallTriangulation(topLeft, topRight, bottomLeft, bottomRight);
    }
    
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
