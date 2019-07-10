using System.Numerics;

namespace Helion.Render.Shared.World
{
    /// <summary>
    /// A vertex in a world which holds positional and UV texture information.
    /// </summary>
    public struct Vertex
    {
        public Vector3 Position;
        public Vector2 UV;

        public Vertex(Vector3 position, Vector2 uv)
        {
            Position = position;
            UV = uv;
        }
    }
}