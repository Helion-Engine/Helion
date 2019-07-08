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
        /// <param name="subsectorEdge">The segment to triangulate. This should not
        /// be a miniseg.</param>
        /// <returns>A series of triangulations, or null if this is called on a
        /// miniseg.</returns>
        public static SegmentWalls? Triangulate(SubsectorEdge subsectorEdge)
        {
            if (subsectorEdge.Side == null || subsectorEdge.IsMiniseg)
                return null;

            WallQuad middle = TriangulateMiddle(subsectorEdge, subsectorEdge.Side.Sector);
            WallQuad? upper = null;
            WallQuad? lower = null;

            if (subsectorEdge.Line != null && subsectorEdge.Line.TwoSided)
            {
                Sector? backSector = subsectorEdge.Side.PartnerSide?.Sector;
                if (backSector == null)
                    throw new HelionException("Should never fail to get a front or back side from a two sided line.");

                upper = TriangulateUpper(subsectorEdge, subsectorEdge.Side.Sector, backSector);
                lower = TriangulateLower(subsectorEdge, subsectorEdge.Side.Sector, backSector);
            }

            // Needed for nullable types since it thinks we could mutate the
            // field and make it null.
            if (subsectorEdge.Line == null)
                throw new HelionException("Should never have a null line with a non-miniseg");

            return new SegmentWalls(subsectorEdge, subsectorEdge.Line, subsectorEdge.Side, upper, middle, lower);
        }

        private static WallQuad TriangulateMiddle(SubsectorEdge subsectorEdge, Sector sector)
        {
            (Triangle upper, Triangle lower) = TriangulateWall(subsectorEdge, sector.Floor, sector.Ceiling);
            return new WallQuad(sector.Floor, sector.Ceiling, upper, lower);
        }

        private static WallQuad TriangulateUpper(SubsectorEdge subsectorEdge, Sector frontSector, Sector backSector)
        {
            Precondition(subsectorEdge.Line?.TwoSided ?? false, "Should not be upper triangulating a segment for a one-sided line (or a miniseg)");

            SectorFlat floor = backSector.Ceiling;
            SectorFlat ceiling = frontSector.Ceiling;
            (Triangle upper, Triangle lower) = TriangulateWall(subsectorEdge, floor, ceiling);
            return new WallQuad(floor, ceiling, upper, lower);
        }

        private static WallQuad TriangulateLower(SubsectorEdge subsectorEdge, Sector frontSector, Sector backSector)
        {
            Precondition(subsectorEdge.Line?.TwoSided ?? false, "Should not be lower triangulating a segment for a one sided (or miniseg) line");

            SectorFlat floor = frontSector.Floor;
            SectorFlat ceiling = backSector.Floor;
            (Triangle upper, Triangle lower) = TriangulateWall(subsectorEdge, floor, ceiling);
            return new WallQuad(floor, ceiling, upper, lower);
        }

        private static (Triangle, Triangle) TriangulateWall(SubsectorEdge subsectorEdge, SectorFlat floor, SectorFlat ceiling)
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

            Vector2 start = subsectorEdge.Start.ToFloat();
            Vector2 end = subsectorEdge.End.ToFloat();

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
            List<SubsectorEdge> segs = subsector.ClockwiseEdges;
            Precondition(segs.Count >= 3, "Cannot triangulate a degenerate subsector");

            // TODO: Properly handle slopes when the time comes.
            float floorZ = subsector.Sector.Floor.Z;
            float ceilZ = subsector.Sector.Ceiling.Z;

            List<Vector3> ceiling = segs.Select(seg => new Vector3(seg.Start.ToFloat(), ceilZ)).ToList();
            List<Vector3> floor = ceiling.Select(pos => new Vector3(pos.X, pos.Y, floorZ)).ToList();
            
            // We want to pass a simple list of vertices to the constructor for
            // subsector triangles, and the vertices be in a forward iteration
            // order. Because triangles have to be counter-clockwise, since we
            // have clockwise vertices as defined by the BSP builder, we have
            // to reverse the floor so the 'forward' iteration in the subsector
            // triangles ends up creating them in a counter-clockwise order.
            //
            // This keeps the constructor agnostic of the ordering by having us
            // do the reversal here instead.
            floor.Reverse();

            return new SubsectorTriangles(subsector, floor, ceiling);
        }
    }
}
