using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Miniseg
{
    /// <summary>
    /// The states the miniseg generator can be in.
    /// </summary>
    public enum MinisegState
    {
        Loaded,
        Working,
        Finished
    }

    /// <summary>
    /// Information on whether the miniseg creator was passing through the void
    /// (the empty space outside of the map) or the non-void inside of the map.
    /// </summary>
    public enum VoidStatus
    {
        NotStarted,
        InVoid,
        NotInVoid
    }

    /// <summary>
    /// All the states for the miniseg creator.
    /// </summary>
    public class MinisegStates
    {
        public MinisegState State = MinisegState.Loaded;
        public VoidStatus VoidStatus = VoidStatus.NotStarted;
        public List<VertexSplitterTime> Vertices = new List<VertexSplitterTime>();
        public List<BspSegment> Minisegs = new List<BspSegment>();
        public int CurrentVertexListIndex = 0;
    }
}
