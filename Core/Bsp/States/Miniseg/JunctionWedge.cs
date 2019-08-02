using Helion.Bsp.Geometry;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Miniseg
{
    /// <summary>
    /// A helper object which contains a pair of one-sided segments that is
    /// used to determine if we are inside the map or outside.
    /// </summary>
    public class JunctionWedge
    {
        /// <summary>
        /// The segment that has the ending point equaling the outbound start
        /// endpoint.
        /// </summary>
        public BspSegment Inbound;

        /// <summary>
        /// The segment that has the starting point equal to the ending point
        /// of the inbound segment.
        /// </summary>
        public BspSegment Outbound;

        /// <summary>
        /// True if the angle formed by Inbound -> Outbound is obtuse or not.
        /// </summary>
        public bool Obtuse;

        /// <summary>
        /// Creates a wedge from the inbound and outbound segments.
        /// </summary>
        /// <param name="inbound">The inbound segment, which means its ending
        /// point is shared with the outbound starting vertex.</param>
        /// <param name="outbound">The outbound segment, which means its
        /// starting point matches the inbounds ending point.</param>
        public JunctionWedge(BspSegment inbound, BspSegment outbound)
        {
            Precondition(inbound.EndIndex == outbound.StartIndex, "The inbound and outbound do not meet at a shared vertex");
            
            Inbound = inbound;
            Outbound = outbound;
            Obtuse = !inbound.OnRight(outbound);
        }

        /// <summary>
        /// Checks if a point is inside of the wedge area, which means it is
        /// inside the map.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>True if it's between the wedge, false if not.</returns>
        public bool Between(Vec2D point)
        {
            // There are two cases: acute (includes parallel), and obtuse. The 
            // image below has the following properties:
            // - Circle is the point relative to the junction
            // - The tick on the line indicates which side is the right side
            // - X is the current junction point
            // - L or R mean the left or right side from the junction lines
            // - The dots are the extension of the line to aid in seeing the L/R sides
            // - The outbound line has the arrow tip/head, the inbound line does not
            //
            //           R R                      L L
            //        _   o                        o   _
            //       |\       /                \       /|
            //         \/   \/                  \     /
            //     o    \   /   o            o  /\   /\   o
            //    L R    \ /   R L          R L   \ /    L R
            //            X                        X
            //           . .                      . .
            //          .   .                    .   .
            //         .  o  .                  .  o  .
            //        .  L L  .                .  R R  .
            //
            //       Acute case               Obtuse case
            //
            // It is clear that LL or RR combinations are obvious answers, the
            // problem is that obtuse angles have RL/LR being valid, but the 
            // opposite is true for acute angles. This ends up being an easy 
            // case to solve.
            bool rightOfInbound = Inbound.OnRight(point);
            bool rightOfOutbound = Outbound.OnRight(point);

            if (!Obtuse)
            {
                if (rightOfInbound && rightOfOutbound)
                    return true;
            }
            else if (rightOfInbound || rightOfOutbound)
                return true;

            return false;
        }
    }
}