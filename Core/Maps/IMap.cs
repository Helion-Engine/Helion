using System.Collections.Generic;
using Helion.Maps.Components;

namespace Helion.Maps
{
    /// <summary>
    /// The interface for a map with map components. This can be loaded by a
    /// things like a world, map editor, resource editor... etc.
    /// </summary>
    public interface IMap
    {
        /// <summary>
        /// The name of the map.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// The type of map this is.
        /// </summary>
        MapType MapType { get; }
        
        /// <summary>
        /// Gets the lines for this map.
        /// </summary>
        /// <returns>The lines for this map.</returns>
        IReadOnlyList<ILine> GetLines();
        
        /// <summary>
        /// Gets the lines for this map.
        /// </summary>
        /// <returns>The lines for this map.</returns>
        IReadOnlyList<ISector> GetSectors();
        
        /// <summary>
        /// Gets the sides for this map.
        /// </summary>
        /// <returns>The sides for this map.</returns>
        IReadOnlyList<ISide> GetSides();
        
        /// <summary>
        /// Gets the things for this map.
        /// </summary>
        /// <returns>The things for this map.</returns>
        IReadOnlyList<IThing> GetThings();
        
        /// <summary>
        /// Gets the vertices for this map.
        /// </summary>
        /// <returns>The vertices for this map.</returns>
        IReadOnlyList<IVertex> GetVertices();
    }
}