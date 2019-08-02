namespace Helion.Bsp.States.Split
{
    /// <summary>
    /// Score weights for all the negatives when splitting. A larger score
    /// implies it is a worse candidate for being a splitter segment.
    /// </summary>
    public class SplitWeights
    {
        /// <summary>
        /// A score that is a multiplication factor for how many splits occur.
        /// It's much nicer if we don't split and create new vertices, but
        /// rather go along segments and intersect at known vertices instead.
        /// </summary>
        public int SplitScoreFactor = 1;

        /// <summary>
        /// The score for not being an axis aligned splitter.
        /// </summary>
        public int NotAxisAlignedScore = 5;

        /// <summary>
        /// The value used to calculate the imbalance of left/right segments.
        /// This is intended to punish splits that put tons of segments on one
        /// side.
        /// </summary>
        public int LeftRightSplitImbalanceScore = 1;

        /// <summary>
        /// A punishment for a split that occurs very close to an endpoint. The
        /// splits near endpoints can create small geometry, and can also lead
        /// to anomalies with floating point arithmetic.
        /// </summary>
        public int NearEndpointSplitScore = 1000;
    }
}