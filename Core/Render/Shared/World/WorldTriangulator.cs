using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.World.Geometry;

namespace Helion.Render.Shared.World
{
    public class WorldTriangulator
    {
        private readonly Func<CiString, Dimension> m_textureDimensionFinder;
        
        public static LineTriangles Triangulate(Line line, Func<CiString, Dimension> textureDimensionFinder)
        {
            WorldTriangulator triangulator = new WorldTriangulator(textureDimensionFinder);
            return line.OneSided ? triangulator.TriangulateOneSided(line) : 
                triangulator.TriangulateTwoSided(line);
        }
        
        public static SubsectorTriangles Triangulate(Subsector subsector, Func<CiString, Dimension> textureDimensionFinder)
        {
            WorldTriangulator triangulator = new WorldTriangulator(textureDimensionFinder);
            return triangulator.TriangulateSubsector(subsector);
        }

        private WorldTriangulator(Func<CiString, Dimension> textureDimensionFinder)
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
            CiString texture, Dimension textureDimension, SideSection section)
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
            CiString texture = line.Front.MiddleTexture;
            SectorFlat floor = line.Front.Sector.Floor;
            SectorFlat ceiling = line.Front.Sector.Ceiling;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            WallQuad middle = TriangulateSideSection(line, line.Front, floor, ceiling, texture, dimension, SideSection.Middle);
            SideTriangles sideTriangles = new SideTriangles(line.Front, middle);
            return new LineTriangles(line, sideTriangles);
        }

        private WallQuad TriangulateTwoSidedUpper(Line line, Side facingSide)
        {
            if (facingSide.PartnerSide == null)
                throw new NullReferenceException("Should never have a null back side for an upper two sided line");
            Side backSide = facingSide.PartnerSide;

            CiString texture = facingSide.UpperTexture;
            SectorFlat floor = backSide.Sector.Ceiling;
            SectorFlat ceiling = facingSide.Sector.Ceiling;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            return TriangulateSideSection(line, facingSide, floor, ceiling, texture, dimension, SideSection.Upper);
        }

        private WallQuad TriangulateTwoSidedMiddle(Line line, Side facingSide)
        {
            CiString texture = facingSide.MiddleTexture;
            if (texture == Constants.NoTexture)
                return WallQuad.Degenerate(facingSide, facingSide.Sector.Floor, facingSide.Sector.Ceiling);

            // TODO: It has a middle texture so create it properly.
            return WallQuad.Degenerate(facingSide, facingSide.Sector.Floor, facingSide.Sector.Ceiling);
        }

        private WallQuad TriangulateTwoSidedLower(Line line, Side facingSide)
        {
            if (facingSide.PartnerSide == null)
                throw new NullReferenceException("Should never have a null back side for a lower two sided line");
            Side backSide = facingSide.PartnerSide;

            CiString texture = facingSide.LowerTexture;
            SectorFlat floor = facingSide.Sector.Floor;
            SectorFlat ceiling = backSide.Sector.Floor;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            return TriangulateSideSection(line, facingSide, floor, ceiling, texture, dimension, SideSection.Lower);
        }

        private SideTriangles TriangulateTwoSidedSide(Line line, Side side)
        {
            WallQuad upper = TriangulateTwoSidedUpper(line, side);
            WallQuad middle = TriangulateTwoSidedMiddle(line, side);
            WallQuad lower = TriangulateTwoSidedLower(line, side);
            
            return new SideTriangles(side, middle, upper, lower);
        }

        private LineTriangles TriangulateTwoSided(Line line)
        {
            if (line.Back == null)
                throw new NullReferenceException("Should not have a null back side when triangulating a two-sided line");

            SideTriangles frontTriangles = TriangulateTwoSidedSide(line, line.Front);
            SideTriangles backTriangles = TriangulateTwoSidedSide(line, line.Back);
            return new LineTriangles(line, frontTriangles, backTriangles);
        }

        private static Vertex CalculateFlatVertex(Vec2D position, PlaneD plane, Dimension textureDimension)
        {
            Vec3D pos = new Vec3D(position.X, position.Y, plane.ToZ(position));
            Vec2D uv = position / textureDimension.ToVector().ToDouble();
            
            return new Vertex(pos.ToFloat(), uv.ToFloat());
        }
        
        private SubsectorFlatFan TriangulateFlat(Subsector subsector, SectorFlat flat, Sector sector)
        {
            Dimension textureDimension = m_textureDimensionFinder(flat.Texture);
            
            var edges = subsector.ClockwiseEdges;
            Vertex root = CalculateFlatVertex(edges.First().Start, flat.Plane, textureDimension);
            List<Vertex> fan = edges.Skip(1)
                                    .Select(edge => edge.Start)
                                    .Select(pos => CalculateFlatVertex(pos, flat.Plane, textureDimension))
                                    .ToList();

            // When looking down at the ground, we'd get clockwise vertices,
            // which get culled by virtue of being CW instead of CCW. Doing a
            // reverse is a trivial fix.
            if (flat.Facing == SectorFlatFace.Floor)
                fan.Reverse();
            
            return new SubsectorFlatFan(root, fan, flat.Texture.ToString(), sector);
    }

        private SubsectorTriangles TriangulateSubsector(Subsector subsector)
        {
            Sector sector = subsector.Sector;
            SubsectorFlatFan floorFan = TriangulateFlat(subsector, sector.Floor, sector);
            SubsectorFlatFan ceilingFan = TriangulateFlat(subsector, sector.Ceiling, sector);
            
            return new SubsectorTriangles(floorFan, ceilingFan);
        }
    }
}