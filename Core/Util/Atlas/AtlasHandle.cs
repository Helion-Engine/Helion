using Helion.Util.Geometry;

namespace Helion.Util.Atlas
{
    public class AtlasHandle
    {
        internal AtlasNode Node;

        public Box2I Location => Node.Location;

        internal AtlasHandle(AtlasNode node)
        {
            Node = node;
        }
    }
}