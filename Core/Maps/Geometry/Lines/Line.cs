using System.Collections.Generic;
using Helion.Util.Container;
using Helion.Util.Geometry;

namespace Helion.Maps.Geometry.Lines
{
    public class Line
    {
        public readonly int Id;
        
        public readonly Vertex StartVertex;
        
        public readonly Vertex EndVertex;
        
        public Side Front;
        
        public Side? Back;
        
        public LineFlags Flags;
        
        public Seg2D Segment;

        public bool OneSided => Back == null;
        
        public bool TwoSided => !OneSided;

        public IList<Side> Sides => Containers.WithoutNulls(Front, Back);

        public Line(int id, Vertex startVertex, Vertex endVertex, LineFlags lineFlags, Side front, Side? back = null)
        {
            Id = id;
            StartVertex = startVertex;
            EndVertex = endVertex;
            Front = front;
            Back = back;
            Flags = lineFlags;
            Segment = new Seg2D(StartVertex.Position, EndVertex.Position);

            startVertex.Add(this);
            endVertex.Add(this);

            front.Line = this;
            if (back != null)
                back.Line = this;
        }

        /// <summary>
        /// To be invoked when the constructor is called or vertices update, so
        /// the two segments are up to date.
        /// </summary>
        internal void UpdateSegments() => Segment = new Seg2D(StartVertex.Position, EndVertex.Position);
    }
}
