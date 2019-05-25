using Helion.BSP.Geometry;
using System;

namespace Helion.BSP.States.Miniseg
{
    public class VertexSplitterTime : IComparable<VertexSplitterTime>
    {
        public VertexIndex Index;
        public double tSplitter;

        public VertexSplitterTime(VertexIndex index, double splitterTime)
        {
            Index = index;
            tSplitter = splitterTime;
        }

        public int CompareTo(VertexSplitterTime other) => tSplitter.CompareTo(other.tSplitter);
    }
}
