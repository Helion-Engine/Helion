using Helion.BSP.Geometry;
using System.Collections.Generic;

namespace Helion.BSP.States.Miniseg
{
    public enum MinisegState
    {
        Loaded,
        Working,
        Finished
    }

    public enum VoidStatus
    {
        NotStarted,
        InVoid,
        NotInVoid
    }

    public class MinisegStates
    {
        public MinisegState State = MinisegState.Loaded;
        public VoidStatus VoidStatus = VoidStatus.NotStarted;
        public List<VertexSplitterTime> Vertices = new List<VertexSplitterTime>();
        public List<BspSegment> Minisegs = new List<BspSegment>();
        public int CurrentListIndex = 0;
    }
}
