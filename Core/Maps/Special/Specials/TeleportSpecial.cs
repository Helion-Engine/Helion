using System;
using System.Linq;
using Helion.Util.Geometry;
using Helion.World.Entities;
using Helion.World.Physics;

namespace Helion.Maps.Special.Specials
{
    public class TeleportSpecial : ISpecial
    {
        private const int TeleportFreezeTicks = 18;

        private EntityActivateSpecialEventArgs m_args;
        private PhysicsManager m_physicsManager;
        private IMap m_map;

        public TeleportSpecial(EntityActivateSpecialEventArgs args, PhysicsManager physicsManager, IMap map)
        {
            m_args = args;
            m_physicsManager = physicsManager;
            m_map = map;
        }

        public SpecialTickStatus Tick()
        {
            Vec2D position;
            Entity entity = m_args.Entity;

            if (GetTeleportPosition(out position))
            {
                entity.UnlinkFromWorld();
                entity.FrozenTics = TeleportFreezeTicks;
                entity.Velocity = Vec3D.Zero;
                entity.SetXY(position);
                if (entity.Player != null)
                    entity.Player.Pitch = 0;
                m_physicsManager.LinkToWorld(entity);
                entity.SetZ(entity.HighestFloorSector.Floor.Plane.ToZ(entity.Position));
                entity.OnGround = true;
            }

            return SpecialTickStatus.Destroy;
        }

        private bool GetTeleportPosition(out Vec2D position)
        {
            var sector = m_map.Sectors.FirstOrDefault(x => x.Tag == m_args.ActivateLineSpecial.SectorTag);

            if (sector != null)
            {
                var max_x1 = sector.Lines.Max(x => x.Segment.Start.X);
                var max_x2 = sector.Lines.Max(x => x.Segment.End.X);
                var max_y1 = sector.Lines.Max(x => x.Segment.Start.Y);
                var max_y2 = sector.Lines.Max(x => x.Segment.End.Y);
                var min_x1 = sector.Lines.Min(x => x.Segment.Start.X);
                var min_x2 = sector.Lines.Min(x => x.Segment.End.X);
                var min_y1 = sector.Lines.Min(x => x.Segment.Start.Y);
                var min_y2 = sector.Lines.Min(x => x.Segment.End.Y);

                max_x1 = Math.Max(max_x1, max_x2);
                max_y1 = Math.Max(max_y1, max_y2);
                min_x1 = Math.Min(min_x1, min_x2);
                min_y1 = Math.Min(min_y1, min_y2);

                position = (new Seg2D(new Vec2D(min_x1, min_y1), new Vec2D(max_x1, max_y1))).FromTime(0.5);
                return true;
            }

            position = default;
            return false;
        }
    }
}
