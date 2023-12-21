using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Models;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Sectors;
using System;

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
    private readonly Func<Entity, GridIterationStatus> m_pushEntityAction;

    public bool OverrideEquals => true;

    public PushSpecial(PushType type, IWorld world, Sector sector, Vec2D pushFactor, Entity? pusher)
    {
        m_type = type;
        m_world = world;
        m_sector = sector;
        m_pushFactor = pushFactor.To3D(0);
        m_magnitude = pushFactor.Length();
        m_pusher = pusher;
        m_pushEntityAction = new(PushEntity);
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
        if (m_sector.SectorEffect != SectorEffect.WindOrPush)
            return SpecialTickStatus.Continue;

        // TODO logic changes when Transfer Heights is implemented
        if (m_type == PushType.Push && m_pusher != null)
        {
            Box2D box = new(m_pusher.Position.XY, m_magnitude * 2);
            m_world.BlockmapTraverser.EntityTraverse(box, m_pushEntityAction);
        }
        else
        {
            LinkableNode<Entity>? node = m_sector.Entities.Head;
            while (node != null)
            {
                Entity entity = node.Value;
                node = node.Next;

                if (ShouldNotPush(entity))
                    continue;

                Vec3D pushFactor = m_pushFactor;
                if (m_type == PushType.Wind)
                {
                    if (!entity.Flags.WindThrust)
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

    private GridIterationStatus PushEntity(Entity entity)
    {
        if (entity.Flags.NoClip)
            return GridIterationStatus.Continue;

        if (!entity.IsBoomSentient && !entity.Flags.Shootable)
            return GridIterationStatus.Continue;

        if (!m_world.CheckLineOfSight(entity, m_pusher))
            return GridIterationStatus.Continue;

        double distance = entity.Position.ApproximateDistance2D(m_pusher.Position);
        Vec2D diff = entity.Position.XY - m_pusher.Position.XY;
        double speed = (m_magnitude * 128) / (diff.X * diff.X + diff.Y * diff.Y + 1);

        if (speed <= 0)
            return GridIterationStatus.Continue;

        double angle = entity.Position.Angle(m_pusher.Position);
        if (m_pusher.Definition.EditorId == (int)EditorId.PointPusher)
            angle += Math.PI;

        entity.Velocity += Vec3D.UnitSphere(angle, 0) * speed;
        entity.Flags.IgnoreDropOff = true;
        return GridIterationStatus.Continue;
    }

    private static bool ShouldNotPush(Entity entity) => !entity.IsPlayer || entity.Flags.NoClip || entity.Flags.NoGravity;

    public bool Use(Entity entity) => false;

    public override bool Equals(object? obj)
    {
        if (obj is not PushSpecial push)
            return false;

        bool pusherEquals;
        if (push.m_pusher == null)
            pusherEquals = m_pusher == null;
        else
            pusherEquals = m_pusher != null && push.m_pusher.Id == m_pusher.Id;

        return pusherEquals &&
            push.m_type == m_type &&
            push.m_sector.Id == m_sector.Id &&
            push.m_pushFactor == m_pushFactor &&
            push.m_magnitude == m_magnitude;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
