using static Helion.Util.Assert;

namespace Helion.BSP.States.Convex
{
    public class VertexCountTracker
    {
        private int withOneLine = 0;
        private int withTwoLines = 0;
        private int withThreeOrMoreLines = 0;

        public bool OnlyTwoLines => withOneLine == 0 && withTwoLines > 0 && withThreeOrMoreLines == 0;
        public bool HasTripleJunction => withThreeOrMoreLines > 0;
        public bool HasTerminalLine => withOneLine > 0;

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

        public void Reset()
        {
            withOneLine = 0;
            withTwoLines = 0;
            withThreeOrMoreLines = 0;
        }
    }
}
