using Helion.Util.Geometry.Vectors;

namespace Helion.Models
{
    public class EntityModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int ThingId { get; set; }
        public double AngleRadians { get; set; }
        public EntityBoxModel Box { get; set; }
        public Vec3D SpawnPoint { get; set; }

        public Vec3D Velocity { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public string? ArmorDefinition { get; set; }
        public int FrozenTics { get; set; }
        public int MoveCount { get; set; }
        public int Sector { get; set; }
        public int? Owner { get; set; }
        public int? Target { get; set; }
        public int? Tracer { get; set; }

        public bool Refire { get; set; }
        public bool MoveLinked { get; set; }
        public bool Respawn { get; set; }

        public int MoveDir { get; set; }
        public bool BlockFloat { get; set; }

        public FrameStateModel Frame { get; set; }
        public EntityFlagsModel Flags { get; set; }
        public EntityPropertiesModel Properties { get; set; }
    }
}
