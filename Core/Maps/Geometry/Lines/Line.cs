using System.Collections.Generic;
using Helion.Maps.Special;
using Helion.Util.Container;
using Helion.Util.Geometry;
using Helion.World.Entities;

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

        public bool HasSpecial => Special.LineSpecialType != LineSpecialType.None;

        public LineSpecial Special;

        public bool Activated;

        public bool HasSectorTag => SectorTag > 0;

        public int SectorTag;

        public byte[] Args;


        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back = null)
            : this(id, startVertex, endVertex, front, back, default, LineSpecialType.None, 0, null)
        {

        }


        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back, LineFlags lineFlags, LineSpecialType special, int sectorTag)
            : this(id, startVertex, endVertex, front, back, lineFlags, special, sectorTag, null)
        {

        }

        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back, LineFlags lineFlags, LineSpecialType special, byte[] args)
            : this(id, startVertex, endVertex, front, back, lineFlags, special, 0, args)
        {

        }

        /// <summary>
        /// If the line blocks the given entity. Only checks line properties and flags. No sector checking.
        /// </summary>
        public bool BlocksEntity(Entity entity)
        {
            if (OneSided)
                return true;

            if (entity.Player != null)
                return Flags.Blocking.Players;

            return false;
        }

        private Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back, LineFlags lineFlags, LineSpecialType special, int sectorTag, byte[]? args)
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

            Special = new LineSpecial(special);
            SectorTag = sectorTag;

            if (args == null)
                Args = new byte[0];
            else
                Args = args;
        }

        /// <summary>
        /// To be invoked when the constructor is called or vertices update, so
        /// the two segments are up to date.
        /// </summary>
        internal void UpdateSegments() => Segment = new Seg2D(StartVertex.Position, EndVertex.Position);
    }
}
