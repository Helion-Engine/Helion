namespace Helion.Bsp.States.Split
{
    /// <summary>
    /// Score weights for all the negatives when splitting. A higher score
    /// implies 
    /// </summary>
    public class SplitWeights
    {
        /// <summary>
        /// A score that is a multiplication factor for how many splits occur.
        /// It's much nicer if we don't split and create new vertices, but
        /// rather go along segments and intersect at known vertices instead.
        /// </summary>
        public readonly int SplitScoreFactor = 1;

        /// <summary>
        /// The score for not being an axis aligned splitter.
        /// </summary>
        public readonly int NotAxisAlignedScore = 5;

        /// <summary>
        /// The value used to calculate the imbalance of left/right segments.
        /// This is intended to punish splits that put tons of segments on one
        /// side.
        /// </summary>
        public readonly int LeftRightSplitImbalanceScore = 1;

        /// <summary>
        /// A punishment for a split that occurs very close to an endpoint. The
        /// splits near endpoints can create small geometry, and can also lead
        /// to anomalies with floating point arithmetic.
        /// </summary>
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
