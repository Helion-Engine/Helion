using Helion.Util.Container;
using Helion.Util.Geometry;
using System.Collections.Generic;

namespace Helion.Maps.Geometry
{
    public class Line
    {
        public readonly int Id;
        public readonly Vertex StartVertex;
        public readonly Vertex EndVertex;
        public Side Front;
        public Side? Back;
        private Seg2D segment;
        public Seg2Fixed SegmentFixed { get; private set; }

        public Seg2D Segment 
        {
            get => segment;
            set 
            {
                segment = value;
                SegmentFixed = new Seg2Fixed(StartVertex.FixedPosition, EndVertex.FixedPosition);
            }
        }

        public bool OneSided => Back == null;
        public bool TwoSided => !OneSided;

        public IList<Side> Sides { get { return Containers.WithoutNulls(Front, Back); } }

        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back = null)
        {
            Id = id;
            StartVertex = startVertex;
            EndVertex = endVertex;
            Front = front;
            Back = back;
            segment = new Seg2D(StartVertex.Position, EndVertex.Position);
            SegmentFixed = new Seg2Fixed(StartVertex.FixedPosition, EndVertex.FixedPosition);

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
