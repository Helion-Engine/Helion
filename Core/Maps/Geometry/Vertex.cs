using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.Maps.Geometry
{
    public class Vertex
    {
        public readonly int Id;
        public Vec2D Position;
        public readonly List<Line> Lines = new List<Line>();

        public Vertex(int id, Vec2D position)
        {
            Id = id;
            Position = position;
        }

        public void Add(Line line)
        {
            Precondition(!Lines.Where(l => l.Id == line.Id).Any(), "Trying to add the same line twice");

            Lines.Add(line);
        }
    }
}
