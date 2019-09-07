using Helion.Maps.Components;
using Helion.Maps.Shared;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Util.Geometry;

namespace Helion.Maps.Hexen.Components
{
    public class HexenThing : IThing
    {
        public int Id { get; }
        public int ThingId { get; }
        public Vec3Fixed Position { get; }
        public ushort Angle { get; }
        public ushort EditorNumber { get; }
        public ThingFlags Flags { get; }
        public readonly ZDoomLineSpecialType Special;
        public readonly SpecialArgs Args;
        
        internal HexenThing(int id, ushort tid, Vec3Fixed position, ushort angle, ushort editorNumber, 
            ThingFlags flags, ZDoomLineSpecialType special, SpecialArgs args)
        {
            Id = id;
            ThingId = tid;
            Position = position;
            Angle = angle;
            EditorNumber = editorNumber;
            Flags = flags;
            Special = special;
            Args = args;
        }
    }
}