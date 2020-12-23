using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Components;
using Helion.Maps.Components.GL;
using Helion.Resources.Archives;
using Helion.Util.Container;
using Helion.Util.Geometry.Vectors;
using Moq;

namespace Helion.Test.Helper.Map.Generator
{
    /// <summary>
    /// Allows for quick generation of map geometry when testing.
    /// </summary>
    public class GeometryGenerator
    {
        private readonly Dictionary<int, ILine> m_lines = new Dictionary<int, ILine>();
        private readonly Dictionary<int, IThing> m_things = new Dictionary<int, IThing>();
        private readonly Dictionary<int, ISector> m_sectors = new Dictionary<int, ISector>();
        private readonly Dictionary<int, ISide> m_sides = new Dictionary<int, ISide>();
        private readonly Dictionary<int, IVertex> m_vertices = new Dictionary<int, IVertex>();
        private readonly List<INode> m_nodes = new List<INode>();

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
            mock.Setup(sec => sec.Id).Returns(m_sectors.Count);
            mock.Setup(sec => sec.Tag).Returns(0);
            mock.Setup(sec => sec.FloorZ).Returns((short)floorZ);
            mock.Setup(sec => sec.CeilingZ).Returns((short)floorZ);
            mock.Setup(sec => sec.LightLevel).Returns(0);
            mock.Setup(sec => sec.FloorTexture).Returns("TEMP");
            mock.Setup(sec => sec.CeilingTexture).Returns("TEMP");

            m_sectors[mock.Object.Id] = mock.Object;

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
            mock.Setup(side => side.Id).Returns(m_sides.Count);
            mock.Setup(side => side.Offset).Returns(Vec2I.Zero);
            mock.Setup(side => side.UpperTexture).Returns("TEMP");
            mock.Setup(side => side.MiddleTexture).Returns("TEMP");
            mock.Setup(side => side.LowerTexture).Returns("TEMP");
            mock.Setup(side => side.GetSector()).Returns(m_sectors[sectorId]);

            m_sides[mock.Object.Id] = mock.Object;

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
            mock.Setup(line => line.Id).Returns(m_lines.Count);
            mock.Setup(line => line.GetStart()).Returns(startVertex);
            mock.Setup(line => line.GetStart()).Returns(endVertex);
            mock.Setup(line => line.GetFront()).Returns(m_sides[sideId]);
            mock.Setup(line => line.OneSided).Returns(true);

            m_lines[mock.Object.Id] = mock.Object;

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
            mock.Setup(line => line.Id).Returns(m_lines.Count);
            mock.Setup(line => line.GetStart()).Returns(startVertex);
            mock.Setup(line => line.GetStart()).Returns(endVertex);
            mock.Setup(line => line.GetFront()).Returns(m_sides[frontSideId]);
            mock.Setup(line => line.GetBack()).Returns(m_sides[backSideId]);
            mock.Setup(line => line.OneSided).Returns(false);

            m_lines[mock.Object.Id] = mock.Object;

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
            mock.Setup(v => v.Id).Returns(m_vertices.Count);
            mock.Setup(v => v.Position).Returns(position);

            m_vertices[mock.Object.Id] = mock.Object;

            return mock.Object;
        }

        private class GeneratedMap : IMap
        {
            private readonly Dictionary<int, ILine> Lines;
            private readonly List<INode> Nodes;
            private readonly Dictionary<int, IThing> Things;
            private readonly Dictionary<int, ISector> Sectors;
            private readonly Dictionary<int, ISide> Sides;
            private readonly Dictionary<int, IVertex> Vertices;
            public Archive Archive => null!;
            public string Name { get; } = "TEMP";
            public MapType MapType { get; } = MapType.Doom;
            public ICovariantReadOnlyDictionary<int, ILine> GetLines() => new ReadOnlyDictionary<int, ILine>(Lines);
            public IReadOnlyList<INode> GetNodes() => Nodes;
            public ICovariantReadOnlyDictionary<int, ISector> GetSectors() => new ReadOnlyDictionary<int, ISector>(Sectors);
            public ICovariantReadOnlyDictionary<int, ISide> GetSides() => new ReadOnlyDictionary<int, ISide>(Sides);
            public ICovariantReadOnlyDictionary<int, IThing> GetThings() => new ReadOnlyDictionary<int, IThing>(Things);
            public ICovariantReadOnlyDictionary<int, IVertex> GetVertices() => new ReadOnlyDictionary<int, IVertex>(Vertices);
            public GLComponents? GL => null;

            internal GeneratedMap(GeometryGenerator generator)
            {
                Lines = generator.m_lines;
                Nodes = generator.m_nodes;
                Things = generator.m_things;
                Sectors = generator.m_sectors;
                Sides = generator.m_sides;
                Vertices = generator.m_vertices;
            }
        }
    }
}