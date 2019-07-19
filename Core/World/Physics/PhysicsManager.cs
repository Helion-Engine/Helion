using Helion.Util.Geometry;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities;

namespace Helion.World.Physics
{
    public class PhysicsManager
    {
        private const double Gravity = -1.0;
        
        private readonly WorldBase m_world;
        private readonly BspTree m_bspTree;
        private readonly Blockmap m_blockmap;

        public PhysicsManager(WorldBase world, BspTree bspTree, Blockmap blockmap)
        {
            m_world = world;
            m_bspTree = bspTree;
            m_blockmap = blockmap;
        }

        public void Link(Entity entity)
        {
            // TODO: Add to sector list.
            ClampBetweenFloorAndCeiling(entity);
        }

        private void ClampBetweenFloorAndCeiling(Entity entity)
        {
            // TODO: This should use the actual plane with these values.
            double lowestCeil = entity.Sector.Ceiling.Plane.ToZ(entity.Position);
            double highestFloor = entity.Sector.Floor.Plane.ToZ(entity.Position);

            if (entity.Box.Top > lowestCeil)
                entity.SetZ(lowestCeil - entity.Height);
            if (entity.Box.Bottom < highestFloor)
                entity.SetZ(highestFloor);
        }

        public void Move(Entity entity)
        {
            entity.UnlinkFromWorld();

            Vec2D velXY = entity.Velocity.To2D();
            if (velXY.X != 0 || velXY.Y != 0)
                MoveXY(entity, velXY);

            // TODO: Don't do any of this if we're on the ground.
            double velZ = entity.Velocity.Z + Gravity;
            MoveZ(entity, velZ);

            Link(entity);
        }

        private void MoveXY(Entity entity, Vec2D velXY)
        {
            // TODO
        }

        private void MoveZ(Entity entity, double velZ)
        {
            entity.SetZ(entity.Position.Z + velZ);
        }
    }
}