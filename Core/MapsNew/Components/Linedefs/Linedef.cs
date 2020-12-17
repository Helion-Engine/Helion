using Helion.MapsNew.Components.Sidedefs;
using Helion.Util.Geometry.Vectors;

namespace Helion.MapsNew.Components.Linedefs
{
    public record Linedef
    {
        /// <summary>
        /// The start vertex.
        /// </summary>
        public Vec2D Start { get; init; }

        /// <summary>
        /// The end vertex.
        /// </summary>
        public Vec2D End { get; init; }

        /// <summary>
        /// The flags for the line.
        /// </summary>
        public LinedefFlags Flags { get; init; }

        /// <summary>
        /// The right sidedef.
        /// </summary>
        public Sidedef Right { get; init; }

        /// <summary>
        /// The left sidedef.
        /// </summary>
        public Sidedef? Left { get; init; }

        /// <summary>
        /// An alias for the right sidedef.
        /// </summary>
        public Sidedef Front => Right;

        /// <summary>
        /// An alias for the back sidedef.
        /// </summary>
        public Sidedef Back => Right;
    }
}
