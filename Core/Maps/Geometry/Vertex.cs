using Helion.Maps.Geometry.Lines;
using Helion.Util.Geometry;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assertion.Assert;

namespace Helion.Maps.Geometry
{
    public class Vertex
    {
        public readonly int Id;
        public readonly List<Line> Lines = new List<Line>();
        private Vec2D position;
        private Vec2Fixed fixedPosition;

        public Vec2D Position 
        {
            get => position;
            set 
            {
                position = value;
                fixedPosition = new Vec2Fixed(new Fixed(position.X), new Fixed(position.Y));
                Lines.ForEach(line => line.UpdateSegments());
            }
        }

        public Vec2Fixed FixedPosition => fixedPosition;

        public Vertex(int id, Vec2D pos)
        {
            Id = id;
            Position = pos;
        }

        public void Add(Line line)
        {
            Precondition(!Lines.Where(l => l.Id == line.Id).Any(), "Trying to add the same line twice");

            Lines.Add(line);
        }
    }
}
