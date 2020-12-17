using System;
using System.Collections.Generic;
using Helion.Maps.Components.GL;
using Helion.Maps.Components.Linedefs;
using Helion.Maps.Components.Sectors;
using Helion.Maps.Components.Sidedefs;
using Helion.Maps.Components.Things;
using Helion.Maps.Specials.Vanilla;
using Helion.Util;
using Helion.Util.Bytes;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Maps
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
        private const ushort NoSidedef = (ushort)0xFFFFU;
        private const int BytesPerDoomLine = 14;
        private const int BytesPerSector = 26;
        private const int BytesPerSide = 30;
        private const int BytesPerDoomThing = 10;
        private const int BytesPerVertex = 4;

        public readonly CIString Name;
        public readonly MapType MapType;
        public readonly List<Linedef> Linedefs = new();
        public readonly List<Sidedef> Sidedefs = new();
        public readonly List<Vec2D> Vertices = new();
        public readonly List<Thing> Things = new();
        public readonly List<Sector> Sectors = new();

        /// <summary>
        /// The GL map components. This will be null if the map does not have
        /// any such components, and thus should use an internal node builder
        /// if it wants such components.
        /// </summary>
        public readonly GLComponents? GL;

        private Map(MapEntryCollection entryCollection)
        {
            Name = entryCollection.Name;
            MapType = entryCollection.MapType;
            ReadVertices(entryCollection.Vertices!);
            ReadSectors(entryCollection.Sectors!);
            ReadSidedefs(entryCollection.Sidedefs!);
            ReadLinedefs(entryCollection.Linedefs!);
            ReadThings(entryCollection.Things!);
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

        private void ReadVertices(byte[] vertexData)
        {
            if (vertexData.Length % BytesPerVertex != 0)
                throw new Exception("Vertex data length incorrect");

            int numVertices = vertexData.Length / BytesPerVertex;
            ByteReader reader = new(vertexData);

            for (int id = 0; id < numVertices; id++)
            {
                Fixed x = new Fixed(reader.Short(), 0);
                Fixed y = new Fixed(reader.Short(), 0);
                Vec2D vertex = new(x.ToDouble(), y.ToDouble());
                Vertices.Add(vertex);
            }
        }

        private void ReadSectors(byte[] sectorData)
        {
            if (sectorData.Length % BytesPerSector != 0)
                throw new Exception("Sector data length incorrect");

            int numSectors = sectorData.Length / BytesPerSector;
            ByteReader reader = new(sectorData);

            for (int index = 0; index < numSectors; index++)
            {
                DoomSector sector = new()
                {
                    Index = index,
                    FloorZ = reader.Short(),
                    CeilingZ = reader.Short(),
                    FloorTexture = reader.EightByteString().ToUpper(),
                    CeilingTexture = reader.EightByteString().ToUpper(),
                    LightLevel = reader.Short(),
                    SectorType = (DoomSectorType)reader.UShort(),
                    Tag = reader.UShort()
                };
                Sectors.Add(sector);
            }
        }

        private void ReadSidedefs(byte[] sideData)
        {
            if (sideData.Length % BytesPerSide != 0)
                throw new Exception("Sidedef data length incorrect");

            int numSides = sideData.Length / BytesPerSide;
            ByteReader reader = new(sideData);

            for (int id = 0; id < numSides; id++)
            {
                Sidedef sidedef = new()
                {
                    Offset = new Vec2I(reader.Short(), reader.Short()),
                    UpperTexture = reader.EightByteString().ToUpper(),
                    LowerTexture = reader.EightByteString().ToUpper(),
                    MiddleTexture = reader.EightByteString().ToUpper(),
                    Sector = Sectors[reader.UShort()]
                };
                Sidedefs.Add(sidedef);
            }

            // TODO: Apply compatibility updates if necessary.
        }

        private void ReadLinedefs(byte[] lineData)
        {
            if (MapType == MapType.Doom)
                ReadDoomLinedefs(lineData);
            else
                ReadHexenLinedefs(lineData);

            // TODO: Apply compatibility updates if necessary.
        }

        private void ReadDoomLinedefs(byte[] lineData)
        {
            if (lineData.Length % BytesPerDoomLine != 0)
                throw new Exception("Linedef data length incorrect");

            int numLines = lineData.Length / BytesPerDoomLine;
            ByteReader reader = new(lineData);

            for (int index = 0; index < numLines; index++)
            {
                DoomLinedef linedef = new()
                {
                    Index = index,
                    Start = Vertices[reader.UShort()],
                    End = Vertices[reader.UShort()],
                    Flags = LinedefFlags.Doom(reader.UShort()),
                    LineType = (VanillaLineSpecialType)reader.UShort(),
                    SectorTag = reader.UShort(),
                    Right = Sidedefs[reader.UShort()],
                    Left = LookupDoomLeftSidedef(reader.UShort())
                };
                Linedefs.Add(linedef);
            }

            Sidedef? LookupDoomLeftSidedef(ushort index) => index == NoSidedef ? null : Sidedefs[index];
        }

        private void ReadHexenLinedefs(byte[] lineData)
        {
            throw new NotImplementedException("Hexen things to be implemented");
        }

        private void ReadThings(byte[] thingData)
        {
            if (MapType == MapType.Doom)
                ReadDoomThings(thingData);
            else
                ReadHexenThings(thingData);

            // TODO: Apply compatibility updates if necessary.
        }

        private void ReadDoomThings(byte[] thingData)
        {
            if (thingData.Length % BytesPerDoomThing != 0)
                throw new Exception("Thing data length incorrect");

            int numThings = thingData.Length / BytesPerDoomThing;
            ByteReader reader = new(thingData);

            for (int id = 0; id < numThings; id++)
            {
                Thing thing = new()
                {
                    Position = new Vec3Fixed(new Fixed(reader.Short(), 0), new Fixed(reader.Short(), 0), Fixed.Lowest()),
                    Angle = reader.UShort(),
                    EditorNumber = reader.UShort(),
                    Flags = ThingFlags.Doom(reader.UShort()),
                };
                Things.Add(thing);
            }
        }

        private void ReadHexenThings(byte[] thingData)
        {
            throw new NotImplementedException("Hexen things to be implemented");
        }
    }
}
