using Helion.Bsp.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Bsp.States.Miniseg
{
    public class JunctionClassifier
    {
        public void Add(BspSegment bspSegment)
        {
            if (!bspSegment.OneSided)
                return;
            
            // TODO
        }

        public void NotifyDoneAdding()
        {
            // TODO
        }

        public void AddSplitJunction(BspSegment inboundSegment, BspSegment outboundSegment)
        {
            // TODO
        }
    }
}