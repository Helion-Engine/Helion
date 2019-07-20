using System;
using Helion.Util.Geometry;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities;

namespace Helion.World.Physics
{
    public class PhysicsManager
    {
        private const int MaxSlides = 3;
        private const double Gravity = 1.0;
        private const double Friction = 0.90625;
        private const double StepHeight = 24.0;
        private const double SlideStepBack = 1.0 / 32.0;
        private const double MinMovementThreshold = 0.06;
        
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

        public void Move(Entity entity)
        {
            entity.UnlinkFromWorld();

            if (HasHorizontalVelocity(entity))
                AttemptMoveXY(entity);
            AttemptMoveZ(entity);

            Link(entity);
        }

        private void AttemptMoveXY(Entity entity)
        {
            Vec2D nextPos = entity.Position.To2D() + entity.Velocity.To2D();
            entity.SetXY(nextPos);

            entity.Velocity.X *= Friction;
            entity.Velocity.Y *= Friction;

            if (Math.Abs(entity.Velocity.X) < MinMovementThreshold)
                entity.Velocity.X = 0;
            if (Math.Abs(entity.Velocity.Y) < MinMovementThreshold)
                entity.Velocity.Y = 0;
        }

        private void ClampBetweenFloorAndCeiling(Entity entity)
        {
            double lowestCeil = entity.LowestCeilingSector.Ceiling.Plane.ToZ(entity.Position);
            double highestFloor = entity.HighestFloorSector.Floor.Plane.ToZ(entity.Position);

            if (entity.Box.Top + entity.Height > lowestCeil)
            {
                entity.SetZ(lowestCeil - entity.Height);
                entity.Velocity.Z = 0;
            }

            if (entity.Box.Bottom <= highestFloor)
            {
                entity.SetZ(highestFloor);
                entity.Velocity.Z = 0;
                entity.OnGround = true;
            }
            else
                entity.OnGround = false;
        }

        private void AttemptMoveZ(Entity entity)
        {
            if (!entity.OnGround)
                entity.Velocity.Z -= Gravity;
            
            entity.SetZ(entity.Position.Z + entity.Velocity.Z);
        }

        private static bool HasHorizontalVelocity(Entity entity) => entity.Velocity.To2D() != Vec2D.Zero;
    }
}