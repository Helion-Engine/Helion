using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using System;
using Helion.Geometry.Vectors;

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
        
        private readonly EntityActivateSpecialEventArgs m_args;
        private readonly IWorld m_world;
        private readonly int m_tid;
        private readonly int m_tag;
        private readonly TeleportFog m_fogFlags;
        private readonly TeleportType m_type;

        public static TeleportFog GetTeleportFog(Line line)
        {
            switch (line.Special.LineSpecialType)
            {
                case ZDoomLineSpecialType.Teleport:
                    if (line.Args.Arg2 == 0)
                        return TeleportFog.Source | TeleportFog.Dest;
                    else
                        return TeleportFog.Dest;
            }

            return TeleportFog.None;
        }

        public TeleportSpecial(EntityActivateSpecialEventArgs args, IWorld world, int tid, int tag, TeleportFog flags, 
            TeleportType type = TeleportType.Doom)
        {
            m_args = args;
            m_world = world;
            m_tid = tid;
            m_tag = tag;
            m_fogFlags = flags;
            m_type = type;
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
                    m_world.CreateTeleportFog(oldPosition + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));

                if ((m_fogFlags & TeleportFog.Dest) != 0)
                    m_world.CreateTeleportFog(entity.Position + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));
            }

            return SpecialTickStatus.Destroy;
        }

        private bool Teleport(Entity entity, Entity teleportSpot)
        {
            if (!CanTeleport(entity, teleportSpot))
                return false;

            entity.UnlinkFromWorld();

            entity.SetPosition(teleportSpot.Position);

            if (m_type == TeleportType.Doom)
            {
                entity.FrozenTics = TeleportFreezeTicks;
                entity.Velocity = Vec3D.Zero;
                entity.AngleRadians = teleportSpot.AngleRadians;
            }
            else if (m_type == TeleportType.BoomCompat || m_type == TeleportType.BoomFixed)
            {
                Line line = m_args.ActivateLineSpecial;
                Vec2D pt = line.EndPosition - line.StartPosition;
                double angle;
                if (m_type == TeleportType.BoomFixed)
                    angle = pt.Angle(Vec2D.Zero) - teleportSpot.AngleRadians + MathHelper.HalfPi;
                else
                    angle = Vec2D.Zero.Angle(pt) - teleportSpot.AngleRadians + MathHelper.HalfPi;

                entity.AngleRadians += angle;

                Vec2D velocity = entity.Velocity.XY;
                Vec2D unit = Vec2D.UnitCircle(angle);

                entity.Velocity.X = velocity.X * unit.X - velocity.Y * unit.Y;
                entity.Velocity.Y = velocity.Y * unit.X + velocity.X * unit.Y;
            }

            if (entity is Player player)
                player.PitchRadians = 0;

            entity.ResetInterpolation();
            entity.CheckOnGround();

            m_world.Link(entity);
            m_world.TelefragBlockingEntities(entity);

            return true;
        }

        private static bool CanTeleport(Entity teleportEntity, Entity teleportSpot)
        {
            if (teleportEntity is Player)
                return true;

            return teleportEntity.GetIntersectingEntities3D(teleportSpot.Position, BlockmapTraverseEntityFlags.Solid).Count == 0;
        }

        public bool Use(Entity entity)
        {
            return false;
        }

        private Entity? FindTeleportSpot()
        {
            if (m_tid == EntityManager.NoTid && m_tag == Sector.NoTag)
                return null;
            
            if (m_tid == EntityManager.NoTid)
            {
                foreach (Sector sector in m_world.FindBySectorTag(m_tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.Flags.IsTeleportSpot)
                            return entity;
            } 
            else if (m_tag == Sector.NoTag)
            {
                foreach (Entity entity in m_world.FindByTid(m_tid))
                    if (entity.Flags.IsTeleportSpot)
                        return entity;
            }
            else
            {
                foreach (Sector sector in m_world.FindBySectorTag(m_tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.ThingId == m_tid && entity.Flags.IsTeleportSpot)
                            return entity;
            }
            
            return null;
        }
    }
}