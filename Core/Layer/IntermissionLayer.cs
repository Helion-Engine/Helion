using Helion.Resources.Definitions.MapInfo;
using Helion.World;

namespace Helion.Layer
{
    public class IntermissionLayer : GameLayer
    {
        private readonly IWorld m_world;
        private readonly MapInfoDef m_mapInfo;
        private readonly ClusterDef? m_endGameCluster;
        
        protected override double Priority => 0.65;

        public IntermissionLayer(IWorld world, MapInfoDef mapInfo, ClusterDef? endGameCluster)
        {
            m_world = world;
            m_mapInfo = mapInfo;
            m_endGameCluster = endGameCluster;
        }
    }
}
