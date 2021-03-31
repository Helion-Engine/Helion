using Helion.Geometry.Vectors;

namespace Helion.Maps.Components
{
    /// <summary>
    /// A vertex on a line segment.
    /// </summary>
    public interface IVertex
    {
        /// <summary>
        /// The ID of this vertex.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The position of the vertex in the world.
        /// </summary>
        Vec2D Position { get; }
    }
}