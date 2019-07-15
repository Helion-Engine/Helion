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
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared.World
{
    public class WorldTriangulator
    {
        private readonly Func<CIString, Dimension> m_textureDimensionFinder;
        
        public static LineTriangles Triangulate(Line line, Func<CIString, Dimension> textureDimensionFinder)
        {
            WorldTriangulator triangulator = new WorldTriangulator(textureDimensionFinder);
            return line.OneSided ? triangulator.TriangulateOneSided(line) : 
                                   triangulator.TriangulateTwoSided(line);
        }
        
        public static SubsectorTriangles Triangulate(Subsector subsector, Func<CIString, Dimension> textureDimensionFinder)
        {
            WorldTriangulator triangulator = new WorldTriangulator(textureDimensionFinder);
            return triangulator.TriangulateSubsector(subsector);
        }

        private WorldTriangulator(Func<CIString, Dimension> textureDimensionFinder)
        {
            m_textureDimensionFinder = textureDimensionFinder;
        }

        private static void CalculateTwoSidedMiddlePosition(ref Vector3 top, ref Vector3 bottom, 
            Dimension textureDimension, Line line)
        {
            float spanZ = top.Z - bottom.Z;

            // TODO: This does not handle offsets, which can shift the texture
            //       and further change how it's clipped. We will deal with the
            //       offset case at a later date.
            
            // If the texture already fits in the tight gap, then we're done as
            // the UV calculator will do the rest of the work for us. We only
            // need to continue on if the texture won't fit into
            if (spanZ < textureDimension.Height)
                return;

            if (line.Flags.Unpegged.Lower)
                top.Z = bottom.Z + textureDimension.Height;
            else
                bottom.Z = top.Z - textureDimension.Height;
        }

        private static PositionQuad CalculatePosition(Line line, Side side, PlaneD floor, PlaneD ceiling, 
            Vec2D start, Vec2D end, SideSection sideSection, Dimension textureDimension)
        {
            Vector3 topLeft = new Vec3D(start.X, start.Y, ceiling.ToZ(start)).ToFloat();
            Vector3 topRight = new Vec3D(end.X, end.Y, ceiling.ToZ(end)).ToFloat();
            Vector3 bottomLeft = new Vec3D(start.X, start.Y, floor.ToZ(start)).ToFloat();
            Vector3 bottomRight = new Vec3D(end.X, end.Y, floor.ToZ(end)).ToFloat();
            
            // Middle textures on two sided lines are special since they have
            // their geometry clipped based on whether the space for the wall
            // or the texture itself is smaller. It is also unique because the
            // ceiling and floor determination of where to start is different.
            if (line.TwoSided && sideSection == SideSection.Middle)
            {
                if (side.PartnerSide == null)
                    throw new NullReferenceException("Should not have a null opposite side for a two sided middle");
                Side otherSide = side.PartnerSide;
                Sector facingSector = side.Sector;
                Sector otherSector = otherSide.Sector;

                topLeft.Z = (float)Math.Min(facingSector.Ceiling.Plane.ToZ(start), otherSector.Ceiling.Plane.ToZ(start));
                topRight.Z = (float)Math.Min(facingSector.Ceiling.Plane.ToZ(end), otherSector.Ceiling.Plane.ToZ(end));
                bottomLeft.Z = (float)Math.Max(facingSector.Floor.Plane.ToZ(start), otherSector.Floor.Plane.ToZ(start));
                bottomRight.Z = (float)Math.Max(facingSector.Floor.Plane.ToZ(end), otherSector.Floor.Plane.ToZ(end));
                
                CalculateTwoSidedMiddlePosition(ref topLeft, ref bottomLeft, textureDimension, line);
                CalculateTwoSidedMiddlePosition(ref topRight, ref bottomRight, textureDimension, line);
            }

            return new PositionQuad(topLeft, topRight, bottomLeft, bottomRight);
        }

        private static bool CheckIfPegToTop(Line line, SideSection section)
        {
            switch (section)
            {
            case SideSection.Upper:
                // Pegs to the bottom by default, only `upper` overrides it.
                return line.Flags.Unpegged.Upper;
            case SideSection.Middle:
                goto case SideSection.Lower;
            case SideSection.Lower:
                // Middle and lower are pegged to the top by default.
                // TODO: This is not how it's done, it's based off of the top.
                return !line.Flags.Unpegged.Lower;
            default:
                Fail("Unexpected section type when setting UV coordinates");
                break;
            }

            return true;
        }

        private UVQuad CalculateUpperUV(Vector2 offset, float wallSpanU, float leftSpanV, float rightSpanV)
        {
            Vector2 topLeft = new Vector2(offset.X, offset.Y);
            Vector2 topRight = new Vector2(offset.X + wallSpanU, offset.Y);
            Vector2 bottomLeft = new Vector2(offset.X, offset.Y + leftSpanV);
            Vector2 bottomRight = new Vector2(offset.X + wallSpanU, offset.Y + rightSpanV);
            
            return new UVQuad(topLeft, topRight, bottomLeft, bottomRight);
        }
        
        private UVQuad CalculateLowerUV(Vector2 offset, float wallSpanU, float leftSpanV, float rightSpanV)
        {
            Vector2 topLeft = new Vector2(offset.X, 1.0f + offset.Y - leftSpanV);
            Vector2 topRight = new Vector2(offset.X + wallSpanU, 1.0f + offset.Y - rightSpanV);
            Vector2 bottomLeft = new Vector2(offset.X, 1.0f + offset.Y);
            Vector2 bottomRight = new Vector2(offset.X + wallSpanU, 1.0f + offset.Y);

            return new UVQuad(topLeft, topRight, bottomLeft, bottomRight);
        }

        private UVQuad CalculateUV(Line line, Side side, PositionQuad quad, Dimension textureDimension, 
            SideSection section)
        {
            Vector2 invDimension = Vector2.One / textureDimension.ToVector().ToFloat();
            Vector2 offset = side.Offset.ToFloat() * invDimension;
            float wallSpanU = (float)line.Segment.Length() * invDimension.U();
            
            // Note: This will get weird with slopes since we need a reference
            // point. Will need to rethink this later.
            float leftSpanV = (quad.TopLeft.Z - quad.BottomLeft.Z) * invDimension.V();
            float rightSpanV = (quad.TopRight.Z - quad.BottomRight.Z) * invDimension.V();

            return CheckIfPegToTop(line, section) ? 
                CalculateUpperUV(offset, wallSpanU, leftSpanV, rightSpanV) : 
                CalculateLowerUV(offset, wallSpanU, leftSpanV, rightSpanV);
        }

        private WallQuad TriangulateSideSection(Line line, Side side, SectorFlat floor, SectorFlat ceiling, 
            CIString texture, Dimension textureDimension, SideFace sideFace, SideSection section)
        {
            // If looking at the front side we want to go from Start -> End,
            // otherwise the back side needs End -> Start to keep everything
            // counterclockwise.
            Vec2D leftVertex = sideFace == SideFace.Front ? line.StartVertex.Position : line.EndVertex.Position;
            Vec2D rightVertex = sideFace == SideFace.Front ? line.EndVertex.Position : line.StartVertex.Position;
            
            PositionQuad quad = CalculatePosition(line, side, floor.Plane, ceiling.Plane, leftVertex, rightVertex, section, textureDimension);
            UVQuad uv = CalculateUV(line, side, quad, textureDimension, section);

            Vertex topLeft = new Vertex(quad.TopLeft, uv.TopLeft);
            Vertex topRight = new Vertex(quad.TopRight, uv.TopRight);
            Vertex bottomLeft = new Vertex(quad.BottomLeft, uv.BottomLeft);
            Vertex bottomRight = new Vertex(quad.BottomRight, uv.BottomRight);
            
            return new WallQuad(topLeft, topRight, bottomLeft, bottomRight, texture, side, section, floor, ceiling);
        }

        private LineTriangles TriangulateOneSided(Line line)
        {
            CIString texture = line.Front.MiddleTexture;
            SectorFlat floor = line.Front.Sector.Floor;
            SectorFlat ceiling = line.Front.Sector.Ceiling;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            WallQuad middle = TriangulateSideSection(line, line.Front, floor, ceiling, texture, dimension, SideFace.Front, SideSection.Middle);
            SideTriangles sideTriangles = new SideTriangles(line.Front, middle);
            return new LineTriangles(line, sideTriangles);
        }

        private WallQuad TriangulateTwoSidedUpper(Line line, Side facingSide, SideFace sideFace)
        {
            if (facingSide.PartnerSide == null)
                throw new NullReferenceException("Should never have a null back side for an upper two sided line");
            Side backSide = facingSide.PartnerSide;

            CIString texture = facingSide.UpperTexture;
            SectorFlat floor = backSide.Sector.Ceiling;
            SectorFlat ceiling = facingSide.Sector.Ceiling;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            return TriangulateSideSection(line, facingSide, floor, ceiling, texture, dimension, sideFace, SideSection.Upper);
        }

        private WallQuad TriangulateTwoSidedMiddle(Line line, Side facingSide, SideFace sideFace)
        {
            SectorFlat floor = facingSide.Sector.Floor;
            SectorFlat ceiling = facingSide.Sector.Ceiling;
            CIString texture = facingSide.MiddleTexture;
            
            if (texture == Constants.NoTexture)
                return WallQuad.Degenerate(facingSide, SideSection.Middle, floor, ceiling);
            
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);
            return TriangulateSideSection(line, facingSide, floor, ceiling, texture, dimension, sideFace, SideSection.Middle);
        }

        private WallQuad TriangulateTwoSidedLower(Line line, Side facingSide, SideFace sideFace)
        {
            if (facingSide.PartnerSide == null)
                throw new NullReferenceException("Should never have a null back side for a lower two sided line");
            Side backSide = facingSide.PartnerSide;

            CIString texture = facingSide.LowerTexture;
            SectorFlat floor = facingSide.Sector.Floor;
            SectorFlat ceiling = backSide.Sector.Floor;
            Dimension dimension = m_textureDimensionFinder.Invoke(texture);

            return TriangulateSideSection(line, facingSide, floor, ceiling, texture, dimension, sideFace, SideSection.Lower);
        }

        private SideTriangles TriangulateTwoSidedSide(Line line, Side side, SideFace sideFace)
        {
            WallQuad upper = TriangulateTwoSidedUpper(line, side, sideFace);
            WallQuad middle = TriangulateTwoSidedMiddle(line, side, sideFace);
            WallQuad lower = TriangulateTwoSidedLower(line, side, sideFace);
            
            return new SideTriangles(side, middle, upper, lower);
        }

        private LineTriangles TriangulateTwoSided(Line line)
        {
            if (line.Back == null)
                throw new NullReferenceException("Should not have a null back side when triangulating a two-sided line");

            SideTriangles frontTriangles = TriangulateTwoSidedSide(line, line.Front, SideFace.Front);
            SideTriangles backTriangles = TriangulateTwoSidedSide(line, line.Back, SideFace.Back);
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