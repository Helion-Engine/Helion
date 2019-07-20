using Helion.Maps.Actions;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Maps.Things
{
    public class Thing
    {
        public readonly int Id;
        public Vec3D Position;
        public double AngleRadians;
        public ushort EditorNumber;
        public ushort Tid;
        public ThingFlags Flags;
        public ActionSpecial Special;

        public Thing(int id, Vec2D position, double angleRadians, ushort editorNumber, ushort flags)
        {
            Id = id;
            Position = new Vec3D(position.X, position.Y, 0.0);
            AngleRadians = angleRadians;
            EditorNumber = editorNumber;
            Flags = new ThingFlags(flags);
            Special = new ActionSpecial();
        }
        
        public Thing(int id, Vec3D position, ushort tid, double angleRadians, ushort editorNumber, ushort flags,
            ActionSpecial actionSpecial)
        {
            Id = id;
            Position = new Vec3D(position.X, position.Y, 0.0);
            AngleRadians = angleRadians;
            EditorNumber = editorNumber;
            Tid = tid;
            Flags = new ThingFlags(flags);
            Special = actionSpecial;
        }
    }
}