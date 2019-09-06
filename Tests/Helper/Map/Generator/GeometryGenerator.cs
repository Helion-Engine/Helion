using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Util.Geometry;
using Moq;

namespace Helion.Test.Helper.Map.Generator
{
    /// <summary>
    /// Allows for quick generation of map geometry when testing.
    /// </summary>
    public class GeometryGenerator
    {
        private readonly List<ILine> lines = new List<ILine>();
        private readonly List<IThing> things = new List<IThing>();
        private readonly List<ISector> sectors = new List<ISector>();
        private readonly List<ISide> sides = new List<ISide>();
        private readonly List<IVertex> vertices = new List<IVertex>();

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
            var mock = new Mock<ISector>();
            mock.Setup(sec => sec.Id).Returns(sectors.Count);
            mock.Setup(sec => sec.Tag).Returns(0);
            mock.Setup(sec => sec.FloorZ).Returns((short)floorZ);
            mock.Setup(sec => sec.CeilingZ).Returns((short)floorZ);
            mock.Setup(sec => sec.LightLevel).Returns(0);
            mock.Setup(sec => sec.FloorTexture).Returns("TEMP");
            mock.Setup(sec => sec.CeilingTexture).Returns("TEMP");
            
            sectors.Add(mock.Object);
            
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
            var mock = new Mock<ISide>();
            mock.Setup(side => side.Id).Returns(sides.Count);
            mock.Setup(side => side.Offset).Returns(Vec2I.Zero);
            mock.Setup(side => side.UpperTexture).Returns("TEMP");
            mock.Setup(side => side.MiddleTexture).Returns("TEMP");
            mock.Setup(side => side.LowerTexture).Returns("TEMP");
            mock.Setup(side => side.GetSector()).Returns(sectors[sectorId]);
            
            sides.Add(mock.Object);
            
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
            IVertex startVertex = CreateVertex(start);
            IVertex endVertex = CreateVertex(end);
            
            var mock = new Mock<ILine>();
            mock.Setup(line => line.Id).Returns(lines.Count);
            mock.Setup(line => line.GetStart()).Returns(startVertex);
            mock.Setup(line => line.GetStart()).Returns(endVertex);
            mock.Setup(line => line.GetFront()).Returns(sides[sideId]);

            lines.Add(mock.Object);
            
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
            IVertex startVertex = CreateVertex(start);
            IVertex endVertex = CreateVertex(end);
            
            var mock = new Mock<ILine>();
            mock.Setup(line => line.Id).Returns(lines.Count);
            mock.Setup(line => line.GetStart()).Returns(startVertex);
            mock.Setup(line => line.GetStart()).Returns(endVertex);
            mock.Setup(line => line.GetFront()).Returns(sides[frontSideId]);
            mock.Setup(line => line.GetBack()).Returns(sides[backSideId]);

            lines.Add(mock.Object);
            
            return this;
        }

        /// <summary>
        /// Compiles the map from the previous commands. This is the terminal
        /// operation for the object.
        /// </summary>
        /// <returns>The compiled map.</returns>
        public IMap ToMap() => new GeneratedMap(this);
        
        private IVertex CreateVertex(Vec2D position)
        {
            var mock = new Mock<IVertex>();
            mock.Setup(v => v.Id).Returns(vertices.Count);
            mock.Setup(v => v.Position).Returns(position);
            
            vertices.Add(mock.Object);
            
            return mock.Object;
        }

        private class GeneratedMap : IMap
        {
            private readonly List<ILine> Lines;
            private readonly List<IThing> Things;
            private readonly List<ISector> Sectors;
            private readonly List<ISide> Sides;
            private readonly List<IVertex> Vertices;

            public string Name { get; } = "TEMP";
            public MapType MapType { get; } = MapType.Doom;
            public IReadOnlyList<ILine> GetLines() => Lines;
            public IReadOnlyList<ISector> GetSectors() => Sectors;
            public IReadOnlyList<ISide> GetSides() => Sides;
            public IReadOnlyList<IThing> GetThings() => Things;
            public IReadOnlyList<IVertex> GetVertices() => Vertices;
            
            internal GeneratedMap(GeometryGenerator generator)
            {
                Lines = generator.lines;
                Things = generator.things;
                Sectors = generator.sectors;
                Sides = generator.sides;
                Vertices = generator.vertices;
            }
        }
    }
}