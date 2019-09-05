using Helion.Util.Geometry;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;

namespace Helion.World.Special.Specials
{
    public class TeleportSpecial : ISpecial
    {
        private const int TeleportFreezeTicks = 18;

        public Sector? Sector { get; } = null;
        private readonly EntityActivateSpecialEventArgs m_args;
        private readonly IWorld m_world;

        public TeleportSpecial(EntityActivateSpecialEventArgs args, IWorld world)
        {
            m_args = args;
            m_world = world;
        }

        public SpecialTickStatus Tick(long gametic)
        {
            Entity entity = m_args.Entity;
            Entity? teleportSpot = FindTeleportSpot();
            if (teleportSpot == null)
                return SpecialTickStatus.Destroy;

            entity.UnlinkFromWorld();

            entity.FrozenTics = TeleportFreezeTicks;
            entity.Velocity = Vec3D.Zero;
            entity.SetPosition(entity.Position);
            entity.AngleRadians = teleportSpot.AngleRadians;
            if (entity.Player != null)
                entity.Player.Pitch = 0;

            m_world.Link(entity);

            entity.ResetInterpolation();
            entity.OnGround = entity.CheckOnGround();

            return SpecialTickStatus.Destroy;
        }

        public void Use()
        {
        }

        private Entity? FindTeleportSpot()
        {
            Line line = m_args.ActivateLineSpecial;
            int tid = line.Args.Arg0;
            int tag = line.Args.Arg1;

            if (tid == EntityManager.NoTid && tag == 0)
                return null;
            
            if (tid == EntityManager.NoTid)
            {
                foreach (Sector sector in m_world.FindBySectorTag(tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.Flags.IsTeleportSpot)
                            return entity;
            } 
            else if (tag == 0)
            {
                foreach (Entity entity in m_world.FindByTid(tid))
                    if (entity.Flags.IsTeleportSpot)
                        return entity;
            }
            else
            {
                foreach (Sector sector in m_world.FindBySectorTag(tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.ThingId == tid && entity.Flags.IsTeleportSpot)
                            return entity;
            }
            
            return null;
        }
    }
}