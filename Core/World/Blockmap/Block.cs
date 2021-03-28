using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Blockmap
{
    /// <summary>
    /// Represents a cell in the blockmap.
    /// </summary>
    public class Block
    {
        /// <summary>
        /// All the lines for this block.
        /// </summary>
        public readonly List<Line> Lines = new List<Line>();
        
        /// <summary>
        /// All the entities in this block.
        /// </summary>
        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();

        /// <summary>
        /// Gets the block X coordinate, assuming the coordinate was set.
        /// </summary>
        public int X => m_coordinate.X;
        
        /// <summary>
        /// Gets the block Y coordinate, assuming the coordinate was set.
        /// </summary>
        public int Y => m_coordinate.Y;
        
        private Vec2I m_coordinate = Vec2I.Zero;

        /// <summary>
        /// Sets the internal coordinates for this block.
        /// </summary>
        /// <remarks>
        /// We are stuck with this because we can't do this in the constructor
        /// as this is passed in as a generic value to a UniformGrid. The only
        /// other way is to make it have some kind of interface and constrain
        /// it to that, but performance needs to be investigated first before
        /// doing that.
        /// </remarks>
        /// <param name="x">The X coordinate, which should not be negative.
        /// </param>
        /// <param name="y">The Y coordinate, which should not be negative.
        /// </param>
        internal void SetCoordinate(int x, int y)
        {
            Precondition(x >= 0, "Cannot have a negative blockmap X index");
            Precondition(y >= 0, "Cannot have a negative blockmap Y index");
            
            m_coordinate = new Vec2I(x, y);
        }
    }
}