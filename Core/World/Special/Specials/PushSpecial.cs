using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics.Blockmap;
using System;
using System.Collections.Generic;

namespace Helion.World.Special.Specials;

public enum PushType
{
    Push,
    Wind,
    Current
}

public class PushSpecial : ISpecial
{
    private const double PushFactor = 1.0 / 128;
    private const double WindFactor = 1.0 / 256;

    private readonly PushType m_type;
    private readonly IWorld m_world;
    private readonly Sector m_sector;
    private readonly Vec3D m_pushFactor;
    private readonly double m_magnitude;
    private readonly Entity? m_pusher;

    public PushSpecial(PushType type, IWorld world, Sector sector, Vec2D pushFactor, Entity? pusher)
    {
        m_type = type;
        m_world = world;
        m_sector = sector;
        m_pushFactor = pushFactor.To3D(0);
        m_magnitude = GetMagnitude(m_pushFactor);
        m_pusher = pusher;
    }

    public ISpecialModel ToSpecialModel()
    {
        return new PushSpecialModel()
        {
            Type = (int)m_type,
            SectorId = m_sector.Id,
            PushX = m_pushFactor.X,
            PushY = m_pushFactor.Y,
            Magnitude = m_magnitude,
            PusherEntityId = m_pusher?.Id
        };
    }

    public SpecialTickStatus Tick()
    {
        if (m_type == PushType.Push && m_pusher != null)
        {
            Box2D box = new(m_pusher.Position.XY, m_magnitude * 2);
            var intersections = m_world.BlockmapTraverser.GetBlockmapIntersections(box, BlockmapTraverseFlags.Entities,
                BlockmapTraverseEntityFlags.NotNoGravity | BlockmapTraverseEntityFlags.NotNoClip);
            PushEntities(intersections);
            DataCache.Instance.FreeBlockmapIntersectList(intersections);
        }
        else
        {
            foreach (var entity in m_sector.Entities)
            {
                Vec3D pushFactor = m_pushFactor;
                if (m_type == PushType.Wind)
                {
                    if (entity.Flags.NoBlockmap || entity.Flags.NoClip || entity.Flags.NoGravity)
                        continue;

                    if (!entity.IsPlayer && !entity.Flags.WindThrust)
                        continue;

                    if (entity.Position.Z != m_sector.ToFloorZ(entity.Position))
                        pushFactor /= 2;
                }
                else if (m_type == PushType.Current)
                {
                    if (entity.Position.Z != m_sector.ToFloorZ(entity.Position))
                        continue;
                }

                entity.Velocity += pushFactor * PushFactor;
            }
        }

        return SpecialTickStatus.Continue;
    }

    private void PushEntities(List<BlockmapIntersect> intersections)
    {
        if (m_pusher == null)
            return;

        for (int i = 0; i < intersections.Count; i++)
        {
            BlockmapIntersect bi = intersections[i];
            if (bi.Entity == null || !bi.Entity.IsPlayer || !m_world.CheckLineOfSight(bi.Entity, m_pusher))
                continue;

            double distance = bi.Entity.Position.XY.Distance(m_pusher.Position.XY);
            double speed = (m_magnitude - (distance / 2)) * WindFactor;

            if (speed <= 0)
                continue;

            double angle = bi.Entity.Position.Angle(m_pusher.Position);
            if (m_pusher.Definition.EditorId == (int)EditorId.PointPusher)
                angle += Math.PI;

            bi.Entity.Velocity += Vec3D.UnitSphere(angle, 0) * speed;
        }
    }

    private static double GetMagnitude(Vec3D pushFactor)
    {
        double dx = Math.Abs(pushFactor.X);
        double dy = Math.Abs(pushFactor.Y);
        if (dx < dy)
            return dx + dy - (dx / 2);
        return dx + dy - (dy / 2);
    }

    public bool Use(Entity entity) => false;
}
