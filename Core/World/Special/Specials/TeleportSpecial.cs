using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.Geometry.Vectors;
using Helion.Util.Container;
using Helion.Resources.Definitions.MapInfo;
using Helion.World.Entities.Definition;
using System;

namespace Helion.World.Special.Specials;

[Flags]
public enum TeleportFog
{
    None = 0,
    Source = 1,
    Dest = 2
}

public struct TeleportSpecial
{
    public const int TeleportFreezeTicks = 18;

    private readonly EntityActivateSpecial m_args;
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

    public TeleportSpecial(in EntityActivateSpecial args, IWorld world, int tid, int tag, TeleportFog flags,
        TeleportType type = TeleportType.Doom)
    {
        m_args = args;
        m_world = world;
        m_tid = tid;
        m_tag = tag;
        m_fogFlags = flags;
        m_type = type;
    }

    public TeleportSpecial(in EntityActivateSpecial args, IWorld world, int lineId, TeleportFog flags,
        TeleportType type = TeleportType.Doom, bool reverseLine = false)
    {
        m_args = args;
        m_world = world;
        m_lineId = lineId;
        m_teleportLineReverse = reverseLine;
        m_fogFlags = flags;
        m_type = type;
    }

    public bool Teleport()
    {
        Entity entity = m_args.Entity;
        if (!FindTeleportSpot(entity, out Vec3D pos, out double angle, out double offsetZ))
            return false;

        bool isMonsterCloset = entity.InMonsterCloset;
        Vec3D oldPosition = entity.Position;
        if (Teleport(entity, pos, angle, offsetZ))
        {
            if (!isMonsterCloset && (m_fogFlags & TeleportFog.Source) != 0)
                m_world.CreateTeleportFog(oldPosition + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));

            if ((m_fogFlags & TeleportFog.Dest) != 0)
                m_world.CreateTeleportFog(entity.Position + (Vec3D.UnitSphere(entity.AngleRadians, 0.0) * Constants.TeleportOffsetDist));
            return true;
        }

        return false;
    }

    private bool Teleport(Entity entity, Vec3D pos, double teleportAngle, double offsetZ)
    {
        pos.Z += offsetZ;
        if (!CanTeleport(entity, pos))
            return false;

        entity.Flags.Teleport = true;

        double oldAngle = entity.AngleRadians;
        Vec3D oldPos = entity.Position;
        entity.UnlinkFromWorld();
        entity.SetPosition(pos);
        Player? player = entity.PlayerObj;

        if (m_type == TeleportType.Doom)
        {
            if (entity.IsPlayer)
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

        if (m_lineId == Line.NoLineId)
            entity.ResetInterpolation();
        else
            TranslateTeleportInterpolation(entity, player, oldPos, oldAngle);

        m_world.TelefragBlockingEntities(entity);
        m_world.Link(entity);
        entity.CheckOnGround();
        entity.Teleported();

        return true;
    }

    private static void TranslateTeleportInterpolation(Entity entity, Player? player, in Vec3D oldPos, double oldAngle)
    {
        // Teleport line needs to translate interpolation values so the teleport is as seamless as possible.
        Vec2D diffPos2D = oldPos.XY - entity.PrevPosition.XY;
        diffPos2D = diffPos2D.Rotate(entity.AngleRadians - oldAngle);
        entity.PrevPosition.X = entity.Position.X - diffPos2D.X;
        entity.PrevPosition.Y = entity.Position.Y - diffPos2D.Y;
        entity.PrevPosition.Z = entity.Position.Z - (oldPos.Z - entity.PrevPosition.Z);

        if (player != null)
        {
            double diffAngle = oldAngle - player.PrevAngle;
            player.PrevAngle = entity.AngleRadians - diffAngle;
        }
    }

    private static bool CanTeleport(Entity teleportEntity, in Vec3D pos)
    {
        if (teleportEntity.IsPlayer)
            return true;

        if (teleportEntity.World.MapInfo.HasOption(MapOptions.AllowMonsterTelefrags))
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
                if (line.Id == sourceLine.Id || !line.TwoSided)
                    continue;

                double lineAngle = line.StartPosition.Angle(line.EndPosition) - sourceLine.StartPosition.Angle(sourceLine.EndPosition);
                if (!m_teleportLineReverse)
                    lineAngle += MathHelper.Pi;

                angle = lineAngle + teleportEntity.AngleRadians;

                // Exit position is proportional to the position on the source teleport line
                double time = sourceLine.Segment.ToTime(teleportEntity.Position.XY);
                Vec2D destLinePos = line.Segment.FromTime(1.0 - time);

                Vec2D sourcePos = sourceLine.Segment.FromTime(time);
                double distance = teleportEntity.Position.XY.Distance(sourcePos);
                double distanceAngle = sourcePos.Angle(teleportEntity.Position.XY);

                // The entity crossed the line, translate the distance from the source line to the exit line
                Vec2D unit = Vec2D.UnitCircle(lineAngle + distanceAngle);
                destLinePos += unit * distance;
                pos = destLinePos.To3D(GetTeleportLineZ(teleportEntity, line, destLinePos, out _));
                GetTeleportLineZ(teleportEntity, sourceLine, teleportEntity.Position.XY, out offsetZ);
                return true;
            }
        }
        if (m_tid == EntityManager.NoTid)
        {
            foreach (Sector sector in m_world.FindBySectorTag(m_tag))
            {
                LinkableNode<Entity>? node = sector.Entities.Head;
                while (node != null)
                {
                    Entity entity = node.Value;
                    node = node.Next;
                    if (entity.Flags.IsTeleportSpot)
                    {
                        pos = GetTeleportPosition(entity);
                        angle = entity.AngleRadians;
                        return true;
                    }
                }
            }
        }
        else if (m_tag == Sector.NoTag)
        {
            foreach (Entity entity in m_world.FindByTid(m_tid))
                if (entity.Flags.IsTeleportSpot)
                {
                    pos = GetTeleportPosition(entity);
                    angle = entity.AngleRadians;
                    return true;
                }
        }
        else
        {
            foreach (Sector sector in m_world.FindBySectorTag(m_tag))
            {
                LinkableNode<Entity>? node = sector.Entities.Head;
                while (node != null)
                {
                    Entity entity = node.Value;
                    node = node.Next;
                    if (entity.ThingId == m_tid && entity.Flags.IsTeleportSpot)
                    {
                        pos = entity.Position;
                        angle = entity.AngleRadians;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static Vec3D GetTeleportPosition(Entity entity)
    {
        // Teleport landings had no blockmap flag which means they didn't move
        // Doom used the sector floor z here
        if (entity.Definition.EditorId == (int)EditorId.TeleportLanding)
            return entity.Position.XY.To3D(entity.Sector.ToFloorZ(entity.Position));

        return entity.Position;
    }

    private static double GetTeleportLineZ(Entity teleportEntity, Line line, in Vec2D pos, out double offsetZ)
    {
        // This may not be the correct Z position but get the most valid position available.
        // Teleport will check once the entity is teleported that the new offset is equal to the current.
        double floorZ;
        if (line.Back != null)
            floorZ = Math.Max(line.Front.Sector.ToFloorZ(pos), line.Back.Sector.ToFloorZ(pos));
        else
            floorZ = line.Front.Sector.ToFloorZ(pos);

        offsetZ = teleportEntity.Position.Z - floorZ;
        return floorZ;
    }
}
