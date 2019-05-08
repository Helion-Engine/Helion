using Helion.BSP.Geometry;
using Helion.Util.Geometry;

namespace Helion.BSP.States.Miniseg
{
    public class JunctionWedge
    {
        public BspSegment Inbound;
        public BspSegment Outbound;
        public bool Obtuse;

        public JunctionWedge(BspSegment inbound, BspSegment outbound)
        {
            Inbound = inbound;
            Outbound = outbound;
            Obtuse = !inbound.OnRight(outbound);
        }

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
                {
                    return true;
                }
            }
            else if (rightOfInbound || rightOfOutbound)
                return true;

            return false;
        }
    }
}
