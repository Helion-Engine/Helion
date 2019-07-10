using System;
using System.Numerics;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.World
{
    public class WorldTriangulator
    {
        private readonly Func<UpperString, Dimension> m_textureDimensionFinder;
        
        private WorldTriangulator(Func<UpperString, Dimension> textureDimensionFinder)
        {
            m_textureDimensionFinder = textureDimensionFinder;
        }

        private static PositionQuad CalculatePosition(PlaneD floor, PlaneD ceiling, Vec2D start, Vec2D end)
        {
            Vector3 topLeft = new Vec3D(start.X, start.Y, ceiling.ToZ(start)).ToFloat();
            Vector3 topRight = new Vec3D(end.X, end.Y, ceiling.ToZ(end)).ToFloat();
            Vector3 bottomLeft = new Vec3D(start.X, start.Y, floor.ToZ(start)).ToFloat();
            Vector3 bottomRight = new Vec3D(end.X, end.Y, floor.ToZ(end)).ToFloat();
            
            return new PositionQuad(topLeft, topRight, bottomLeft, bottomRight);
        }
        
        private UVQuad CalculateUV(Line line, Side front, PositionQuad quad, Dimension textureDimension)
        {
            // TODO
            return new UVQuad(
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            );
        }

        private WallQuad TriangulateSideSection(Line line, Side side, SectorFlat floor, SectorFlat ceiling, 
            UpperString texture, Dimension textureDimension, SideSection section)
        {
            PositionQuad quad = CalculatePosition(floor.Plane, ceiling.Plane, line.StartVertex.Position, line.EndVertex.Position);
            UVQuad uv = CalculateUV(line, side, quad, textureDimension);

            Vertex topLeft = new Vertex(quad.TopLeft, uv.TopLeft);
            Vertex topRight = new Vertex(quad.TopRight, uv.TopRight);
            Vertex bottomLeft = new Vertex(quad.BottomLeft, uv.BottomLeft);
            Vertex bottomRight = new Vertex(quad.BottomRight, uv.BottomRight);
            
            return new WallQuad(topLeft, topRight, bottomLeft, bottomRight, texture, side, floor, ceiling);
        }

        private LineTriangles TriangulateOneSided(Line line)
        {
            UpperString texture = line.Front.MiddleTexture;
            SectorFlat floor = line.Front.Sector.Floor;
            SectorFlat ceiling = line.Front.Sector.Ceiling;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            WallQuad middle = TriangulateSideSection(line, line.Front, floor, ceiling, texture, dimension, SideSection.Middle);
            SideTriangles sideTriangles = new SideTriangles(line.Front, middle);
            return new LineTriangles(line, sideTriangles);
        }

        private WallQuad TriangulateTwoSidedUpper(Side front)
        {
            throw new NotImplementedException("TODO: TriangulateTwoSidedUpper()");
        }

        private WallQuad TriangulateTwoSidedMiddle(Side front)
        {
            throw new NotImplementedException("TODO: TriangulateTwoSidedMiddle()");
        }

        private WallQuad TriangulateTwoSidedLower(Side front)
        {
            throw new NotImplementedException("TODO: TriangulateTwoSidedLower()");
        }

        private SideTriangles TriangulateTwoSidedSide(Side side)
        {
            WallQuad upper = TriangulateTwoSidedUpper(side);
            WallQuad middle = TriangulateTwoSidedMiddle(side);
            WallQuad lower = TriangulateTwoSidedLower(side);
            
            return new SideTriangles(side, middle, upper, lower);
        }

        private LineTriangles TriangulateTwoSided(Line line)
        {
            if (line.Back == null)
                throw new NullReferenceException("Should not have a null back side when triangulating a two-sided line");

            SideTriangles frontTriangles = TriangulateTwoSidedSide(line.Front);
            SideTriangles backTriangles = TriangulateTwoSidedSide(line.Back);
            return new LineTriangles(line, frontTriangles, backTriangles);
        }

        public static LineTriangles Triangulate(Line line, Func<UpperString, Dimension> textureDimensionFinder)
        {
            WorldTriangulator triangulator = new WorldTriangulator(textureDimensionFinder);
            return line.OneSided ? triangulator.TriangulateOneSided(line) : 
                                   triangulator.TriangulateTwoSided(line);
        }
    }
}