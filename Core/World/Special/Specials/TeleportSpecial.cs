using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace Helion.World.Special.Specials;

[Flags]
public enum TeleportFog
{
    None = 0,
    Source = 1,
    Dest = 2
}

public enum TeleportOptions
{
    None = 0,
    KeepHeight = 1,
    KeepMomentum = 2,
}

public struct TeleportSpecial
{
    public const int TeleportFreezeTicks = 18;

    private readonly Entity m_entity;
    private readonly Line? m_sourceLine;
    private readonly IWorld m_world;
    private readonly int m_tid;
    private readonly int m_tag;
    private readonly int m_lineId;
    private readonly bool m_teleportLineReverse;
    private readonly TeleportFog m_fogFlags;
    private readonly TeleportType m_type;
    private readonly TeleportOptions m_options;

    // list used to avoid re-allocations in teleport code
    private static readonly List<Entity> randomSpotList = new();

    public static TeleportFog GetTeleportFog(Line line)
    {
        switch (line.Special.LineSpecialType)
        {
            case ZDoomLineSpecialType.Teleport:
            case ZDoomLineSpecialType.TeleportNoStop:
                if (line.Args.Arg2 == 0)
                    return TeleportFog.Source | TeleportFog.Dest;
                else
                    return TeleportFog.Dest;
        }

        return TeleportFog.None;
    }

    public TeleportSpecial(Entity entity, Line? sourceLine, IWorld world, int tid, int tag, TeleportFog flags,
        TeleportType type = TeleportType.Doom, TeleportOptions options = TeleportOptions.None)
    {
        m_entity = entity;
        m_sourceLine = sourceLine;
        m_world = world;
        m_tid = tid;
        m_tag = tag;
        if (m_tid != EntityManager.NoTid && m_tag == Sector.NoTag)
            m_tag = -1;
        m_fogFlags = flags;
        m_type = type;
        m_options = options;
    }

