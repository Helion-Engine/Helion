using System.Collections.Generic;
using System.Numerics;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Util.Geometry;
using Helion.World.Bsp;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared.World
{
    public static class WorldTriangulator
    {
        public static List<WorldVertex> HandleSubsector(Subsector subsector, SectorFlat flat,
            Dimension textureDimension)
        {
            PlaneD plane = flat.Plane;
            List<SubsectorEdge> edges = subsector.ClockwiseEdges;
            List<WorldVertex> vertices = new List<WorldVertex>();

            if (flat.Facing == SectorFlatFace.Ceiling)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    Vec2D vertex = edges[i].Start;
                    float z = (float)plane.ToZ(vertex);
                    
                    Vector3 position = new Vector3((float)vertex.X, (float)vertex.Y, z);
                    Vector2 uv = CalculateUV(vertex, textureDimension);
                    
                    vertices.Add(new WorldVertex(position, uv));
                }
            }
            else
            {
                // Because the floor is looked at downwards and because it is
                // clockwise, to get counter-clockwise vertices we reverse the
                // iteration order and go from the end vertex.
                for (int i = edges.Count - 1; i >= 0; i--)
                {
                    Vec2D vertex = edges[i].End;
                    float z = (float)plane.ToZ(vertex);
                    
                    Vector3 position = new Vector3((float)vertex.X, (float)vertex.Y, z);
                    Vector2 uv = CalculateUV(vertex, textureDimension);
                    
                    vertices.Add(new WorldVertex(position, uv));
                }
            }

            Postcondition(vertices.Count >= 3, $"Processed a degenerate subsector flat {subsector.Id} (for sector {subsector.Sector.Id})");
            return vertices;
        }

        private static Vector2 CalculateUV(Vec2D vertex, Dimension textureDimension)
        {
            // TODO: Sector offsets will go here eventually.
            Vector2 uv = vertex.ToFloat() / textureDimension.ToVector().ToFloat();
            
            // When we map coordinates to their texture coordinates, because
            // we do division above, a coordinate with Y values of 16 to 32
            // for a 64-dimension texture gets mapped onto 0.25 and 0.5.
            // However the textures are drawn from the top down in vanilla
            // (and all the other ports), which means 16 is effectively 0.75
            // and 32 is 0.5.
            //
            // This means our drawing is inverted along the Y axis, and this is
            // trivially fixed by inverting letting the shader take care of the
            // rest when it clamps it to the image.
            uv.Y = -uv.Y;
            return uv;
        }
    }
}