using Helion.Audio;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using Helion.Worlds.Entities;
using Helion.Worlds.Entities.Definition;
using Helion.Worlds.Entities.Players;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Physics;
using Helion.Worlds.Physics.Blockmap;
using Helion.Worlds.Sound;

namespace Helion.Worlds.Special.Specials
{
    public class TeleportSpecial : ISpecial
    {
        private const int TeleportFreezeTicks = 18;
        private const int TeleportOffsetDist = 16;

        private readonly EntityActivateSpecialEventArgs m_args;
        private readonly World m_world;

        public TeleportSpecial(EntityActivateSpecialEventArgs args, World world)
        {
            m_args = args;
            m_world = world;
        }

        public SpecialTickStatus Tick()
        {
            Entity entity = m_args.Entity;
            Entity? teleportSpot = FindTeleportSpot();
            if (teleportSpot == null)
                return SpecialTickStatus.Destroy;

            if (Teleport(entity, teleportSpot))
            {
                Entity? teleport = CreateTeleportFogAt(entity.Position + (Vec3D.Unit(entity.AngleRadians, 0.0) * TeleportOffsetDist));
                if (teleport != null)
                    m_world.SoundManager.CreateSoundOn(teleport, Constants.TeleportSound, SoundChannelType.Auto, new SoundParams(teleport));
            }

            return SpecialTickStatus.Destroy;
        }

        private bool Teleport(Entity entity, Entity teleportSpot)
        {
            if (!CanTeleport(entity, teleportSpot))
                return false;

            entity.UnlinkFromWorld();

            entity.FrozenTics = TeleportFreezeTicks;
            entity.Velocity = Vec3D.Zero;
            entity.SetPosition(teleportSpot.Position);
            entity.AngleRadians = teleportSpot.AngleRadians;
            if (entity is Player player)
                player.PitchRadians = 0;

            entity.ResetInterpolation();
            entity.CheckOnGround();

            m_world.Link(entity);
            m_world.TelefragBlockingEntities(entity);

            return true;
        }

        private bool CanTeleport(Entity teleportEntity, Entity teleportSpot)
        {
            if (teleportEntity is Player)
                return true;

            return teleportEntity.GetIntersectingEntities3D(teleportSpot.Position, BlockmapTraverseEntityFlags.Solid).Count == 0;
        }

        private Entity? CreateTeleportFogAt(in Vec3D pos)
        {
            EntityDefinition? definition = m_world.EntityManager.DefinitionComposer.GetByName("TeleportFog");
            if (definition != null)
                return m_world.EntityManager.Create(definition, pos, 0.0, 0.0, 0);

            return null;
        }

        public void Use(Entity entity)
        {
        }

        private Entity? FindTeleportSpot()
        {
            Line line = m_args.ActivateLineSpecial;
            int tid = line.Args.Arg0;
            int tag = line.Args.Arg1;

            if (tid == EntityManager.NoTid && tag == Sector.NoTag)
                return null;

            if (tid == EntityManager.NoTid)
            {
                foreach (Sector sector in m_world.FindBySectorTag(tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.Flags.IsTeleportSpot)
                            return entity;
            }
            else if (tag == Sector.NoTag)
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