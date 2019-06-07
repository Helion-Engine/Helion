using Helion.Maps.Geometry;
using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared.Triangulator;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Renderers.World
{
    public struct WorldVertexFlat
    {
        public int TextureHandle;
        public WorldVertex Root;
        public WorldVertex[] Fan;

        public WorldVertexFlat(int textureHandle, WorldVertex root, WorldVertex[] fan)
        {
            Precondition(fan.Length >= 2, "A fan must at least form a triangle");

            TextureHandle = textureHandle;
            Root = root;
            Fan = fan;
        }
    }

    public struct WorldVertexSubsector
    {
        public WorldVertexFlat Floor;
        public WorldVertexFlat Ceiling;

        public WorldVertexSubsector(WorldVertexFlat floor, WorldVertexFlat ceiling)
        {
            Floor = floor;
            Ceiling = ceiling;
        }
    }

    public struct WorldVertexWall
    {
        public bool NoTexture;
        public int TextureHandle;
        public WorldVertex TopLeft;
        public WorldVertex TopRight;
        public WorldVertex BottomLeft;
        public WorldVertex BottomRight;

        public WorldVertexWall(bool noTexture, int textureHandle, WorldVertex topLeft, WorldVertex topRight, 
            WorldVertex bottomLeft, WorldVertex bottomRight)
        {
            NoTexture = noTexture;
            TextureHandle = textureHandle;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }
    }

    // TODO: Would it be better to have middle/upper/lower here instead of the
    // array in case it means this is just a reference? (no locality?)
    public struct WorldVertexSegment
    {
        public WorldVertexWall[] Walls;

        public ref WorldVertexWall Middle => ref Walls[0];
        public ref WorldVertexWall Upper => ref Walls[1];
        public ref WorldVertexWall Lower => ref Walls[2];

        public WorldVertexSegment(WorldVertexWall[] walls)
        {
            Precondition(walls.Length == 0 || walls.Length == 1 || walls.Length == 3, "$Unexpected number of walls: {walls.Length}");
            Walls = walls;
        }
    }

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

        private WorldVertex VertexToWorldVertex(Vector3 pos, Sector sector, GLTexture texture)
        {
            // TODO: Support offsets when the time comes.
            float u = pos.X / texture.Dimension.Width;
            float v = pos.Y / texture.Dimension.Height;
            return new WorldVertex(pos.X, pos.Y, pos.Z, u, v, 1.0f, sector.UnitLightLevel);
        }

        private WorldVertexFlat MakeFlat(List<Vector3> vertices, Sector sector, SectorFlat flat)
        {
            GLTexture texture = textureManager.Get(flat.Texture);

            WorldVertex root = VertexToWorldVertex(vertices[0], sector, texture);
            WorldVertex[] fan = vertices.Skip(1).Select(v => VertexToWorldVertex(v, sector, texture)).ToArray();
            return new WorldVertexFlat(texture.Handle, root, fan);
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

        private WorldVertexWall MakeWall(WallTriangles triangles, string textureName, 
            Vector2 offset, float lightLevel)
        {
            Vector3 topLeft = triangles.UpperTriangle.First;
            Vector3 bottomRight = triangles.LowerTriangle.Third;
            Vector2 left = new Vector2(topLeft.X, topLeft.Y);
            Vector2 right = new Vector2(bottomRight.X, bottomRight.Y);
            float top = topLeft.Z;
            float bottom = bottomRight.Z;

            // TODO: Support alpha.
            float alpha = 1.0f;

            // TODO: Support the 'texture' namespace.
            GLTexture texture = textureManager.Get(textureName);
            bool noTexture = (textureName == Constants.NoTexture);
            Vector2 widthHeight = GetWallDimensions(topLeft, bottomRight);

            // TODO: Handle upper/lower unpegged.

            (float leftU, float topV) = offset * texture.InverseUV;
            Vector2 deltaUV = widthHeight * texture.InverseUV;
            (float rightU, float bottomV) = (leftU + deltaUV.X, topV + deltaUV.Y);

            WorldVertex topLeftVertex = new WorldVertex(left.X, left.Y, top, leftU, topV, alpha, lightLevel);
            WorldVertex topRightVertex = new WorldVertex(right.X, right.Y, top, rightU, topV, alpha, lightLevel);
            WorldVertex bottomLeftVertex = new WorldVertex(left.X, left.Y, bottom, leftU, bottomV, alpha, lightLevel);
            WorldVertex bottomRightVertex = new WorldVertex(right.X, right.Y, bottom, rightU, bottomV, alpha, lightLevel);
            return new WorldVertexWall(noTexture, texture.Handle, topLeftVertex, topRightVertex, bottomLeftVertex, bottomRightVertex);
        }

        private WorldVertexSegment CreateWorldVertexSegment(SegmentTriangles? triangles, Segment segment)
        {
            WorldVertexWall[] walls = new WorldVertexWall[0];

            if (triangles == null)
                return new WorldVertexSegment(walls);

            if (segment.Side == null)
                throw new HelionException("A non-miniseg should always have a line");

            Side side = segment.Side;
            float lightLevel = side.Sector.UnitLightLevel;
            Vector2 offset = new Vector2(side.Offset.X + segment.OffsetX, side.Offset.Y);

            if (triangles.Lower != null && triangles.Upper != null)
            {
                walls = new WorldVertexWall[3];
                walls[0] = MakeWall(triangles.Middle, side.MiddleTexture, offset, lightLevel);
                walls[1] = MakeWall(triangles.Upper, side.UpperTexture, offset, lightLevel);
                walls[2] = MakeWall(triangles.Lower, side.LowerTexture, offset, lightLevel);
            }
            else
            {
                walls = new WorldVertexWall[1];
                walls[0] = MakeWall(triangles.Middle, side.MiddleTexture, offset, lightLevel);
            }

            return new WorldVertexSegment(walls);
        }

        private void LoadSegments(List<Segment> segments)
        {
            Segments = new WorldVertexSegment[segments.Count];

            segments.ForEach(segment =>
            {
                SegmentTriangles? triangles = WorldTriangulator.Triangulate(segment);
                WorldVertexSegment vertices = CreateWorldVertexSegment(triangles, segment);
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
