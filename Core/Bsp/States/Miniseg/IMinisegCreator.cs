using System.Collections.Generic;
using Helion.Bsp.Geometry;

namespace Helion.Bsp.States.Miniseg
{
    /// <summary>
    /// The instance responsible for creating minisegs, which are segments that
    /// do not exist in the lines of the original map but are required to make
    /// a convex enclosed polygon (aka, the subsector, a leaf in the BSP tree).
    /// </summary>
    public interface IMinisegCreator
    {
        /// <summary>
        /// All the states for the miniseg creator.
        /// </summary>
        MinisegStates States { get; }

        /// <summary>
        /// Loads all the collinear vertices that were found from the splitter.
        /// </summary>
        /// <param name="splitter">The splitter that was used.</param>
        /// <param name="collinearVertices">All the vertices that lay on the
        /// line of the splitter, including ones that were created by the 
        /// splitter when partitioning the map.</param>
        void Load(BspSegment splitter, HashSet<int> collinearVertices);

        /// <summary>
        /// Advances to the next state, which may create minisegs.
        /// </summary>
        void Execute();
    }
}