using Helion.Render.Common.World.Triangulation;

namespace Helion.Render.OpenGL.Renderers.World.Geometry.Static.Walls
{
    public readonly struct StaticWallQuad
    {
        public readonly GLStaticWallGeometryVertex TopLeft;
        public readonly GLStaticWallGeometryVertex TopRight;
        public readonly GLStaticWallGeometryVertex BottomLeft;
        public readonly GLStaticWallGeometryVertex BottomRight;

        public StaticWallQuad(WallTriangulation triangulation)
        {
            // TODO
            TopLeft = default;
            TopRight = default;
            BottomLeft = default;
            BottomRight = default;
        }
    }
}
