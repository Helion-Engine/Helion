using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared.Triangulator;
using Helion.Resources;
using Helion.Util;
using Helion.World;
using Helion.World.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Helion.Util.Geometry;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Renderers.World
{
    public class WorldRenderableGeometry
    {
        public WorldVertexSegment[] Segments = new WorldVertexSegment[0];
        public WorldVertexSubsector[] Subsectors = new WorldVertexSubsector[0];
        private readonly GLTextureManager textureManager;

        public WorldRenderableGeometry(GLTextureManager glTextureManager)
        {
            textureManager = glTextureManager;
        }

        public bool CheckSubsectorVisibility(ushort index)
        {
            int subsectorIndex = index & BspNodeCompact.SubsectorMask;

            // TODO: Check If the thing is in a disjoint area of the map.
            // TODO: If a segment is back-facing, don't render it.

            return true;
        }

        public bool CheckNodeVisibility(ushort index)
        {
            // TODO: Check if we can see the bounding box from our frustum.

            return true;
        }

        private static WorldVertex VertexToWorldVertex(Vector3 pos, Sector sector, GLTexture texture)
        {
            float u = pos.X * texture.InverseUV.X;
            float v = pos.Y * texture.InverseUV.Y;
            return new WorldVertex(pos.X, pos.Y, pos.Z, u, v, 1.0f, sector.UnitLightLevel);
        }

        private WorldVertexFlat MakeFlat(List<Vector3> vertices, Sector sector, SectorFlat flat)
        {
            GLTexture texture = textureManager.Get(flat.Texture, ResourceNamespace.Flats);
            bool isSky = flat.Texture == Constants.SkyTexture;

            WorldVertex root = VertexToWorldVertex(vertices[0], sector, texture);
            WorldVertex[] fan = vertices.Skip(1).Select(v => VertexToWorldVertex(v, sector, texture)).ToArray();
            return new WorldVertexFlat(texture.Handle, isSky, root, fan);
        }

        private WorldVertexSubsector CreateWorldVertexSubsector(SubsectorTriangles triangles, Subsector subsector)
        {
            Sector sector = subsector.Sector;

            WorldVertexFlat floor = MakeFlat(triangles.FloorVertices, sector, sector.Floor);
            WorldVertexFlat ceiling = MakeFlat(triangles.CeilingVertices, sector, sector.Ceiling);
            return new WorldVertexSubsector(floor, ceiling);
        }

        private void LoadSubsectors(Subsector[] subsectors)
        {
            Subsectors = new WorldVertexSubsector[subsectors.Length];

            Array.ForEach(subsectors, subsector =>
            {
                SubsectorTriangles triangles = WorldTriangulator.Triangulate(subsector);
                WorldVertexSubsector vertices = CreateWorldVertexSubsector(triangles, subsector);
                Subsectors[subsector.Id] = vertices;
            });
        }

        private static Vector2 GetWallDimensions(Vector3 topLeft, Vector3 bottomRight)
        {
            float deltaX = topLeft.X - bottomRight.X;
            float deltaY = topLeft.Y - bottomRight.Y;
            float width = (float)Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
            float height = topLeft.Z - bottomRight.Z;
            return new Vector2(width, height);
        }

        private static WallUV FromLowerUnpegged(Vector3 topLeft, Vector3 bottomRight, GLTexture texture, Segment segment)
        {
            if (segment.Side == null)
            {
                Fail("Should never have a null side when handling lower unpegged");
                return new WallUV();
            }
            
            Vector2 wallDimension = GetWallDimensions(topLeft, bottomRight);
            Vector2 offset = new Vector2(segment.OffsetX + segment.Side.Offset.X, segment.Side.Offset.Y);

            float leftU = offset.X * texture.InverseUV.X;
            float rightU = leftU + (wallDimension.X * texture.InverseUV.X);
            float bottomV = 1.0f + (offset.Y * texture.InverseUV.Y);
            float topV = bottomV - (wallDimension.Y * texture.InverseUV.Y);

            return new WallUV(leftU, rightU, topV, bottomV);
        }
        
        private static WallUV FromUpperUnpegged(Vector3 topLeft, Vector3 bottomRight, GLTexture texture, Segment segment)
        {
            if (segment.Side == null)
            {
                Fail("Should never have a null side when handling upper unpegged");
                return new WallUV();
            }

            Vector2 wallDimension = GetWallDimensions(topLeft, bottomRight);
            Vector2 offset = new Vector2(segment.OffsetX + segment.Side.Offset.X, segment.Side.Offset.Y);

            float leftU = offset.X * texture.InverseUV.X;
            float rightU = leftU + (wallDimension.X * texture.InverseUV.X);
            float topV = offset.Y * texture.InverseUV.Y;
            float bottomV = topV + (wallDimension.Y * texture.InverseUV.Y);

            return new WallUV(leftU, rightU, topV, bottomV);
        } 
        
        private WorldVertexWall MakeOneSidedMiddleWall(SegmentWalls walls)
        {
            Line line = walls.Line;
            Side side = walls.Side;
            float lightLevel = side.Sector.UnitLightLevel;
            WallQuad wallQuad = walls.Middle;
            
            Vector3 topLeft = wallQuad.UpperTriangle.First;
            Vector3 bottomRight = wallQuad.LowerTriangle.Third;

            UpperString textureName = side.MiddleTexture;
            GLTexture texture = textureManager.Get(textureName, ResourceNamespace.Textures);
            bool hasNoTexture = (textureName == Constants.NoTexture);

            // Middle is default pegged to the top.
            WallUV uv = line.Flags.Unpegged.Lower ? 
                FromLowerUnpegged(topLeft, bottomRight, texture, walls.Segment) : 
                FromUpperUnpegged(topLeft, bottomRight, texture, walls.Segment);

            Vector2 left = new Vector2(topLeft.X, topLeft.Y);
            Vector2 right = new Vector2(bottomRight.X, bottomRight.Y);
            float top = topLeft.Z;
            float bottom = bottomRight.Z;
            
            WorldVertex topLeftVertex = new WorldVertex(left.X, left.Y, top, uv.LeftU, uv.TopV, 1.0f, lightLevel);
            WorldVertex topRightVertex = new WorldVertex(right.X, right.Y, top, uv.RightU, uv.TopV, 1.0f, lightLevel);
            WorldVertex bottomLeftVertex = new WorldVertex(left.X, left.Y, bottom, uv.LeftU, uv.BottomV, 1.0f, lightLevel);
            WorldVertex bottomRightVertex = new WorldVertex(right.X, right.Y, bottom, uv.RightU, uv.BottomV, 1.0f, lightLevel);
            return new WorldVertexWall(hasNoTexture, false, texture.Handle, topLeftVertex, topRightVertex, bottomLeftVertex, bottomRightVertex);
        }

        // TODO: Refactor the extremely similar function
        private WorldVertexWall MakeTwoSidedLowerWall(SegmentWalls walls)
        {
            if (walls.Lower == null)
            {
                Fail("Should never have encountered a lower wall without a lower component");
                return new WorldVertexWall();
            }
            
            Line line = walls.Line;
            Side side = walls.Side;
            float lightLevel = side.Sector.UnitLightLevel;
            WallQuad wallQuad = walls.Lower;

            Invariant(side.PartnerSide != null, "Passed a one-sided line for a two-sided lower side");

            bool isSky = false;
            if (side.PartnerSide != null)
                isSky = (side.PartnerSide.Sector.Floor.Texture == Constants.SkyTexture);

            Vector3 topLeft = wallQuad.UpperTriangle.First;
            Vector3 bottomRight = wallQuad.LowerTriangle.Third;

            UpperString textureName = side.LowerTexture;
            GLTexture texture = textureManager.Get(textureName, ResourceNamespace.Textures);
            bool hasNoTexture = (textureName == Constants.NoTexture);

            // Lower is default pegged to the top.
            WallUV uv = line.Flags.Unpegged.Lower ? 
                FromLowerUnpegged(topLeft, bottomRight, texture, walls.Segment) : 
                FromUpperUnpegged(topLeft, bottomRight, texture, walls.Segment);

            Vector2 left = new Vector2(topLeft.X, topLeft.Y);
            Vector2 right = new Vector2(bottomRight.X, bottomRight.Y);
            float top = topLeft.Z;
            float bottom = bottomRight.Z;
            
            WorldVertex topLeftVertex = new WorldVertex(left.X, left.Y, top, uv.LeftU, uv.TopV, 1.0f, lightLevel);
            WorldVertex topRightVertex = new WorldVertex(right.X, right.Y, top, uv.RightU, uv.TopV, 1.0f, lightLevel);
            WorldVertex bottomLeftVertex = new WorldVertex(left.X, left.Y, bottom, uv.LeftU, uv.BottomV, 1.0f, lightLevel);
            WorldVertex bottomRightVertex = new WorldVertex(right.X, right.Y, bottom, uv.RightU, uv.BottomV, 1.0f, lightLevel);
            return new WorldVertexWall(hasNoTexture, isSky, texture.Handle, topLeftVertex, topRightVertex, bottomLeftVertex, bottomRightVertex);
        }
        
        private WorldVertexWall MakeEmptyTwoSidedMiddleWall(SegmentWalls walls)
        {
            Vector3 topLeft = walls.Middle.UpperTriangle.First;
            Vector3 topRight = walls.Middle.UpperTriangle.Third;
            Vector3 bottomLeft = walls.Middle.LowerTriangle.Second;
            Vector3 bottomRight = walls.Middle.UpperTriangle.Third;
            
            WorldVertex topLeftVertex = new WorldVertex(topLeft.X, topLeft.Y, topLeft.Z, 0, 0, 0, 0);
            WorldVertex topRightVertex = new WorldVertex(topRight.X, topRight.Y, topRight.Z, 0, 0, 0, 0);
            WorldVertex bottomLeftVertex = new WorldVertex(bottomLeft.X, bottomLeft.Y, bottomLeft.Z, 0, 0, 0, 0);
            WorldVertex bottomRightVertex = new WorldVertex(bottomRight.X, bottomRight.Y, bottomRight.Z, 0, 0, 0, 0);
            return new WorldVertexWall(true, false, 0, topLeftVertex, topRightVertex, bottomLeftVertex, bottomRightVertex);
        }
        
        private WorldVertexWall MakeTwoSidedMiddleWall(SegmentWalls walls)
        {
            if (walls.Side.PartnerSide == null)
            {
                Fail("Should never have encountered a two sided middle wall without a back side");
                return new WorldVertexWall();
            }
            
            Line line = walls.Line;
            Side facingSide = walls.Side;
            Side otherSide = walls.Side.PartnerSide;
            Sector facingSector = facingSide.Sector;
            Sector otherSector = otherSide.Sector;
            float lightLevel = facingSector.UnitLightLevel;

            UpperString textureName = facingSide.MiddleTexture;
            if (textureName == Constants.NoTexture)
                return MakeEmptyTwoSidedMiddleWall(walls);
            
            // The triangulation for middle two sided lines are usually wrong
            // because the location of the vertices change based on the area to
            // be rendered in. We have to calculate it ourselves.
            GLTexture texture = textureManager.Get(textureName, ResourceNamespace.Textures);
            float highestFloorZ = Math.Max(facingSector.Floor.Z, otherSector.Floor.Z);
            float lowestCeilZ = Math.Min(facingSector.Ceiling.Z, otherSector.Ceiling.Z);
            
            // Rendering depends on how much space there is. We have to clamp
            // the texture to either fit inside an area that is smaller than
            // it, or if there's enough space then we align it to the floor
            // or ceiling and apply the offset.
            float wallSpanZ = Math.Min(lowestCeilZ - highestFloorZ, texture.Dimension.Height);
            
            // TODO: We need to handle the case where offsets are applied to a
            //       texture but it already is clamped between a narrower
            //       opening than what the texture allows. This is going to be
            //       very annoying because the amount of corner cases...
            //       The logic below isn't right anyways as soon as we start
            //       taking offsets into consideration, so we'll come back to
            //       this later.
            float offsetY = facingSide.Offset.Y;
            float top = (line.Flags.Unpegged.Upper ? lowestCeilZ : highestFloorZ + wallSpanZ) + offsetY;
            float bottom = (line.Flags.Unpegged.Upper ? top - wallSpanZ : highestFloorZ) + offsetY;
            
            Vector2 left = walls.Middle.UpperTriangle.First.To2D();
            Vector2 right = walls.Middle.LowerTriangle.Third.To2D();
            Vector3 topLeft = new Vector3(left.X, left.Y, top);
            Vector3 bottomRight = new Vector3(right.X, right.Y, bottom);
            
            // Two sided middle is default pegged to the top.
            WallUV uv = line.Flags.Unpegged.Lower ? 
                FromLowerUnpegged(topLeft, bottomRight, texture, walls.Segment) : 
                FromUpperUnpegged(topLeft, bottomRight, texture, walls.Segment);
            
            WorldVertex topLeftVertex = new WorldVertex(left.X, left.Y, top, uv.LeftU, uv.TopV, 1.0f, lightLevel);
            WorldVertex topRightVertex = new WorldVertex(right.X, right.Y, top, uv.RightU, uv.TopV, 1.0f, lightLevel);
            WorldVertex bottomLeftVertex = new WorldVertex(left.X, left.Y, bottom, uv.LeftU, uv.BottomV, 1.0f, lightLevel);
            WorldVertex bottomRightVertex = new WorldVertex(right.X, right.Y, bottom, uv.RightU, uv.BottomV, 1.0f, lightLevel);
            return new WorldVertexWall(false, false, texture.Handle, topLeftVertex, topRightVertex, bottomLeftVertex, bottomRightVertex);
        }

        // TODO: Refactor the extremely similar function
        private WorldVertexWall MakeTwoSidedUpperWall(SegmentWalls walls)
        {
            if (walls.Upper == null)
            {
                Fail("Should never have encountered an upper wall without a upper component");
                return new WorldVertexWall();
            }
            
            Line line = walls.Line;
            Side side = walls.Side;
            float lightLevel = side.Sector.UnitLightLevel;
            WallQuad wallQuad = walls.Upper;
            
            Invariant(side.PartnerSide != null, "Passed a one-sided line for a two-sided upper side");

            bool isSky = false;
            if (side.PartnerSide != null)
                isSky = (side.PartnerSide.Sector.Ceiling.Texture == Constants.SkyTexture);

            Vector3 topLeft = wallQuad.UpperTriangle.First;
            Vector3 bottomRight = wallQuad.LowerTriangle.Third;

            UpperString textureName = side.UpperTexture;
            GLTexture texture = textureManager.Get(textureName, ResourceNamespace.Textures);
            bool hasNoTexture = (textureName == Constants.NoTexture);

            // Upper is default pegged to the bottom.
            WallUV uv = line.Flags.Unpegged.Upper ? 
                FromUpperUnpegged(topLeft, bottomRight, texture, walls.Segment) :
                FromLowerUnpegged(topLeft, bottomRight, texture, walls.Segment);

            Vector2 left = new Vector2(topLeft.X, topLeft.Y);
            Vector2 right = new Vector2(bottomRight.X, bottomRight.Y);
            float top = topLeft.Z;
            float bottom = bottomRight.Z;
            
            WorldVertex topLeftVertex = new WorldVertex(left.X, left.Y, top, uv.LeftU, uv.TopV, 1.0f, lightLevel);
            WorldVertex topRightVertex = new WorldVertex(right.X, right.Y, top, uv.RightU, uv.TopV, 1.0f, lightLevel);
            WorldVertex bottomLeftVertex = new WorldVertex(left.X, left.Y, bottom, uv.LeftU, uv.BottomV, 1.0f, lightLevel);
            WorldVertex bottomRightVertex = new WorldVertex(right.X, right.Y, bottom, uv.RightU, uv.BottomV, 1.0f, lightLevel);
            return new WorldVertexWall(hasNoTexture, isSky, texture.Handle, topLeftVertex, topRightVertex, bottomLeftVertex, bottomRightVertex);
        }

        private WorldVertexSegment CreateWorldVertexSegment(SegmentWalls? segmentWalls, Segment segment)
        {
            WorldVertexWall[] walls = new WorldVertexWall[1];

            // Minisegs can never be made into walls, so they're the only case
            // that is null. We can exit early in this case.
            if (segmentWalls == null)
                return new WorldVertexSegment(walls);
            
            Precondition(!segmentWalls.Segment.IsMiniseg, "Shouldn't be trying to make geometry for a miniseg");
            
            if (segmentWalls.Lower != null && segmentWalls.Upper != null)
            {
                walls = new WorldVertexWall[3];
                walls[0] = MakeTwoSidedLowerWall(segmentWalls);
                walls[1] = MakeTwoSidedMiddleWall(segmentWalls);
                walls[2] = MakeTwoSidedUpperWall(segmentWalls);
            }
            else
                walls[0] = MakeOneSidedMiddleWall(segmentWalls);

            return new WorldVertexSegment(walls);
        }

        private void LoadSegments(List<Segment> segments)
        {
            Segments = new WorldVertexSegment[segments.Count];

            segments.ForEach(segment =>
            {
                SegmentWalls? walls = WorldTriangulator.Triangulate(segment);

                WorldVertexSegment vertices = CreateWorldVertexSegment(walls, segment);
                Segments[segment.Id] = vertices;
            });
        }

        public void Load(WorldBase world)
        {
            LoadSegments(world.BspTree.Segments);
            LoadSubsectors(world.BspTree.Subsectors);
        }
    }
}
