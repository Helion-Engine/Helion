using System.Collections.Generic;
using System.Linq;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Geometry
{
    public class Vertex
    {
        public readonly int Id;
        public readonly Vec2D Position;
        public readonly List<Line> Lines = new List<Line>();

        public Vertex(int id, Vec2D pos)
        {
            Id = id;
            Position = pos;
        }

        public void Add(Line line)
        {
            Precondition(Lines.All(l => l.Id != line.Id), "Trying to add the same line twice");

            Lines.Add(line);
        }
    }
}