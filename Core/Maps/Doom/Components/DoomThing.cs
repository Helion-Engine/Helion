using Helion.Maps.Components;
using Helion.Maps.Doom.Components.Types;
using Helion.Util.Geometry;

namespace Helion.Maps.Doom.Components
{
    public class DoomThing : IThing
    {
        public int Id { get; }
        public Vec3Fixed Position { get; }
        public ushort Angle { get; }
        public ushort EditorNumber { get; }
        public readonly DoomThingFlags Flags;

        internal DoomThing(int id, Vec2Fixed position, ushort angle, ushort editorNumber, DoomThingFlags flags)
        {
            Id = id;
            Position = new Vec3Fixed(position.X, position.Y, Fixed.Lowest());
            Angle = angle;
            EditorNumber = editorNumber;
            Flags = flags;
        }
    }
}