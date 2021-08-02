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
        private readonly int m_lineId;
        private readonly bool m_teleportLineReverse;
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

        public TeleportSpecial(EntityActivateSpecialEventArgs args, IWorld world, int lineId, TeleportFog flags,
            TeleportType type = TeleportType.Doom, bool reverseLine = false)
        {
            m_args = args;
            m_world = world;
            m_lineId = lineId;
            m_teleportLineReverse = reverseLine;
            m_fogFlags = flags;
            m_type = type;
        }

        public SpecialTickStatus Tick()
        {
            Entity entity = m_args.Entity;
            if (!FindTeleportSpot(entity, out Vec3D pos, out double angle))
                return SpecialTickStatus.Destroy;

            Vec3D oldPosition = entity.Position;
            if (Teleport(entity, pos, angle))
            {
                if ((m_fogFlags & TeleportFog.Source) != 0)
                    m_world.CreateTeleportFog(oldPosition + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));

                if ((m_fogFlags & TeleportFog.Dest) != 0)
                    m_world.CreateTeleportFog(entity.Position + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));
            }

            return SpecialTickStatus.Destroy;
        }

        private bool Teleport(Entity entity, in Vec3D pos, double teleportAngle)
        {
            if (!CanTeleport(entity, pos))
                return false;

            entity.UnlinkFromWorld();
            entity.SetPosition(pos);

            if (m_type == TeleportType.Doom)
            {
                entity.FrozenTics = TeleportFreezeTicks;
                entity.Velocity = Vec3D.Zero;
                entity.AngleRadians = teleportAngle;

                if (entity is Player player)
                    player.PitchRadians = 0;
            }
            else if (m_type == TeleportType.BoomCompat || m_type == TeleportType.BoomFixed)
            {
                double oldAngle = entity.AngleRadians;
                Line sourceLine = m_args.ActivateLineSpecial;
                if (m_type == TeleportType.BoomFixed)
                {
                    double sourceCrossAngle = sourceLine.StartPosition.Angle(sourceLine.EndPosition) + entity.AngleRadians;
                    entity.AngleRadians = teleportAngle + sourceCrossAngle;
                }
                else
                {
                    Vec2D pt = sourceLine.EndPosition - sourceLine.StartPosition;
                    teleportAngle = Vec2D.Zero.Angle(pt) - teleportAngle + MathHelper.HalfPi;
                    entity.AngleRadians += teleportAngle;
                }

                Vec2D velocity = entity.Velocity.XY.Rotate(entity.AngleRadians - oldAngle);
                entity.Velocity.X = velocity.X;
                entity.Velocity.Y = velocity.Y;
            }

            entity.ResetInterpolation();
            entity.CheckOnGround();

            m_world.Link(entity);
            m_world.TelefragBlockingEntities(entity);

            return true;
        }

        private static bool CanTeleport(Entity teleportEntity, in Vec3D pos)
        {
            if (teleportEntity is Player)
                return true;

            return teleportEntity.GetIntersectingEntities3D(pos, BlockmapTraverseEntityFlags.Solid).Count == 0;
        }

        public bool Use(Entity entity)
        {
            return false;
        }

        private bool FindTeleportSpot(Entity teleportEntity, out Vec3D pos, out double angle)
        {
            pos = Vec3D.Zero;
            angle = 0;

            if (m_tid == EntityManager.NoTid && m_tag == Sector.NoTag && m_lineId == Line.NoLineId)
                return false;

            if (m_lineId != Line.NoLineId)
            {
                Line sourceLine = m_args.ActivateLineSpecial;
                foreach (Line line in m_world.FindByLineId(m_lineId))
                {
                    if (line.Id == sourceLine.Id)
                        continue;

                    // Exit position is proportional to the position on the source teleport line
                    // TODO these line exit angles aren't correct
                    double time = sourceLine.Segment.ToTime(teleportEntity.Position.XY);
                    Vec2D destLinePos = line.Segment.FromTime(1.0 - time);
                    pos = destLinePos.To3D(GetTeleportLineZ(line, destLinePos));
                    angle = line.Segment.End.Angle(line.Segment.Start);
                    if (m_teleportLineReverse)
                        angle += MathHelper.Pi;
                    return true;
                }
            }
            if (m_tid == EntityManager.NoTid)
            {
                foreach (Sector sector in m_world.FindBySectorTag(m_tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.Flags.IsTeleportSpot)
                        {
                            pos = entity.Position;
                            angle = entity.AngleRadians;
                            return true;
                        }
            } 
            else if (m_tag == Sector.NoTag)
            {
                foreach (Entity entity in m_world.FindByTid(m_tid))
                    if (entity.Flags.IsTeleportSpot)
                    {
                        pos = entity.Position;
                        angle = entity.AngleRadians;
                        return true;
                    }
            }
            else
            {
                foreach (Sector sector in m_world.FindBySectorTag(m_tag))
                    foreach (Entity entity in sector.Entities)
                        if (entity.ThingId == m_tid && entity.Flags.IsTeleportSpot)
                        {
                            pos = entity.Position;
                            angle = entity.AngleRadians;
                            return true;
                        }
            }
            
            return false;
        }

        private static double GetTeleportLineZ(Line line, in Vec2D lineCenter)
        {
            if (line.Back == null)
                return line.Front.Sector.ToFloorZ(lineCenter);

            return Math.Max(line.Front.Sector.ToFloorZ(lineCenter), line.Back.Sector.ToFloorZ(lineCenter));
        }
    }
}