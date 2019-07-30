using Helion.Bsp.States.Split;

namespace Helion.Bsp
{
    /// <summary>
    /// A simple configuration class that holds tunable parameters
    /// </summary>
    public class BspConfig
    {
        /// <summary>
        /// The epsilon distance to which any vertices within this distance will
        /// be assumed to be the same vertex.
        /// </summary>
        public readonly double VertexWeldingEpsilon = 0.005;

        /// <summary>
        /// The distance (in length, or map units) from an endpoint to which we
        /// consider it a close enough split to punish such a split score-wise
        /// when selecting a splitter.
        /// </summary>
        public readonly double PunishableEndpointDistance = 0.1;

        /// <summary>
        /// The weights for each split criteria.
        /// </summary>
        public readonly SplitWeights SplitWeights = new SplitWeights();

        /// <summary>
        /// Creates a BSP config with the default values.
        /// </summary>
        public BspConfig()
        {
        }

        /// <summary>
        /// Creates a BSP config with the values provided.
        /// </summary>
        /// <param name="vertexWeldingEpsilon">The distance on the map before
        /// the vertices weld together.</param>
        /// <param name="punishableEndpointDistance">The distance in map units
        /// where a heavy punishment is applied when trying to determine which
        /// line to use as a splitter.</param>
        /// <param name="splitWeights">The weights of splitting which determine
        /// which line of all the available lines will be the splitter.</param>
        public BspConfig(double vertexWeldingEpsilon, double punishableEndpointDistance, SplitWeights splitWeights)
        {
            VertexWeldingEpsilon = vertexWeldingEpsilon;
            PunishableEndpointDistance = punishableEndpointDistance;
            SplitWeights = splitWeights;
        }
    }
}