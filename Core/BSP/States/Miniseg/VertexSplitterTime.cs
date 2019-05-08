using Helion.BSP.Geometry;

namespace Helion.BSP.States.Miniseg
{
    public class VertexSplitterTime
    {
        public VertexIndex Index;
        public double tSplitter;

        public VertexSplitterTime(VertexIndex index, double splitterTime)
        {
            Index = index;
            tSplitter = splitterTime;
        }

        public static bool operator <(VertexSplitterTime first, VertexSplitterTime second)
        {
            return first.tSplitter < second.tSplitter;
        }

        public static bool operator >(VertexSplitterTime first, VertexSplitterTime second)
        {
            return first.tSplitter > second.tSplitter;
        }
    }
}
