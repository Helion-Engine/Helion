using Helion.Maps.Entries;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util;
using System.Collections.Generic;

namespace Helion.Maps
{
    /// <summary>
    /// A map that contains all the geometry, things, and anything else needed
    /// for an editor or a world.
    /// </summary>
    public class Map
    {
        public UpperString Name;
        public List<Line> Lines = new List<Line>();
        public List<Side> Sides = new List<Side>();
        public List<Sector> Sectors = new List<Sector>();
        public List<SectorFlat> SectorFlats = new List<SectorFlat>();
        public List<Vertex> Vertices = new List<Vertex>();

        private Map(UpperString name) => Name = name;

        public static Map? From(MapEntryCollection mapEntryCollection)
        {
            Map map = new Map(mapEntryCollection.Name);

            if (!MapEntryReader.ReadInto(mapEntryCollection, map))
                return null;

            return map;
        }

        /// <summary>
        /// Returns all unique texture names in the map
        /// </summary>
        public HashSet<UpperString> GetUniqueTextureNames()
        {
            HashSet<UpperString> textures = new HashSet<UpperString>();

            foreach(var side in Sides)
            {
                if (side.UpperTexture.NotEmpty())
                    textures.Add(side.UpperTexture);
                if (side.MiddleTexture.NotEmpty())
                    textures.Add(side.MiddleTexture);
                if (side.LowerTexture.NotEmpty())
                    textures.Add(side.LowerTexture);
            }

            return textures;
        }

        /// <summary>
        /// Returns all unique flat names in the map
        /// </summary>
        public HashSet<UpperString> GetUniqueFlatNames()
        {
            HashSet<UpperString> mapFlats = new HashSet<UpperString>();

            foreach (var sector in Sectors)
            {
                if (sector.Floor.Texture.NotEmpty())
                    mapFlats.Add(sector.Floor.Texture);
                if (sector.Ceiling.Texture.NotEmpty())
                    mapFlats.Add(sector.Ceiling.Texture);
            }

            return mapFlats;
        }
    }
}
