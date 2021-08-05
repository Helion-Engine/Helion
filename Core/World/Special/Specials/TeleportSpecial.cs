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
            if (!FindTeleportSpot(entity, out Vec3D pos, out double angle, out double offsetZ))
                return SpecialTickStatus.Destroy;

            Vec3D oldPosition = entity.Position;
            if (Teleport(entity, pos, angle, offsetZ))
            {
                if ((m_fogFlags & TeleportFog.Source) != 0)
                    m_world.CreateTeleportFog(oldPosition + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));

                if ((m_fogFlags & TeleportFog.Dest) != 0)
                    m_world.CreateTeleportFog(entity.Position + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));
            }

            return SpecialTickStatus.Destroy;
        }

        private bool Teleport(Entity entity, Vec3D pos, double teleportAngle, double offsetZ)
        {
            pos.Z += offsetZ;
            if (!CanTeleport(entity, pos))
                return false;

            double oldAngle = entity.AngleRadians;
            Vec3D oldPos = entity.Position;
            entity.UnlinkFromWorld();
            entity.SetPosition(pos);
            Player? player = entity as Player;

            if (m_type == TeleportType.Doom)
            {
                entity.FrozenTics = TeleportFreezeTicks;
                entity.Velocity = Vec3D.Zero;
                entity.AngleRadians = teleportAngle;

                if (player != null)
                    player.PitchRadians = 0;
            }
            else if (m_type == TeleportType.BoomCompat || m_type == TeleportType.BoomFixed)
            {

                Line sourceLine = m_args.ActivateLineSpecial;

                // Only use these calculations for Teleporting to a sector with teleport thing. For line teleport using the angle given. 
                if (m_lineId == Line.NoLineId)
                {
                    if (m_type == TeleportType.BoomFixed)
                        entity.AngleRadians = teleportAngle + entity.AngleRadians - sourceLine.StartPosition.Angle(sourceLine.EndPosition) - MathHelper.HalfPi;
                    else
                        entity.AngleRadians += sourceLine.StartPosition.Angle(sourceLine.EndPosition) - teleportAngle + MathHelper.HalfPi;
                }
                else
                {
                    entity.AngleRadians = teleportAngle;
                }

                Vec2D velocity = entity.Velocity.XY.Rotate(entity.AngleRadians - oldAngle);
                entity.Velocity.X = velocity.X;
                entity.Velocity.Y = velocity.Y;
            }

            m_world.Link(entity, true);
            if (entity.Position.Z - entity.HighestFloorZ != offsetZ)
                OffsetTeleportZ(entity, offsetZ);

            if (m_lineId == Line.NoLineId)
                entity.ResetInterpolation();
            else if (player != null)
                TranslateTeleportInterpolation(player, oldPos, oldAngle);

            entity.CheckOnGround();
            m_world.TelefragBlockingEntities(entity);
            return true;
        }

        private void OffsetTeleportZ(Entity entity, double offsetZ)
        {
            // Carry over the offset from the floor to the new teleport position.
            // This floor can be different thant he previous.
            // E.g. player z is 32 above a floor height of 0.
            // Teleport moves player to a floor height of 64 so player z should now be 96.
            Vec3D pos;
            entity.UnlinkFromWorld();
            pos = entity.Position;
            pos.Z = entity.HighestFloorZ + offsetZ;
            entity.SetPosition(pos);
            m_world.Link(entity, true);
        }

        private static void TranslateTeleportInterpolation(Player player, in Vec3D oldPos, double oldAngle)
        {
            // Teleport line needs to translate interpolation values so the teleport is as seamless as possible.
            Vec2D diffPos2D = oldPos.XY - player.PrevPosition.XY;
            diffPos2D = diffPos2D.Rotate(player.AngleRadians - oldAngle);
            double diffAngle = oldAngle - player.PrevAngle;

            player.PrevAngle = player.AngleRadians - diffAngle;
            player.PrevPosition.X = player.Position.X - diffPos2D.X;
            player.PrevPosition.Y = player.Position.Y - diffPos2D.Y;
            player.PrevPosition.Z = player.Position.Z - (oldPos.Z - player.PrevPosition.Z);
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

        private bool FindTeleportSpot(Entity teleportEntity, out Vec3D pos, out double angle, out double offsetZ)
        {
            pos = Vec3D.Zero;
            angle = 0;
            offsetZ = 0;

            if (m_tid == EntityManager.NoTid && m_tag == Sector.NoTag && m_lineId == Line.NoLineId)
                return false;

            if (m_lineId != Line.NoLineId)
            {
                Line sourceLine = m_args.ActivateLineSpecial;
                foreach (Line line in m_world.FindByLineId(m_lineId))
                {
                    if (line.Id == sourceLine.Id)
                        continue;

                    angle = line.StartPosition.Angle(line.EndPosition) - sourceLine.StartPosition.Angle(sourceLine.EndPosition);
                    if (!m_teleportLineReverse)
                        angle += MathHelper.Pi;

                    angle += teleportEntity.AngleRadians;

                    // Exit position is proportional to the position on the source teleport line
                    double time = sourceLine.Segment.ToTime(teleportEntity.Position.XY);
                    Vec2D destLinePos = line.Segment.FromTime(1.0 - time);
                    pos = destLinePos.To3D(GetTeleportLineZ(teleportEntity, line, destLinePos, out offsetZ));

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

        private static double GetTeleportLineZ(Entity teleportEntity, Line line, in Vec2D lineCenter, out double offsetZ)
        {
            // This may not be the correct Z position but get the most valid position available.
            // Teleport will check once the entity is teleported that the new offset is equal to the current.
            offsetZ = teleportEntity.Position.Z - teleportEntity.HighestFloorZ;
            if (line.Back == null)
                return line.Front.Sector.ToFloorZ(lineCenter);

            return Math.Max(line.Front.Sector.ToFloorZ(lineCenter), line.Back.Sector.ToFloorZ(lineCenter));
        }
    }
}