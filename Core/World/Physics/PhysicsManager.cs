using System;
using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities;
using static Helion.Util.Assertion.Assert;

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

        public void LinkToWorld(Entity entity)
        {
            m_blockmap.Link(entity);
            LinkToSectors(entity);
            ClampBetweenFloorAndCeiling(entity);
        }

        public void Move(Entity entity)
        {
            entity.UnlinkFromWorld();

            MoveXY(entity);
            MoveZ(entity);

            LinkToWorld(entity);
        }

        private void LinkToSectors(Entity entity)
        {
            Precondition(entity.SectorNodes.Count == 0, "Forgot to unlink entity from blockmap");
            
            // TODO: We (very likely) do a fair amount of object creation here.
            //       Let's use `stackalloc` for an array in the future and do
            //       direct comparison via iteration. It's probably the very
            //       few examples where O(n) beats O(1) due to how small n is.
            //       Plus we also do a foreach over the hash set, which has
            //       performance issues we can resolve as well by fixing this.
            Sector centerSector = m_bspTree.ToSector(entity.Position);
            HashSet<Sector> sectors = new HashSet<Sector> { centerSector };
            
            Box2D box = entity.Box.To2D(); 
            m_blockmap.Iterate(box, EntitySectorOverlapFinder);
            
            PerformSectorLinkingAndBoundDiscovery(entity, sectors, centerSector);

            GridIterationStatus EntitySectorOverlapFinder(Block block)
            {
                // Doing iteration over enumeration for performance reasons.
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.Segment.Intersects(box))
                    {
                        sectors.Add(line.Front.Sector);
                        if (line.Back != null)
                            sectors.Add(line.Back.Sector);
                    } 
                }
                
                return GridIterationStatus.Continue;
            }
        }

        private void PerformSectorLinkingAndBoundDiscovery(Entity entity, HashSet<Sector> sectors, Sector centerSector)
        {
            double highestFloorZ = double.MinValue;
            double lowestCeilZ = double.MaxValue;
            Sector highestFloor = centerSector;
            Sector lowestCeiling = centerSector;
            
            foreach (Sector sector in sectors)
            {
                LinkableNode<Entity> node = sector.Link(entity);
                entity.SectorNodes.Add(node);
                
                double floorZ = sector.Floor.Plane.ToZ(entity.Position);
                if (floorZ > highestFloorZ)
                {
                    highestFloor = sector;
                    highestFloorZ = floorZ;
                }
                
                double ceilZ = sector.Ceiling.Plane.ToZ(entity.Position);
                if (ceilZ < lowestCeilZ)
                {
                    lowestCeiling = sector;
                    lowestCeilZ = ceilZ;
                }
            }

            entity.HighestFloorSector = highestFloor;
            entity.LowestCeilingSector = lowestCeiling;
        }

        private void MoveXY(Entity entity)
        {
            if (entity.Velocity.To2D() == Vec2D.Zero)
                return;

            Vec2D nextPos = entity.Position.To2D() + entity.Velocity.To2D();
            // TODO: Try to move to nextPos.
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

        private void MoveZ(Entity entity)
        {
            if (!entity.OnGround)
                entity.Velocity.Z -= Gravity;
            
            entity.SetZ(entity.Position.Z + entity.Velocity.Z);
        }
    }
}