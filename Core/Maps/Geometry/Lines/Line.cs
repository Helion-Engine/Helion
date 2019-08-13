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

        // TODO fix me
        public IList<Side> Sides => Containers.WithoutNulls(Front, Back);

        public bool HasSpecial => Special.LineSpecialType != ZLineSpecialType.None;

        public LineSpecial Special;

        public bool Activated;

        public bool HasSectorTag => SectorTag > 0;

        public int SectorTag => Args[0];

        public byte[] Args;

        public byte TagArg => Args[0];
        public byte SpeedArg => Args[1];
        public byte DelayArg => Args[2];
        public byte AmountArg => Args[2];


        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back = null)
            : this(id, startVertex, endVertex, front, back, default, new LineSpecial(ZLineSpecialType.None), null)
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

        public Line(int id, Vertex startVertex, Vertex endVertex, Side front, Side? back, LineFlags lineFlags, LineSpecial? special, byte[] args)
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

            Special = special;
     
            Args = args;
        }

        /// <summary>
        /// To be invoked when the constructor is called or vertices update, so
        /// the two segments are up to date.
        /// </summary>
        internal void UpdateSegments() => Segment = new Seg2D(StartVertex.Position, EndVertex.Position);
    }
}
