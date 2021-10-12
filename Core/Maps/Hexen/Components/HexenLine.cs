using Helion.Geometry.Vectors;
using Helion.Maps.Components;
using Helion.Maps.Doom.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;

namespace Helion.Maps.Hexen.Components;

public class HexenLine : ILine
{
    public int Id { get; }
    public DoomVertex Start;
    public DoomVertex End;
    public DoomSide Front;
    public DoomSide? Back;
    public MapLineFlags Flags { get; }
    public ZDoomLineSpecialType LineType;
    public SpecialArgs Args;

    public Vec2D StartPosition => Start.Position;
    public Vec2D EndPosition => End.Position;
    public bool OneSided => Back == null;

    internal HexenLine(int id, DoomVertex start, DoomVertex end, DoomSide front, DoomSide? back,
        MapLineFlags flags, ZDoomLineSpecialType lineType, SpecialArgs args)
    {
        Id = id;
        Start = start;
        End = end;
        Front = front;
        Back = back;
        Flags = flags;
        LineType = lineType;
        Args = args;
    }

    public IVertex GetStart() => Start;
    public IVertex GetEnd() => End;
    public ISide GetFront() => Front;
    public ISide? GetBack() => Back;
}