    public TeleportSpecial(Entity entity, Line sourceLine, IWorld world, int lineId, TeleportFog flags,
        TeleportType type = TeleportType.Doom, bool reverseLine = false)
    {
        m_entity = entity;
        m_sourceLine = sourceLine;
        m_world = world;
        m_tag = -1;
        m_lineId = lineId;
        m_teleportLineReverse = reverseLine;
        m_fogFlags = flags;
        m_type = type;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Teleport(Entity teleportSpot)
    {
        return TeleportInternal(teleportSpot);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Teleport()
    {
        return TeleportInternal(null);
    }

    private readonly bool TeleportInternal(Entity? teleportSpot)
    {
        Vec3D pos;
        double angle;
        double offsetZ = 0;
        if (teleportSpot != null)
        {
            angle = teleportSpot.AngleRadians;
            pos = GetTeleportPosition(teleportSpot);
        }
        else
        {
            if (!FindTeleportSpot(m_entity, out pos, out angle, out offsetZ))
                return false;
        }

        if (WorldStatic.FinalDoomTeleport)
            pos.Z = m_entity.Position.Z;

        if ((m_options & TeleportOptions.KeepHeight) != 0)
            offsetZ = m_entity.Position.Z - m_entity.Sector.Floor.Z;

        var isMonsterCloset = (m_entity.ClosetFlags & ClosetFlags.MonsterCloset) != 0;
        var oldPosition = m_entity.Position;
        if (Teleport(m_entity, pos, angle, offsetZ))
        {
            if (!isMonsterCloset && (m_fogFlags & TeleportFog.Source) != 0)
                m_world.CreateTeleportFog(oldPosition);

            if ((m_fogFlags & TeleportFog.Dest) != 0)
                m_world.CreateTeleportFog(m_entity);

            return true;
        }

        return false;
    }

    private readonly bool Teleport(Entity entity, Vec3D pos, double teleportAngle, double offsetZ)
    {
        pos.Z += offsetZ;
        if (!CanTeleport(entity, pos))
            return false;

        entity.Flags.SetTeleported();

        var oldAngle = entity.AngleRadians;
        var oldPos = entity.Position;
        var player = entity.PlayerObj;
        entity.UnlinkFromWorld();
        entity.Position = pos;

        if (m_type == TeleportType.Doom)
        {
            // Adding 1 to account for the decrement being handled in base entity class. Doom would do this only for players and because player logic ran first it would be one behind.
            if (entity.IsPlayer && (m_options & TeleportOptions.KeepMomentum) == 0)
            {
                entity.FrozenTics = TeleportFreezeTicks + 1;
                entity.Velocity = Vec3D.Zero;
            }

            entity.AngleRadians = teleportAngle;
            player?.PitchRadians = 0;
        }
        else if (m_type == TeleportType.BoomCompat || m_type == TeleportType.BoomFixed)
        {
            var sourceLine = m_sourceLine;

            // Only use these calculations for Teleporting to a sector with teleport thing. For line teleport using the angle given.
            if (m_lineId == Line.NoLineId && sourceLine != null)
            {
                if (m_type == TeleportType.BoomFixed)
                    entity.AngleRadians = teleportAngle + entity.AngleRadians - sourceLine.Segment.Start.Angle(sourceLine.Segment.End) - MathHelper.HalfPi;
                else
                    entity.AngleRadians += sourceLine.Segment.Start.Angle(sourceLine.Segment.End) - teleportAngle + MathHelper.HalfPi;
            }
            else
            {
                entity.AngleRadians = teleportAngle;
            }

            var velocity = entity.Velocity.XY.Rotate(entity.AngleRadians - oldAngle);
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
        m_world.EntityTeleported(entity);

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
        if (teleportEntity.Flags.Teleported())
            return false;

        if (teleportEntity.IsPlayer)
            return true;

        if (WorldStatic.World.MapInfo.HasOption(MapOptions.AllowMonsterTelefrags))
            return true;

        return WorldStatic.World.BlockmapTraverser.SolidBlockTraverse(teleportEntity, pos,
            !WorldStatic.InfinitelyTallThings && !WorldStatic.FinalDoomTeleport);
    }

    private readonly bool FindTeleportSpot(Entity teleportEntity, out Vec3D pos, out double angle, out double offsetZ)
    {
        pos = Vec3D.Zero;
        angle = 0;
        offsetZ = 0;

        if (m_tid == EntityManager.NoTid && m_tag == -1 && m_lineId == Line.NoLineId)
            return false;

        if (m_lineId != Line.NoLineId && m_sourceLine != null)
        {
            foreach (Line line in m_world.FindByLineId(m_lineId))
            {
                if (line.Id == m_sourceLine.Id || line.Back == null)
                    continue;

                var lineAngle = line.GetAngle() - m_sourceLine.GetAngle();
                if (!m_teleportLineReverse)
                    lineAngle += MathHelper.Pi;

                angle = lineAngle + teleportEntity.AngleRadians;

                var teleportEntityPos = teleportEntity.Position.XY;
                // Exit position is proportional to the position on the source teleport line
                var time = m_sourceLine.Segment.ToTime(teleportEntityPos);
                var destTime = m_teleportLineReverse ? time : 1.0 - time;
                var destLinePos = line.Segment.FromTime(destTime);
                var destZ = GetTeleportLineZ(teleportEntity, line, destLinePos, out _);

                // This makes the teleport lines not feel jittery for players. Only do for real players and validate the final position.
                // Closets can rely on this exact behavior (Remanence MAP01)
                if (teleportEntity.PlayerObj != null && !teleportEntity.PlayerObj.IsVooDooDoll)
                {
                    var sourcePos = m_sourceLine.Segment.FromTime(time);
                    var distance = teleportEntityPos.Distance(sourcePos);
                    var distanceAngle = sourcePos.Angle(teleportEntityPos);
                    var unit = Vec2D.UnitCircle(lineAngle + distanceAngle);
                    var testDestPos =  destLinePos + unit * distance;

                    var saveZ = teleportEntity.Position.Z;
                    teleportEntity.Position.Z = destZ;
                    m_world.PhysicsManager.IsPositionValid(teleportEntity, testDestPos.X, testDestPos.Y);
                    if (teleportEntity.BlockingBlockLineIndex == -1 && teleportEntity.BlockingEntity == null)
                        destLinePos = testDestPos;
                    teleportEntity.Position.Z = saveZ;
                }

                GetTeleportLineZ(teleportEntity, m_sourceLine, teleportEntityPos, out offsetZ);
                pos = destLinePos.To3D(destZ);
                return true;
            }
        }

        if (m_tid == EntityManager.NoTid)
        {
            var teleportNode = m_world.EntityManager.TeleportSpots.First;
            while (teleportNode != null)
            {
                if (teleportNode.Value.Sector.Tag == m_tag)
                {
                    Entity entity = teleportNode.Value;
                    pos = GetTeleportPosition(entity);
                    angle = entity.AngleRadians;
                    return true;
                }
                teleportNode = teleportNode.Next;
            }
        }
        else
        {
            // the intended use case of the TID-based specials is to randomly pick a Teleport Target
            // thing with the right TID, but there's some edge case handling for old WADs inherited from ZDoom
            randomSpotList.Clear();
            foreach (Entity entity in m_world.FindByTid(m_tid))
            {
                if ((m_tag == -1 || entity.Sector.Tag == m_tag) && entity.Flags.IsTeleportSpot())
                {
                    randomSpotList.Add(entity);
                }
            }

            if (randomSpotList.Count > 0)
            {
                int choice = m_world.Random.NextByte() % randomSpotList.Count;
                var entity = randomSpotList[choice];
                // don't hold onto references to the entities for too long
                randomSpotList.Clear();
                pos = GetTeleportPosition(entity);
                angle = entity.AngleRadians;
                return true;
            }
            else if (m_tag == -1) // compatibility edge cases used in both ZDoom and DSDA-Doom
            {
                // teleport to first map spot (e.g. Hexen MAP10)
                foreach (Entity entity in m_world.FindByTid(m_tid))
                {
                    if (entity.Definition.Type == EntityType.MapSpot)
                    {
                        pos = GetTeleportPosition(entity);
                        angle = entity.AngleRadians;
                        return true;
                    }
                }

                // if even that failed, teleport to first non-solid thing (e.g. Caldera MAP13)
                foreach (Entity entity in m_world.FindByTid(m_tid))
                {
                    if (!entity.Flags.Solid())
                    {
                        pos = GetTeleportPosition(entity);
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
