using Helion.Audio;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using System;

namespace Helion.World.Special.Specials
{
    [Flags]
    public enum TeleportFog
    {
        None = 0,
        Source = 1,
        Dest = 2
    }

    public class TeleportSpecial : ISpecial
    {
        private const int TeleportFreezeTicks = 18;
        private const int TeleportOffsetDist = 16;
        
        private readonly EntityActivateSpecialEventArgs m_args;
        private readonly IWorld m_world;
        private readonly TeleportFog m_fogFlags;

        public static TeleportFog GetTeleportFog(Line line)
        {
            switch (line.Special.LineSpecialType)
            {
                case ZDoomLineSpecialType.Teleport:
                    if (line.Args.Arg2 == 0)
                        return TeleportFog.Source | TeleportFog.Dest;
                    else
                        return TeleportFog.Dest;
                default:
                    break;
            }

            return TeleportFog.None;
        }

        public TeleportSpecial(EntityActivateSpecialEventArgs args, IWorld world, TeleportFog flags)
        {
            m_args = args;
            m_world = world;
            m_fogFlags = flags;
        }

        public SpecialTickStatus Tick()
        {
            Entity entity = m_args.Entity;
            Entity? teleportSpot = FindTeleportSpot();
            if (teleportSpot == null)
                return SpecialTickStatus.Destroy;

            Vec3D oldPosition = entity.Position;
            if (Teleport(entity, teleportSpot))
            {
                if ((m_fogFlags & TeleportFog.Source) != 0)
                    CreateTeleportFog(oldPosition, entity.AngleRadians);

                if ((m_fogFlags & TeleportFog.Dest) != 0)
                    CreateTeleportFog(entity.Position, entity.AngleRadians);
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

        private void CreateTeleportFog(in Vec3D pos, double angle)
        {
            Entity? teleport = CreateTeleportFogAt(pos + (Vec3D.Unit(angle, 0.0) * TeleportOffsetDist));
            if (teleport != null)
                m_world.SoundManager.CreateSoundOn(teleport, Constants.TeleportSound, SoundChannelType.Auto, new SoundParams(teleport));
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