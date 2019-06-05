using Helion.Maps.Geometry;
using Helion.Util;
using Helion.World.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Helion.Util.Assert;

namespace Helion.Render.Shared.Triangulator
{
    /// <summary>
    /// A helper class that will take some world and polygonize all the walls
    /// and floors so they can be rendered as triangles.
    /// </summary>
    public static class WorldTriangulator
    {
        /// <summary>
        /// Triangulates all the components of some segment.
        /// </summary>
        /// <param name="segment">The segment to triangulate. This should not
        /// be a miniseg.</param>
        /// <returns>A series of triangulations, or null if this is called on a
        /// miniseg.</returns>
        public static SegmentTriangles? Triangulate(Segment segment)
        {
            if (segment.Side == null || segment.IsMiniseg)
                return null;

            WallTriangles middle = TriangulateMiddle(segment, segment.Side.Sector);
            WallTriangles? upper = null;
            WallTriangles? lower = null;

            if (segment.Line != null && segment.Line.TwoSided)
            {
                Sector? backSector = segment.Side.PartnerSide?.Sector;
                if (backSector == null)
                    throw new HelionException("Should never fail to get a front or back side from a two sided line.");

                upper = TriangulateUpper(segment, segment.Side.Sector, backSector);
                lower = TriangulateLower(segment, segment.Side.Sector, backSector);
            }

            // Needed for nullable types since it thinks we could mutate the
            // field and make it null.
            if (segment.Line == null)
                throw new HelionException("Should never have a null line with a non-miniseg");

            return new SegmentTriangles(segment, segment.Line, segment.Side, upper, middle, lower);
        }

        private static WallTriangles TriangulateMiddle(Segment segment, Sector sector)
        {
            (Triangle upper, Triangle lower) = TriangulateWall(segment, sector.Floor, sector.Ceiling);
            return new WallTriangles(sector.Floor, sector.Ceiling, upper, lower);
        }

        private static WallTriangles TriangulateUpper(Segment segment, Sector frontSector, Sector backSector)
        {
            Precondition(segment.Line?.TwoSided ?? false, "Should not be upper triangulating a segment for a one-sided line (or a miniseg)");

            SectorFlat floor = backSector.Ceiling;
            SectorFlat ceiling = frontSector.Ceiling;
            (Triangle upper, Triangle lower) = TriangulateWall(segment, floor, ceiling);
            return new WallTriangles(floor, ceiling, upper, lower);
        }

        private static WallTriangles TriangulateLower(Segment segment, Sector frontSector, Sector backSector)
        {
            Precondition(segment.Line?.TwoSided ?? false, "Should not be lower triangulating a segment for a one sided (or miniseg) line");

            SectorFlat floor = frontSector.Floor;
            SectorFlat ceiling = backSector.Floor;
            (Triangle upper, Triangle lower) = TriangulateWall(segment, floor, ceiling);
            return new WallTriangles(floor, ceiling, upper, lower);
        }

        private static (Triangle, Triangle) TriangulateWall(Segment segment, SectorFlat floor, SectorFlat ceiling)
        {
            // The segment is triangulated as follows:
            //
            //   Upper
            //   0---2   0         Each index at the vertices is the order to
            //   |  /   /|         which the vertex is created to make it have
            //   | /   / |         a counter-clockwise rotation.
            //   |/   /  |         This means the upper triangle for edge 1-2
            //   1   1---2         shares vertices w/ lower triangle edge 0-1.
            //       Lower

            Vector2 start = segment.Start.ToFloat();
            Vector2 end = segment.End.ToFloat();

            // TODO: When we get planes, we need to calculate them for the Z.
            Vector3 topLeft = new Vector3(start.X, start.Y, ceiling.Z);
            Vector3 topRight = new Vector3(end.X, end.Y, ceiling.Z);
            Vector3 bottomLeft = new Vector3(start.X, start.Y, floor.Z);
            Vector3 bottomRight = new Vector3(end.X, end.Y, floor.Z);

            Triangle upper = new Triangle(topLeft, bottomLeft, topRight);
            Triangle lower = new Triangle(topRight, bottomLeft, bottomRight);
            return (upper, lower);
        }

        /// <summary>
        /// Triangulates the subsector.
        /// </summary>
        /// <param name="subsector">The subsector to triangulate.</param>
        /// <returns>A subsector triangulation.</returns>
        public static SubsectorTriangles Triangulate(Subsector subsector)
        {
            List<Segment> segs = subsector.ClockwiseEdges;
            Precondition(segs.Count >= 3, "Cannot triangulate a degenerate subsector");

            // TODO: Properly handle slopes when the time comes.
            float floorZ = subsector.Sector.Floor.Z;
            float ceilZ = subsector.Sector.Ceiling.Z;
            Vector3 root = new Vector3(segs.First().Start.ToFloat(), ceilZ);

            var fanVertices = segs.Skip(1).SkipLast(1)
                                  .Select(e => (new Vector3(e.Start.ToFloat(), ceilZ), new Vector3(e.End.ToFloat(), ceilZ)))
                                  .Select(fan => new Triangle(root, fan.Item1, fan.Item2));

            List<Triangle> ceiling = fanVertices.ToList();

            List<Triangle> floor = fanVertices.Reverse()
                                              .Select(tri => new Triangle(tri.First, tri.Third, tri.Second))
                                              .ToList();

            return new SubsectorTriangles(subsector, floor, ceiling);
        }
    }
}
