using Helion.BSP.Builder;
using Helion.BSP.Node;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Util.Geometry;
using NLog;
using static Helion.Util.Assert;

namespace Helion.World.Geometry
{
    public class BspTree
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public Segment[] Segments = new Segment[0];
        public Subsector[] Subsectors = new Subsector[0];
        private BspNodeCompact[] nodes = new BspNodeCompact[0];

        private BspNodeCompact Root => nodes[^1];

        private BspTree(BspNode root)
        {
            Precondition(!root.Degenerate, "Cannot make a BSP tree from a degenerate build");
            CreateComponents(root);
        }

        public static BspTree? Create(Map map)
        {
            OptimizedBspBuilder builder = new OptimizedBspBuilder(map);
            BspNode root = builder.Build();

            if (root.Degenerate)
            {
                log.Error("Cannot create BSP tree for map {0}, it is corrupt", map.Name);
                return null;
            }

            return new BspTree(root);
        }

        private void CreateComponents(BspNode root)
        {
            // TODO: Traverse the root node and make the segs/subsectors/nodes.
        }

        public Subsector ToSubsector(Vec2Fixed point)
        {
            BspNodeCompact node = Root;

            while (true)
            {
                if (node.Splitter.OnRight(point))
                {
                    if (node.IsRightSubsector)
                    {
                        return Subsectors[node.RightChildAsSubsector];
                    }
                    else
                    {
                        node = nodes[node.RightChild];
                    }
                }
                else
                {
                    if (node.IsLeftSubsector)
                    {
                        return Subsectors[node.LeftChildAsSubsector];
                    }
                    else
                    {
                        node = nodes[node.LeftChild];
                    }
                }
            }
        }

        public Sector ToSector(Vec2Fixed point) => ToSubsector(point).Sector;
    }
}
