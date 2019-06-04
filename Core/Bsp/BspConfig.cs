using Helion.Bsp.States.Split;

namespace Helion.Bsp
{
    /// <summary>
    /// A simple configuration class that holds tunable parameters
    /// </summary>
    public class BspConfig
    {
        /// <summary>
        /// The epsilon distance to which any verices within this distance will
        /// be assumed to be the same vertex.
        /// </summary>
        public readonly double VertexWeldingEpsilon = Constants.AtomicWidth;

        /// <summary>
        /// The distance (in length, or map units) from an endpoint to which we
        /// consider it a close enough split to punish such a split score-wise
        /// when selecting a splitter.
        /// </summary>
        public readonly double PunishableEndpointDistance = 0.25;

        /// <summary>
        /// The weights for each split criteria.
        /// </summary>
        public readonly SplitWeights SplitWeights = new SplitWeights();

        public BspConfig()
        {
        }

        public BspConfig(double vertexWeldingEpsilon, double punishableEndpointDistance, SplitWeights splitWeights)
        {
            VertexWeldingEpsilon = vertexWeldingEpsilon;
            PunishableEndpointDistance = punishableEndpointDistance;
            SplitWeights = splitWeights;
        }
    }
}
