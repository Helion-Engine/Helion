using Helion.World;
using Helion.World.Geometry;

namespace Helion.Render.OpenGL.Legacy.Renderers.World
{
    public class WorldRenderableGeometry
    {
        public bool CheckSubsectorVisibility(ushort index)
        {
            int subsectorIndex = index & BspNodeCompact.SubsectorMask;

            // TODO: If the thing is in a disjoint area of the map, we can't see it
            // TODO: If we can't see the sector (reject table?), exit early
            // TODO: If a segment is back-facing, don't render it

            return true;
        }

        public bool CheckNodeVisibility(ushort index)
        {
            // TODO: Check if we can see the bounding box.

            return true;
        }

        public void Load(WorldBase world)
        {
            // TODO
        }
    }
}
