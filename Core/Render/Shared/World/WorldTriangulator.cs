using System;
using System.Collections.Generic;
using System.Numerics;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Container;
using Helion.Util.Geometry;
using Helion.World.Bsp;
using Helion.World.Physics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Shared.World
{
    public static class WorldTriangulator
    {
        // TODO: There is probably a lot of repetition for the wall geometry
        //       generators. Let's refactor it later.

        public static WallVertices HandleOneSided(Line line, Side side, Vector2 textureUVInverse)
        {
            Sector sector = side.Sector;
            SectorFlat floor = sector.Floor;
            SectorFlat ceiling = sector.Ceiling;

            Vec2D left = line.Segment.Start;
            Vec2D right = line.Segment.End;
            double topZ = ceiling.Z;
            double bottomZ = floor.Z;

            double length = line.Segment.Length();
            double spanZ = topZ - bottomZ;
            WallUV uv = CalculateOneSidedWallUV(line, side, length, textureUVInverse, spanZ);
            
            WorldVertex topLeft = new WorldVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
            WorldVertex topRight = new WorldVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
            WorldVertex bottomLeft = new WorldVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
            WorldVertex bottomRight = new WorldVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);
            
            return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
        }

        public static WallVertices HandleTwoSidedLower(Line line, Side facingSide, Side otherSide, Vector2 textureUVInverse)
        {
            Sector sector = facingSide.Sector;
            SectorFlat topFlat = otherSide.Sector.Floor;
            SectorFlat bottomFlat = sector.Floor;
            
            Vec2D left = line.Segment.Start;
            Vec2D right = line.Segment.End;
            double topZ = topFlat.Z;
            double bottomZ = bottomFlat.Z;
            
            double length = line.Segment.Length();
            WallUV uv = CalculateTwoSidedLowerWallUV(line, facingSide, length, textureUVInverse, topZ, bottomZ);
            
            WorldVertex topLeft = new WorldVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
            WorldVertex topRight = new WorldVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
            WorldVertex bottomLeft = new WorldVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
            WorldVertex bottomRight = new WorldVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);
            
            return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
        }
        
        public static WallVertices HandleTwoSidedMiddle(Line line, Side facingSide, Side otherSide, 
            Dimension textureDimension, Vector2 textureUVInverse, LineOpening opening, out bool nothingVisibleToDraw)
        {
            Precondition(opening.OpeningHeight > 0, "Should not be handling a two sided middle when there's no opening");

            Vec2D left = line.Segment.Start;
            Vec2D right = line.Segment.End;
            double topZ = opening.CeilingZ;
            double bottomZ = opening.FloorZ;

            if (line.Flags.Unpegged.Lower)
                topZ = bottomZ + textureDimension.Height;
            else
                bottomZ = topZ - textureDimension.Height;

            topZ += facingSide.Offset.Y;
            bottomZ += facingSide.Offset.Y;
            
            // We want to clip it to the line opening.
            topZ = Math.Min(topZ, opening.CeilingZ);
            bottomZ = Math.Max(bottomZ, opening.FloorZ);

            if (topZ <= bottomZ)
            {
                nothingVisibleToDraw = true;
                return default;
            }
            
            double length = line.Segment.Length();
            WallUV uv = CalculateTwoSidedMiddleWallUV(facingSide, length, textureUVInverse);
            
            WorldVertex topLeft = new WorldVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
            WorldVertex topRight = new WorldVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
            WorldVertex bottomLeft = new WorldVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
            WorldVertex bottomRight = new WorldVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);

            nothingVisibleToDraw = false;
            return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
        }

        public static WallVertices HandleTwoSidedUpper(Line line, Side facingSide, Side otherSide, Vector2 textureUVInverse)
        {
            Sector sector = facingSide.Sector;
            SectorFlat topFlat = sector.Ceiling;
            SectorFlat bottomFlat = otherSide.Sector.Ceiling;
            
            Vec2D left = line.Segment.Start;
            Vec2D right = line.Segment.End;
            double topZ = topFlat.Z;
            double bottomZ = bottomFlat.Z;
            double spanZ = topZ - bottomZ;
            
            // TODO: If unchanging, we can pre-calculate the length.
            double length = line.Segment.Length();
            WallUV uv = CalculateTwoSidedUpperWallUV(line, facingSide, length, textureUVInverse, spanZ);
            
            WorldVertex topLeft = new WorldVertex(left.X, left.Y, topZ, uv.TopLeft.X, uv.TopLeft.Y);
            WorldVertex topRight = new WorldVertex(right.X, right.Y, topZ, uv.BottomRight.X, uv.TopLeft.Y);
            WorldVertex bottomLeft = new WorldVertex(left.X, left.Y, bottomZ, uv.TopLeft.X, uv.BottomRight.Y);
            WorldVertex bottomRight = new WorldVertex(right.X, right.Y, bottomZ, uv.BottomRight.X, uv.BottomRight.Y);
            
            return new WallVertices(topLeft, topRight, bottomLeft, bottomRight);
        }

        /// <summary>
        /// Triangulates a subsector by populating the provided dynamic array
        /// of vertices.
        /// </summary>
        /// <param name="subsector">The subsector to triangulate.</param>
        /// <param name="flat">The flat plane for the subsector.</param>
        /// <param name="textureDimension">The texture dimension.</param>
        /// <param name="verticesToPopulate">An output array where vertices are
        /// written to upon triangulating.</param>
        public static void HandleSubsector(Subsector subsector, SectorFlat flat, Dimension textureDimension, 
            DynamicArray<WorldVertex> verticesToPopulate)
        {
            Precondition(subsector.ClockwiseEdges.Count >= 3, "Cannot render subsector when it's degenerate (should have 3+ edges)");
            
            PlaneD plane = flat.Plane;
            List<SubsectorEdge> edges = subsector.ClockwiseEdges;
            verticesToPopulate.Clear();

            if (flat.Facing == SectorFlatFace.Ceiling)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    Vec2D vertex = edges[i].Start;
                    float z = (float)plane.ToZ(vertex);
                    
                    Vector3 position = new Vector3((float)vertex.X, (float)vertex.Y, z);
                    Vector2 uv = CalculateFlatUV(vertex, textureDimension);
                    
                    verticesToPopulate.Add(new WorldVertex(position, uv));
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
                    Vector2 uv = CalculateFlatUV(vertex, textureDimension);
                    
                    verticesToPopulate.Add(new WorldVertex(position, uv));
                }
            }
        }

        private static WallUV CalculateOneSidedWallUV(Line line, Side side, double length, 
            Vector2 textureUVInverse, double spanZ)
        {
            Vector2 offsetUV = side.Offset.ToFloat() * textureUVInverse;
            float wallSpanU = (float)length * textureUVInverse.U();
            float spanV = (float)spanZ * textureUVInverse.V();

            float leftU = offsetUV.U();
            float rightU = offsetUV.U() + wallSpanU;
            float topV;
            float bottomV;
            
            if (line.Flags.Unpegged.Lower)
            {
                bottomV = 1.0f + offsetUV.V();
                topV = bottomV - spanV;
            }
            else
            {
                topV = offsetUV.V();
                bottomV = offsetUV.V() + spanV;
            }
            
            return new WallUV(new Vector2(leftU, topV), new Vector2(rightU, bottomV));   
        }

        private static WallUV CalculateTwoSidedLowerWallUV(Line line, Side facingSide, double length, 
            Vector2 textureUVInverse, double topZ, double bottomZ)
        {
            Vector2 offsetUV = facingSide.Offset.ToFloat() * textureUVInverse;
            float wallSpanU = (float)length * textureUVInverse.U();

            float leftU = offsetUV.U();
            float rightU = offsetUV.U() + wallSpanU;
            float topV;
            float bottomV;
            
            if (line.Flags.Unpegged.Lower)
            {
                double ceilZ = facingSide.Sector.Ceiling.Z;
                float topDistFromCeil = (float)(ceilZ - topZ);
                float bottomDistFromCeil = (float)(ceilZ - bottomZ);
                
                topV = offsetUV.V() + (topDistFromCeil * textureUVInverse.V());
                bottomV = offsetUV.V() + (bottomDistFromCeil * textureUVInverse.V());
            }
            else
            {
                float spanZ = (float)(topZ - bottomZ);
                float spanV = spanZ * textureUVInverse.V();

                topV = offsetUV.V();
                bottomV = offsetUV.V() + spanV;
            }
            
            return new WallUV(new Vector2(leftU, topV), new Vector2(rightU, bottomV)); 
        }
        
        private static WallUV CalculateTwoSidedMiddleWallUV(Side side, double length, Vector2 textureUVInverse)
        {
            Vector2 offsetUV = side.Offset.ToFloat() * textureUVInverse;
            float wallSpanU = (float)length * textureUVInverse.U();

            // TODO: This is not right, we will fix it later.
            float leftU = offsetUV.U();
            float rightU = offsetUV.U() + wallSpanU;
            float topV = offsetUV.V();
            float bottomV = 1.0f + offsetUV.V();
            
            return new WallUV(new Vector2(leftU, topV), new Vector2(rightU, bottomV)); 
        }
        
        private static WallUV CalculateTwoSidedUpperWallUV(Line line, Side side, double length, 
            Vector2 textureUVInverse, double spanZ)
        {
            Vector2 offsetUV = side.Offset.ToFloat() * textureUVInverse;
            float wallSpanU = (float)length * textureUVInverse.U();
            float spanV = (float)spanZ * textureUVInverse.V();

            float leftU = offsetUV.U();
            float rightU = offsetUV.U() + wallSpanU;
            float topV;
            float bottomV;
            
            if (line.Flags.Unpegged.Upper)
            {
                topV = offsetUV.V();
                bottomV = topV + spanV;
            }
            else
            {
                bottomV = 1.0f + offsetUV.V();
                topV = bottomV - spanV;
            }
            
            return new WallUV(new Vector2(leftU, topV), new Vector2(rightU, bottomV));   
        }
        
        private static Vector2 CalculateFlatUV(Vec2D vertex, Dimension textureDimension)
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