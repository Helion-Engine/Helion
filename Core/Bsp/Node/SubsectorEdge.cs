using Helion.Maps.Geometry.Lines;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.Node
{
    /// <summary>
    /// An edge of a subsector, or better put: a segment on the edge of a 
    /// convex polygon that is the leaf of a BSP tree.
    /// </summary>
    public class SubsectorEdge
    {
        /// <summary>
        /// The starting vertex.
        /// </summary>
        public Vec2D Start;

        /// <summary>
        /// The ending vertex.
        /// </summary>
        public Vec2D End;
        
        /// <summary>
        /// The line this is a part of.
        /// </summary>
        public readonly Line? Line;
        
        /// <summary>
        /// If this segment is on the front of the line or not. This is not
        /// meaningful if it is a miniseg, and can be either true or false.
        /// </summary>
        public readonly bool IsFront;

        /// <summary>
        /// True if it's a miniseg, false if not.
        /// </summary>
        public bool IsMiniseg => Line == null;

        /// <summary>
        /// Creates a subsector edge from some geometric data and for some
        /// side.
        /// </summary>
        /// <param name="start">The starting point.</param>
        /// <param name="end">The ending point.</param>
        /// <param name="line">The line this is on top of, or null if this is
        /// a miniseg.</param>
        /// <param name="front">True if this is on the front side, false if it
        /// is the back. This value is not used if this is a miniseg. This
        /// must never be false for a one sided line.</param>
        public SubsectorEdge(Vec2D start, Vec2D end, Line? line = null, bool front = true)
        {
            Precondition(line == null || front || line.TwoSided, "Provided a one sided segment and said it uses the back side");
            
            Start = start;
            End = end;
            IsFront = front;
            Line = line;
        }
    }
}