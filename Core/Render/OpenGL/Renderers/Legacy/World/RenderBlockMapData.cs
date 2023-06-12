using Helion.Geometry.Vectors;
using Helion.World.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int CheckCount;
        public int MaxDistance;
    }
}
