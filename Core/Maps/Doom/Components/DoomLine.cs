using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials.Vanilla;

namespace Helion.Maps.Doom.Components
{
    public class DoomLine : ILine
    {
        public int Id { get; }
        public MapLineFlags Flags { get; }
        public DoomVertex Start;
        public DoomVertex End;
        public DoomSide Front;
        public DoomSide? Back;
        public VanillaLineSpecialType LineType;
        public ushort SectorTag;

        public Vec2D StartPosition => Start.Position;
        public Vec2D EndPosition => End.Position;
        public bool OneSided => Back == null;
        
        internal DoomLine(int id, DoomVertex start, DoomVertex end, DoomSide front, DoomSide? back, 
            MapLineFlags flags, VanillaLineSpecialType lineType, ushort sectorTag)
        {
            Id = id;
            Start = start;
            End = end;
            Front = front;
            Back = back;
            Flags = flags;
            LineType = lineType;
            SectorTag = sectorTag;
        }

        public IVertex GetStart() => Start;
        public IVertex GetEnd() => End;
        public ISide GetFront() => Front;
        public ISide? GetBack() => Back;
    }
}