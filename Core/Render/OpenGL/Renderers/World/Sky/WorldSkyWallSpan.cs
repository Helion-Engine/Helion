using System.Collections.Generic;
using System.Numerics;

namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public class WorldSkyWallSpan
    {
        public readonly List<WorldSkyWall> Walls = new List<WorldSkyWall>();
        
        public WorldSkyWallSpan(IEnumerable<(Vector2 start, Vector2 end)> vertexPairs, int numPairs)
        {
            // Suppose we have 4 line segments for this span. We want to make
            // the first wall component U coordinate to go from [0.0, 0.25],
            // and the next one from [0.25, 0.5]... etc. We also assume that
            // our vertex pair list is not massive in size so doing a count
            // is acceptable for such a small container.
            float deltaU = 1.0f / numPairs;
            float startU = 0.0f;
            float endU = startU + deltaU;

            foreach (var vertexPair in vertexPairs)
            {
                // Why is nullable references not letting me inline this?
                (Vector2 start, Vector2 end) = vertexPair;
                Walls.Add(ToWall(start, end, startU, endU));
                
                // We'll assume that the amount of error we collect from doing
                // floating point addition is trivial enough to have no impact.
                startU = endU;
                endU += deltaU;
            }
        }

        private static WorldSkyWall ToWall(Vector2 first, Vector2 second, float startU, float endU)
        {
            // We are using NDC coordinates here, so Z is no longer up (Y is).
            //
            // We also are extending along the Y axis from 0.0 -> 1.0 since we
            // will not be doing any transformations except for a rotation for
            // the player's viewing angle.
            //
            // The following are equal to the following:
            //
            //  Left  Right
            //    0    1
            //     +--+      Top
            //     | /|               Upper 
            //     |/ |               wall
            // 2,4 +--+ 3,5  Middle
            //     | /|               Lower 
            //     |/ |               wall
            //     +--+      Bottom
            //    6    7
            WorldSkyVertex upperTopLeft = new WorldSkyVertex(first.X, 1.0f, first.Y, startU, 0.0f);
            WorldSkyVertex upperTopRight = new WorldSkyVertex(second.X, 1.0f, second.Y, endU, 0.0f);
            WorldSkyVertex upperBottomLeft = new WorldSkyVertex(first.X, 0.0f, first.Y, startU, 1.0f);
            WorldSkyVertex upperBottomRight = new WorldSkyVertex(second.X, 0.0f, second.Y, endU, 1.0f);
            WorldSkyVertex lowerMiddleLeft = new WorldSkyVertex(first.X, 0.0f, first.Y, startU, 0.0f);
            WorldSkyVertex lowerMiddleRight = new WorldSkyVertex(second.X, 0.0f, second.Y, endU, 0.0f);
            WorldSkyVertex lowerBottomLeft = new WorldSkyVertex(first.X, -1.0f, first.Y, startU, 1.0f);
            WorldSkyVertex lowerBottomRight = new WorldSkyVertex(second.X, -1.0f, second.Y, endU, 1.0f);
            
            WorldSkyTriangle upperTop = new WorldSkyTriangle(upperTopLeft, upperBottomLeft, upperTopRight);
            WorldSkyTriangle upperBottom = new WorldSkyTriangle(upperTopRight, upperBottomLeft, upperBottomRight);
            WorldSkyTriangle lowerTop = new WorldSkyTriangle(lowerMiddleLeft, lowerBottomLeft, lowerMiddleRight);
            WorldSkyTriangle lowerBottom = new WorldSkyTriangle(lowerMiddleRight, lowerBottomLeft, lowerBottomRight);
            
            return new WorldSkyWall(upperTop, upperBottom, lowerTop, lowerBottom);
        }
    }
}