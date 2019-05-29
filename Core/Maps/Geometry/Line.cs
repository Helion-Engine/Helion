using Helion.Util.Container;
using Helion.Util.Geometry;
using System.Collections.Generic;

namespace Helion.Maps.Geometry
{
    public class Line : Seg2D
    {
        public readonly int Id;
        public readonly Vertex StartVertex;
        public readonly Vertex EndVertex;
        public Side Front;
        public Side? Back;

        public IList<Side> Sides { get { return Containers.WithoutNulls(Front, Back); } }

        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back = null) : 
            base(startVertex.Position, endVertex.Position)
        {
            Id = id;
            StartVertex = startVertex;
            EndVertex = endVertex;
            Front = front;
            Back = back;

            startVertex.Add(this);
            endVertex.Add(this);

            front.Line = this;
            if (back != null)
                back.Line = this;
        }
    }
}
