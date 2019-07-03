using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Renderers.World
{
    public struct WorldVertexFlat
    {
        public int TextureHandle;
        public bool IsSky;
        public WorldVertex Root;
        public WorldVertex[] Fan;

        public WorldVertexFlat(int textureHandle, bool isSky, WorldVertex root, WorldVertex[] fan)
        {
            Precondition(fan.Length >= 2, "A fan must at least form a triangle");

            TextureHandle = textureHandle;
            IsSky = isSky;
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
        public bool IsSky;
        public bool FloorHigherThanCeiling;
        public int TextureHandle;
        public WorldVertex TopLeft;
        public WorldVertex TopRight;
        public WorldVertex BottomLeft;
        public WorldVertex BottomRight;

        public WorldVertexWall(bool noTexture, bool isSky, int textureHandle, WorldVertex topLeft, WorldVertex topRight,
            WorldVertex bottomLeft, WorldVertex bottomRight)
        {
            NoTexture = noTexture;
            IsSky = isSky;
            FloorHigherThanCeiling = (bottomLeft.Z > topLeft.Z || bottomRight.Z > topRight.Z);
            TextureHandle = textureHandle;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }
    }

    // TODO: Would it be better to have middle/upper/lower here instead of the
    //       array in case it means this is just a reference? (no locality?)
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
}
