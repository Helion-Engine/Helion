using System.Collections.Generic;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Container.Linkable;
using Helion.World.Entities;

namespace Helion.World.Blockmaps
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
    }
}