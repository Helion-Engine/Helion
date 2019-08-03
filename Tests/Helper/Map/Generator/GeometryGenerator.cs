using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Maps.Things;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Test.Helper.Map.Generator
{
    /// <summary>
    /// Allows for quick generation of map geometry when testing.
    /// </summary>
    public class GeometryGenerator
    {
        private readonly List<Line> lines = new List<Line>();
        private readonly List<Thing> things = new List<Thing>();
        private readonly List<SectorFlat> sectorFlats = new List<SectorFlat>();
        private readonly List<Sector> sectors = new List<Sector>();
        private readonly List<Side> sides = new List<Side>();
        private readonly List<Vertex> vertices = new List<Vertex>();

        /// <summary>
        /// Creates a new sector from a floor and ceiling value.
        /// </summary>
        /// <remarks>
        /// The ID generated from this is equal to how many components of this
        /// type we added before.
        /// </remarks>
        /// <param name="floorZ">The floor height.</param>
        /// <param name="ceilingZ">The ceiling height.</param>
        /// <returns>A reference to itself for chaining.</returns>
        public GeometryGenerator AddSector(double floorZ, double ceilingZ)
        {
            SectorFlat floor = new SectorFlat(sectorFlats.Count, "TEMP", floorZ, (byte)sectorFlats.Count, SectorFlatFace.Floor);
            SectorFlat ceiling = new SectorFlat(sectorFlats.Count, "TEMP", floorZ, (byte)sectorFlats.Count, SectorFlatFace.Floor);
            sectorFlats.Add(floor);
            sectorFlats.Add(ceiling);
            
            Sector sector = new Sector(sectors.Count, (byte)sectors.Count, floor, ceiling);
            sectors.Add(sector);
            
            return this;
        }

        /// <summary>
        /// Creates a simple sector when we don't care about any properties of
        /// the sector.
        /// </summary>
        /// <remarks>
        /// The ID generated from this is equal to how many components of this
        /// type we added before.
        /// </remarks>
        /// <returns>A reference to itself for chaining.</returns>
        public GeometryGenerator AddSector() => AddSector(0, 1);

        /// <summary>
        /// Creates a side that references a previously created sector.
        /// </summary>
        /// <remarks>
        /// The ID generated from this is equal to how many components of this
        /// type we added before.
        /// </remarks>
        /// <param name="sectorId">The sector reference.</param>
        /// <returns>A reference to itself for chaining.</returns>
        public GeometryGenerator AddSide(int sectorId)
        {
            Side side = new Side(sides.Count, Vec2I.Zero, "TEMP", "TEMP", "TEMP", sectors[sectorId]);
            sides.Add(side);
            
            return this;
        }
        
        /// <summary>
        /// Creates a line that is one sided.
        /// </summary>
        /// <remarks>
        /// The ID generated from this is equal to how many components of this
        /// type we added before.
        /// </remarks>
        /// <param name="sideId">The ID of the side.</param>
        /// <param name="start">The start position.</param>
        /// <param name="end">The end position.</param>
        /// <returns>A reference to itself for chaining.</returns>
        public GeometryGenerator AddLine(int sideId, Vec2D start, Vec2D end)
        {
            Vertex startVertex = CreateVertex(start);
            Vertex endVertex = CreateVertex(end);
            Line line = new Line(lines.Count, startVertex, endVertex, sides[sideId]);
            lines.Add(line);
            
            return this;
        }
        
        /// <summary>
        /// Creates a line that is one sided.
        /// </summary>
        /// <remarks>
        /// The ID generated from this is equal to how many components of this
        /// type we added before.
        /// </remarks>
        /// <param name="frontSideId">The ID of the front side.</param>
        /// <param name="backSideId">The ID of the back side.</param>
        /// <param name="start">The start position.</param>
        /// <param name="end">The end position.</param>
        /// <returns>A reference to itself for chaining.</returns>
        public GeometryGenerator AddLine(int frontSideId, int backSideId, Vec2D start, Vec2D end)
        {
            Vertex startVertex = CreateVertex(start);
            Vertex endVertex = CreateVertex(end);
            Line line = new Line(lines.Count, startVertex, endVertex, sides[frontSideId], sides[backSideId]);
            lines.Add(line);
            
            return this;
        }

        /// <summary>
        /// Compiles the map from the previous commands. This is the terminal
        /// operation for the object.
        /// </summary>
        /// <returns>The compiled map.</returns>
        public IMap ToMap() => new GeneratedMap(this);
        
        private Vertex CreateVertex(Vec2D position)
        {
            Vertex vertex = new Vertex(vertices.Count, position);
            vertices.Add(vertex);
            return vertex;
        }

        private class GeneratedMap : IMap
        {
            public CIString Name { get; } = "";
            public MapType MapType { get; } = MapType.Doom;
            public IList<Line> Lines { get; }
            public IList<Thing> Things { get; }
            public IList<Sector> Sectors { get; }
            public IList<SectorFlat> SectorFlats { get; }
            public IList<Side> Sides { get; }
            public IList<Vertex> Vertices { get; }

            internal GeneratedMap(GeometryGenerator generator)
            {
                Lines = generator.lines;
                Things = generator.things;
                Sectors = generator.sectors;
                SectorFlats = generator.sectorFlats;
                Sides = generator.sides;
                Vertices = generator.vertices;
            }
        } 
    }
}