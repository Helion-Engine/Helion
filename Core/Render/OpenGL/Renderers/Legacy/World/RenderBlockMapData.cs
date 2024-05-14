using Helion.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Islands;

namespace Helion.Render.OpenGL.Renderers.Legacy.World
{
    struct RenderBlockMapData
    {
        public Entity ViewerEntity;
        public Vec2D? OccludePos;
        public Vec2D ViewPosInterpolated;
        public Vec2D ViewDirection;
        public Vec3D ViewPosInterpolated3D;
        public Vec3D ViewPos3D;
        public Island ViewIsland;
        public int CheckCount;
        public int MaxDistance;
        public int MaxDistanceSquared;
    }
}
