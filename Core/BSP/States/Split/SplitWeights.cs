namespace Helion.BSP.States.Split
{
    public class SplitWeights
    {
        public readonly int SplitScoreFactor = 1;
        public readonly int NotAxisAlignedScore = 5;
        public readonly int LeftRightSplitImbalanceScore = 1;
        public readonly int NearEndpointSplitScore = 1000;

        public SplitWeights()
        {
        }

        public SplitWeights(int splitScoreFactor, int notAxisAlignedScore, int leftRightSplitImbalanceScore, int nearEndpointSplitScore)
        {
            SplitScoreFactor = splitScoreFactor;
            NotAxisAlignedScore = notAxisAlignedScore;
            LeftRightSplitImbalanceScore = leftRightSplitImbalanceScore;
            NearEndpointSplitScore = nearEndpointSplitScore;
        }
    }
}
