using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public struct WorldSkyTriangle
    {
        public readonly WorldSkyVertex First;
        public readonly WorldSkyVertex Second;
        public readonly WorldSkyVertex Third;

        public WorldSkyTriangle(WorldSkyVertex first, WorldSkyVertex second, WorldSkyVertex third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }
    
    public struct WorldSkyWall
    {
        public WorldSkyTriangle UpperTop;
        public WorldSkyTriangle UpperBottom;
        public WorldSkyTriangle LowerTop;
        public WorldSkyTriangle LowerBottom;

        public WorldSkyWall(WorldSkyTriangle upperTop, WorldSkyTriangle upperBottom, WorldSkyTriangle lowerTop, 
            WorldSkyTriangle lowerBottom)
        {
            UpperTop = upperTop;
            UpperBottom = upperBottom;
            LowerTop = lowerTop;
            LowerBottom = lowerBottom;
        }

        public List<WorldSkyTriangle> GetTriangles()
        {
            return new List<WorldSkyTriangle>{UpperTop, UpperBottom, LowerTop, LowerBottom};
        }
    }
}