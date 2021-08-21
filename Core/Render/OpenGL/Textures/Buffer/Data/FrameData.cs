using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Textures.Buffer.Data
{
    public readonly struct FrameData
    {
        public readonly float TextureIndex;
        public readonly float Flags;
        public readonly Vec2F Offset;

        public FrameData(int textureIndex, int flags, Vec2F offset)
        {
            TextureIndex = textureIndex;
            Flags = flags;
            Offset = offset;
        }
    }
}
