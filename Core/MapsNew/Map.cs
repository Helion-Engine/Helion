using System.Collections.Generic;
using Helion.Maps;
using Helion.MapsNew.Components;
using Helion.MapsNew.Components.GL;
using Helion.Util;
using Helion.Util.Geometry.Vectors;

namespace Helion.MapsNew
{
    /// <summary>
    /// Map information that has been read from a collection of map entries.
    /// </summary>
    /// <remarks>
    /// Intended to be a collection of data that acts as an intermediate from
    /// which we can build a world map from.
    /// </remarks>
    public class Map
    {
        public readonly CIString Name;
        public readonly List<Linedef> Linedefs = new();
        public readonly List<Sidedef> Sidedefs = new();
        public readonly List<Vec2D> Vertices = new();
        public readonly List<Thing> Things = new();
        public readonly List<Sector> Sectors = new();

        /// <summary>
        /// The GL map components. This will be null if the map does not have
        /// any such components, or if it failed to read them
        /// </summary>
        public readonly GLComponents? GL;

        private Map(MapEntryCollection entryCollection)
        {
            Name = entryCollection.Name;
            ReadVertices();
            ReadSectors();
            ReadSidedefs();
            ReadLinedefs();
            ReadThings();
            GL = GLComponents.ReadOrThrow(entryCollection);
        }

        /// <summary>
        /// Reads a map from a map entry collection.
        /// </summary>
        /// <param name="entryCollection">The collection of entries that make
        /// up the map.</param>
        /// <returns>The map, or null if the map is corrupt and cannot be read
        /// properly.</returns>
        public static Map? Read(MapEntryCollection entryCollection)
        {
            try
            {
                return new Map(entryCollection);
            }
            catch
            {
                return null;
            }
        }

        private void ReadVertices()
        {
            // TODO
        }

        private void ReadSectors()
        {
            // TODO
        }

        private void ReadSidedefs()
        {
            // TODO
        }

        private void ReadLinedefs()
        {
            // TODO
        }

        private void ReadThings()
        {
            // TODO
        }
    }
}
