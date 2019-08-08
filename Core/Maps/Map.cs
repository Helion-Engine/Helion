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

            foreach (var side in sector.Sides)
            {
                if (side.Line == null)
                    continue;

                if (side.Sector != sector && side.Sector.Floor.Z < lowestZ)
                {
                    lowestSector = side.Sector;
                    lowestZ = lowestSector.Floor.Z;
                }

                if (side.PartnerSide != null && side.PartnerSide.Sector != sector && side.PartnerSide.Sector.Floor.Z < lowestZ)
                {
                    lowestSector = side.PartnerSide.Sector;
                    lowestZ = lowestSector.Floor.Z;
                }
            }

            return lowestSector;
        }


        public Sector? GetHighestAdjacentFloor(Sector sector)
        {
            double highestZ = double.MinValue;
            Sector? highestSector = null;

            foreach (var side in sector.Sides)
            {
                if (side.Line == null)
                    continue;

                if (side.Sector != sector && side.Sector.Floor.Z > highestZ)
                {
                    highestSector = side.Sector;
                    highestZ = highestSector.Floor.Z;
                }

                if (side.PartnerSide != null && side.PartnerSide.Sector != sector && side.PartnerSide.Sector.Floor.Z > highestZ)
                {
                    highestSector = side.PartnerSide.Sector;
                    highestZ = highestSector.Floor.Z;
                }
            }

            return highestSector;
        }

        public Sector? GetLowestAdjacentCeiling(Sector sector)
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            foreach (var side in sector.Sides)
            {
                if (side.Line == null)
                    continue;

                if (side.Sector != sector && side.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = side.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }

                if (side.PartnerSide != null && side.PartnerSide.Sector != sector && side.PartnerSide.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = side.PartnerSide.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }
            }

            return lowestSector;
        }

        public Sector? GetHighestAdjacentCeiling(Sector sector)
        {
            double highestZ = double.MinValue;
            Sector? highestSector = null;

            foreach (var side in sector.Sides)
            {
                if (side.Line == null)
                    continue;

                if (side.Sector != sector && side.Sector.Ceiling.Z > highestZ)
                {
                    highestSector = side.Sector;
                    highestZ = highestSector.Ceiling.Z;
                }

                if (side.PartnerSide != null && side.PartnerSide.Sector != sector && side.PartnerSide.Sector.Ceiling.Z > highestZ)
                {
                    highestSector = side.PartnerSide.Sector;
                    highestZ = highestSector.Ceiling.Z;
                }
            }

            return highestSector;
        }
    }
}