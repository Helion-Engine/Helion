using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Convex
{
    /// <summary>
    /// Keeps track of how many times a segment enters and exits a vertex. This
    /// tells us very quickly whether it's definitely not convex, or if it is
    /// worth testing further for convexity.
    /// </summary>
    public class VertexCountTracker
    {
        private int withOneLine = 0;
        private int withTwoLines = 0;
        private int withThreeOrMoreLines = 0;

        /// <summary>
        /// True if each vertex has two segments coming in/out of it, false
        /// otherwise.
        /// </summary>
        public bool OnlyTwoLines => withOneLine == 0 && withTwoLines > 0 && withThreeOrMoreLines == 0;

        /// <summary>
        /// True if at least one vertex has 3+ segments going out of it, false
        /// otherwise.
        /// </summary>
        /// <remarks>
        /// This being true means it is not convex (aka: splittable).
        /// </remarks>
        public bool HasTripleJunction => withThreeOrMoreLines > 0;

        /// <summary>
        /// Tells us whether there is a dangling vertex that only has one 
        /// segment entering or exiting from it.
        /// </summary>
        /// <remarks>
        /// This is perfectly normal in a map, but it tells us that we have a
        /// split to do or that there's some degeneracy.
        /// </remarks>
        public bool HasTerminalLine => withOneLine > 0;

        /// <summary>
        /// Tracks a new count for a vertex. This should be invoked every time
        /// a vertex is found to have some segment using it.
        /// </summary>
        /// <remarks>
        /// This is intended to be called for every single time a segment is
        /// found at an endpoint. Suppose there is a vertex that has 3 segments
        /// coming out of it. We'd call this function the first time we make it 
        /// with a value of 1. Then when we find the next segment that uses the
        /// vertex, we'd call this with an argument of 2. Finally when we find
        /// the third segment that shares that vertex, we'd call this yet again
        /// with an argument of 3.
        /// </remarks>
        /// <param name="inboundOutboundCount">A counter for what was just 
        /// found at some vertex.</param>
        public void Track(int inboundOutboundCount)
        {
            switch (inboundOutboundCount)
            {
            case 1:
                withOneLine++;
                break;
            case 2:
                withOneLine--;
                withTwoLines++;
                break;
            case 3:
                withTwoLines--;
                withThreeOrMoreLines++;
                break;
            default:
                Fail("Should not be tracking a number that is not 1, 2, or 3");
                break;
            }
        }

        /// <summary>
        /// Resets the statistics of this object.
        /// </summary>
        public void Reset()
        {
            withOneLine = 0;
            withTwoLines = 0;
            withThreeOrMoreLines = 0;
        }
    }
}
