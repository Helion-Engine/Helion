using Helion.BSP.States.Split;

namespace Helion.BSP
{
    public class BspConfig
    {
        public readonly double VertexWeldingEpsilon = Constants.AtomicWidth;
        public readonly double PunishableEndpointDistance = 0.25;
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
