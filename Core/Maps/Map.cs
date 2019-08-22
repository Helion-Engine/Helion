using System.Collections.Generic;
using Helion.Maps.Entries;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Maps.Things;
using Helion.Util;

namespace Helion.Maps
{
    /// <summary>
    /// A map that contains all the geometry, things, and anything else needed
    /// for an editor or a world.
    /// </summary>
    public class Map : IMap
    {
        /// <inheritdoc/>
        public CIString Name { get; }
        
        /// <inheritdoc/>
        public MapType MapType { get; }
        
        /// <inheritdoc/>
        public IList<Line> Lines { get; } = new List<Line>();
        
        /// <inheritdoc/>
        public IList<Thing> Things { get; } = new List<Thing>();
        
        /// <inheritdoc/>
        public IList<Side> Sides { get; } = new List<Side>();
        
        /// <inheritdoc/>
        public IList<Sector> Sectors { get; } = new List<Sector>();
        
        /// <inheritdoc/>
        public IList<SectorFlat> SectorFlats { get; } = new List<SectorFlat>();
        
        /// <inheritdoc/>
        public IList<Vertex> Vertices { get; } = new List<Vertex>();

        private Map(CIString name, MapType type)
        {
            Name = name;
            MapType = type;
        }

        /// <summary>
        /// Creates a map from a collection of map entries.
        /// </summary>
        /// <param name="mapEntryCollection">The map entry collection.</param>
        /// <returns>The map on reading, or null if there was any errors when
        /// processing the map.</returns>
        public static Map? From(MapEntryCollection mapEntryCollection)
        {
            Map map = new Map(mapEntryCollection.Name, mapEntryCollection.MapType);
            return MapEntryReader.ReadInto(mapEntryCollection, map) ? map : null;
        }

        // TODO use plane values - can probably make these neater and more concise, they are all basically the same function
        // TODO add function to handle WR_RaiseByShortestLowerTexture
        public Sector? GetLowestAdjacentFloor(Sector sector)
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Floor.Z < lowestZ)
                {
                    lowestSector = line.Front.Sector;
                    lowestZ = lowestSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != sector && line.Back.Sector.Floor.Z < lowestZ)
                {
                    lowestSector = line.Back.Sector;
                    lowestZ = lowestSector.Floor.Z;
                }
            }

            return lowestSector;
        }

        public Sector? GetHighestAdjacentFloor(Sector sector)
        {
            double highestZ = double.MinValue;
            Sector? highestSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Floor.Z > highestZ)
                {
                    highestSector = line.Front.Sector;
                    highestZ = highestSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != sector && line.Back.Sector.Floor.Z > highestZ)
                {
                    highestSector = line.Back.Sector;
                    highestZ = highestSector.Floor.Z;
                }
            }

            return highestSector;
        }

        public Sector? GetLowestAdjacentCeiling(Sector sector)
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = line.Front.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != sector && line.Back.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = line.Back.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }
            }

            return lowestSector;
        }

        public Sector? GetHighestAdjacentCeiling(Sector sector)
        {
            double highestZ = double.MinValue;
            Sector? highestSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Ceiling.Z > highestZ)
                {
                    highestSector = line.Front.Sector;
                    highestZ = highestSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != sector && line.Back.Sector.Ceiling.Z > highestZ)
                {
                    highestSector = line.Back.Sector;
                    highestZ = highestSector.Ceiling.Z;
                }
            }

            return highestSector;
        }

        public Sector? GetNextLowestFloor(Sector sector)
        {
            double currentZ = double.MinValue;
            Sector? currentSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Floor.Z < sector.Floor.Z && line.Front.Sector.Floor.Z > currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != sector &&
                    line.Back.Sector.Floor.Z < sector.Floor.Z && line.Back.Sector.Floor.Z > currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Floor.Z;
                }
            }

            return currentSector;
        }

        public Sector? GetNextLowestCeiling(Sector sector)
        {
            double currentZ = double.MinValue;
            Sector? currentSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Ceiling.Z < sector.Ceiling.Z && line.Front.Sector.Ceiling.Z > currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != sector &&
                    line.Back.Sector.Ceiling.Z < sector.Ceiling.Z && line.Back.Sector.Ceiling.Z > currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }
            }

            return currentSector;
        }

        public Sector? GetNextHighestFloor(Sector sector)
        {
            double currentZ = double.MaxValue;
            Sector? currentSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Floor.Z > sector.Floor.Z && line.Front.Sector.Floor.Z < currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != sector &&
                    line.Back.Sector.Floor.Z > sector.Floor.Z && line.Back.Sector.Floor.Z < currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Floor.Z;
                }
            }

            return currentSector;
        }

        public Sector? GetNextHighestCeiling(Sector sector)
        {
            double currentZ = double.MaxValue;
            Sector? currentSector = null;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.Ceiling.Z > sector.Ceiling.Z && line.Front.Sector.Ceiling.Z < currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != sector &&
                    line.Back.Sector.Ceiling.Z > sector.Ceiling.Z && line.Back.Sector.Ceiling.Z < currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }
            }

            return currentSector;
        }

        public byte GetMinLightLevelNeighbor(Sector sector)
        {
            byte min = sector.LightLevel;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.LightLevel < min)
                    min = line.Front.Sector.LightLevel;
                if (line.Back != null && line.Back.Sector != sector && line.Back.Sector.LightLevel < min)
                    min = line.Back.Sector.LightLevel;
            }

            return min;
        }

        public byte GetMaxLightLevelNeighbor(Sector sector)
        {
            byte max = sector.LightLevel;

            foreach (var line in sector.Lines)
            {
                if (line.Front.Sector != sector && line.Front.Sector.LightLevel > max)
                    max = line.Front.Sector.LightLevel;
                if (line.Back != null && line.Back.Sector != sector && line.Back.Sector.LightLevel > max)
                    max = line.Back.Sector.LightLevel;
            }

            return max;
        }
    }
}