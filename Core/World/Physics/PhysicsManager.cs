using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Maps.Special;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.World.Blockmaps;
using Helion.World.Bsp;
using Helion.World.Entities;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Physics
{
    /// <summary>
    /// Responsible for handling all the physics and collision detection in a
    /// world.
    /// </summary>
    public class PhysicsManager
    {
        private const int MaxSlides = 3;
        private const int JumpDelayTicks = 7;
        private const double Gravity = 1.0;
        private const double Friction = 0.90625;
        private const double SlideStepBackTime = 1.0 / 32.0;
        private const double MinMovementThreshold = 0.06;
        private const double EntityUseDistance = 64.0;

        // TODO actually verify this value
        private const double SetEntityToFloorSpeedMax = 5;

        private readonly BspTree m_bspTree;
        private readonly Blockmap m_blockmap;
        private EntityActivateSpecialEventArgs m_entityActivatedSpecialArgs = new EntityActivateSpecialEventArgs();

        /// <summary>
        /// Fires when an entity activates a line special with use or by crossing a line.
        /// </summary>
        public event EventHandler<EntityActivateSpecialEventArgs> EntityActivatedSpecial;

        /// <summary>
        /// Fires when the player executes the use command but hits a non-special blocking line.
        /// </summary>
        public event EventHandler<Entity> PlayerUseFail;

        /// <summary>
        /// Creates a new physics manager which utilizes the arguments for any
        /// collision detection or linking to the world.
        /// </summary>
        /// <param name="bspTree">The BSP tree for the world.</param>
        /// <param name="blockmap">The blockmap for the world.</param>
        public PhysicsManager(BspTree bspTree, Blockmap blockmap)
        {
            m_bspTree = bspTree;
            m_blockmap = blockmap;
        }

        /// <summary>
        /// Links an entity to the world.
        /// </summary>
        /// <param name="entity">The entity to link.</param>
        public void LinkToWorld(Entity entity)
        {
            m_blockmap.Link(entity);
            LinkToSectors(entity);
            ClampBetweenFloorAndCeiling(entity);
        }

        /// <summary>
        /// Performs all the movement logic on the entity.
        /// </summary>
        /// <param name="entity">The entity to move.</param>
        public void Move(Entity entity)
        {
            MoveXY(entity);
            MoveZ(entity);
        }

        public SectorMoveStatus MoveSectorZ(Sector sector, SectorFlat flat, SectorMoveType moveType, MoveDirection direction, double speed, double destZ, CrushData? crush)
        {
            // Save the Z value because we are only checking if the dest is valid
            // If the move is invalid because of a blocking entity then it will not be set to destZ
            // TODO handle plane Z
            double startZ = flat.Z;
            double thingZ;
            SectorMoveStatus status = SectorMoveStatus.Success;
            flat.Z = destZ;
            flat.Plane.MoveZ(destZ - startZ);

            if (sector.Ceiling.Z < sector.Floor.Z)
            {
                flat.Z = startZ;
                flat.Plane.MoveZ(startZ - destZ);
                return SectorMoveStatus.CeilingHitFloor;
            }

            foreach (var entity in sector.Entities)
            {
                // At slower speeds we need to set entities to the floor
                // Otherwise the player will fall and hit the floor repeatedly creating a weird bouncing effect
                if (moveType == SectorMoveType.Floor && direction == MoveDirection.Down && -speed < SetEntityToFloorSpeedMax &&
                    entity.OnGround && !entity.IsFlying && entity.HighestFloorSector == sector)
                {
                    entity.SetZ(destZ);
                }

                entity.UnlinkFromWorld();
                LinkToWorld(entity);

                thingZ = entity.OnGround ? sector.Floor.Plane.ToZ(entity.Position.To2D()) : entity.Position.Z;

                if (thingZ + entity.Height > entity.LowestCeilingSector.Ceiling.Z)
                {
                    if (crush != null)
                    {              
                        status = SectorMoveStatus.Crush;
                        if (CrusherShouldContinue(status, crush))
                            continue;
                    }

                    flat.Z = startZ;
                    flat.Plane.MoveZ(startZ - destZ);

                    // Entity blocked movement, reset all entities in moving sector after restting sector Z
                    foreach (var relinkEntity in sector.Entities)
                    {
                        relinkEntity.UnlinkFromWorld();
                        LinkToWorld(relinkEntity);
                    }

                    return SectorMoveStatus.Blocked;
                }
            }

            // For crushing ceilings we continue to move, but still want to return SectorMoveStatus.Crush
            if (status == SectorMoveStatus.Success || CrusherShouldContinue(status, crush))
                flat.PrevZ = startZ;

            return status;
        }

        private static bool CrusherShouldContinue(SectorMoveStatus status, CrushData? crush) => crush != null && status == SectorMoveStatus.Crush && crush.CrushMode == ZCrushMode.DoomWithSlowDown;

        /// <summary>
        /// Executes use logic on the entity. 
        /// EntityUseActivated event will fire if the entity activates a line special or is in range to hit a blocking line.
        /// PlayerUseFail will fire if the entity is a player and we hit a block line but didn't activate a special.
        /// </summary>
        /// <remarks>
        /// If the line has a special and we are hitting the front then we can use it (player Z does not apply here).
        /// If there is a LineOpening with OpeningHeight <= 0, it's a closed sector. The special line behind it cannot activate until the sector has an opening.
        /// </remarks>
        /// <param name="entity">The entity to execute use.</param>
        public void EntityUse(Entity entity)
        {
            Line? activateLine = null;
            Line? currentActivateLine = null;
            bool hitBlockLine = false;
            double closetDist = double.MaxValue;
            double currentClosestDist;

            Vec2D start = entity.Position.To2D();
            Vec2D end = new Vec2D(start.X + (Math.Cos(entity.Angle) * EntityUseDistance), start.Y + (Math.Sin(entity.Angle) * EntityUseDistance));
            Seg2D useSeg = new Seg2D(start, end);
            m_blockmap.Iterate(useSeg, TraceLineFinder);

            GridIterationStatus TraceLineFinder(Block block)
            {
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.Segment.Intersects(useSeg))
                    {
                        if (line.HasSpecial && line.Special.CanActivate(entity, line.Flags, ActivationContext.UseLine) && line.Segment.OnRight(start))
                            currentActivateLine = line;

                        bool isBlockingLine = line.OneSided;
                        bool canActivateThrough = !line.OneSided;

                        if (!isBlockingLine && line.Back != null)
                        {
                            LineOpening opening = new LineOpening(line.Front.Sector, line.Back.Sector);
                            isBlockingLine = !opening.CanPassOrStepThrough(entity);
                            canActivateThrough = opening.OpeningHeight > 0;
                        }

                        // Only check BlocksEntity here so if we fail to activate special the PlayerUseFail event is raised for two-sided impassible lines
                        if (isBlockingLine || line.BlocksEntity(entity))
                            hitBlockLine = true;

                        if (!canActivateThrough || line.HasSpecial)
                        {
                            currentClosestDist = line.Segment.ClosestDistance(start);
                            if (currentClosestDist < closetDist)
                            {
                                activateLine = currentActivateLine;
                                closetDist = currentClosestDist;
                            }
                        }
                    }
                }

                return GridIterationStatus.Continue;
            }

            if (activateLine != null)
            {
                // The use line was blocked by a blocking line
                if (activateLine.Segment.ClosestDistance(start) != closetDist)
                    activateLine = null;
            }

            if (activateLine != null)
            {
                m_entityActivatedSpecialArgs.ActivationContext = ActivationContext.UseLine;
                m_entityActivatedSpecialArgs.Entity = entity;
                m_entityActivatedSpecialArgs.ActivateLineSpecial = activateLine;
                EntityActivatedSpecial?.Invoke(this, m_entityActivatedSpecialArgs);
            }
            else if (hitBlockLine && entity.Player != null)
            {
                PlayerUseFail?.Invoke(this, entity);
            }
        }

        private static int CalculateSteps(Vec2D velocity, double radius)
        {
            Invariant(radius > 0.5, $"Actor radius too small for safe XY physics movement");

            // We want to pick some atomic distance to keep moving our bounding
            // box. It can't be bigger than the radius because we could end up
            // skipping over a line.
            double moveDistance = radius - 0.5;
            double biggerAxis = Math.Max(Math.Abs(velocity.X), Math.Abs(velocity.Y));
            return (int)(biggerAxis / moveDistance) + 1;
        }

        private static void ApplyFriction(Entity entity)
        {
            if (!entity.OnGround && !entity.IsFlying)
                return;

            entity.Velocity.X *= Friction;
            entity.Velocity.Y *= Friction;
        }
        
        private static void StopXYMovementIfSmall(Entity entity)
        {
            if (Math.Abs(entity.Velocity.X) < MinMovementThreshold)
                entity.Velocity.X = 0;
            if (Math.Abs(entity.Velocity.Y) < MinMovementThreshold)
                entity.Velocity.Y = 0;
        }
        
        private static bool LineBlocksEntity(Entity entity, Line line)
        {
            if (line.BlocksEntity(entity))
                return true;

            if (line.Back != null)
            {
                LineOpening opening = new LineOpening(line.Front.Sector, line.Back.Sector);
                return !opening.CanPassOrStepThrough(entity);
            }

            return false;
        }

        private static bool EntityBlocksEntity(Entity entity, Entity other)
        {
            // Note: This is simple for right now, everything blocks everything
            // else except running into itself.
            return !ReferenceEquals(entity, other);
        }

        private void SetEntityOnFloorOrEntity(Entity entity, double floorZ)
        {
            // If we're airborne and just landed on the ground, we need a delay
            // for jumping. This should only happen if we've coming down from a
            // manual jump.
            if (!entity.OnGround && entity.IsJumping)
            {
                entity.IsJumping = false;
                entity.JumpDelayTicks = JumpDelayTicks;
            }
            
            entity.SetZ(floorZ);
            entity.OnGround = true;
            
            // For now we remove any negative velocity. If upward velocity is
            // reset to zero then the jump we apply to players is lost and they
            // can never jump. Maybe we want to fix this in the future by doing
            // application of jumping after the XY movement instead of before?
            entity.Velocity.Z = Math.Max(0, entity.Velocity.Z);
        }

        private void ClampBetweenFloorAndCeiling(Entity entity)
        {
            if (entity.NoClip && entity.IsFlying)
                return;

            double lowestCeil = entity.LowestCeilingSector.Ceiling.Plane.ToZ(entity.Position);
            double highestFloor = entity.HighestFloorSector.Floor.Plane.ToZ(entity.Position);

            if (entity.Box.Top > lowestCeil)
            {
                entity.SetZ(lowestCeil - entity.Height);
                entity.Velocity.Z = 0;
            }

            if (entity.Box.Bottom <= highestFloor)
                SetEntityOnFloorOrEntity(entity, highestFloor);
            else
                entity.OnGround = false;
        }

        private void LinkToSectors(Entity entity)
        {
            Precondition(entity.SectorNodes.Empty(), "Forgot to unlink entity from blockmap");
            
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
                        if (!entity.NoClip)
                        {
                            if (line.HasSpecial && !entity.IntersectSpecialLines.Any(x => x.Id == line.Id))
                                entity.IntersectSpecialLines.Add(line);
                        }

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
        
        private void ClearVelocityXY(Entity entity)
        {
            entity.Velocity.X = 0;
            entity.Velocity.Y = 0;
        }

        private void PerformMoveXY(Entity entity)
        {
            Precondition(entity.Velocity.To2D() != Vec2D.Zero, "Cannot move with zero horizontal velocity");
            
            int slidesLeft = MaxSlides;
            Vec2D velocity = entity.Velocity.To2D();

            if (entity.NoClip)
            {
                HandleNoClip(entity, velocity);
                return;
            }

            // TODO temporary until we know how this actually works
            if (entity.IsCrushing())
                return;

            // We advance in small steps that are smaller than the radius of
            // the actor so we don't skip over any lines or things due to fast
            // entity speed.
            int numMoves = CalculateSteps(velocity, entity.Radius);
            Vec2D stepDelta = velocity / numMoves;
            
            for (int movesLeft = numMoves; movesLeft > 0; movesLeft--)
            {
                if (stepDelta == Vec2D.Zero)
                    break;
                
                Vec2D nextPosition = entity.Position.To2D() + stepDelta;

                if (CanMoveTo(entity, nextPosition))
                {
                    MoveTo(entity, nextPosition);
                    continue;
                }

                if (entity.Definition.Flags.SlidesOnWalls && slidesLeft > 0)
                {
                    HandleSlide(entity, ref stepDelta, ref movesLeft);
                    slidesLeft--;
                    continue;
                }
                
                ClearVelocityXY(entity);
                break;
            }
        }

        private void HandleNoClip(Entity entity, Vec2D velocity)
        {
            entity.UnlinkFromWorld();
            var pos = entity.Position.To2D() + velocity;
            entity.SetXY(pos);
            LinkToWorld(entity);
        }

        private bool CanMoveTo(Entity entity, Vec2D nextPosition)
        {
            Box2D nextBox = entity.Box.To2D().CopyToOffset(nextPosition, entity.Radius);
            return !m_blockmap.Iterate(nextBox, CheckForBlockers);

            GridIterationStatus CheckForBlockers(Block block)
            {
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.Segment.Intersects(nextBox) && LineBlocksEntity(entity, line))
                        return GridIterationStatus.Stop;
                }

                LinkableNode<Entity>? entityNode = block.Entities.Head;
                while (entityNode != null)
                {
                    Entity nextEntity = entityNode.Value;
                    if (EntityBlocksEntity(entity, nextEntity))
                    {
                        entity.IntersectEntities.Add(nextEntity);
                        if (nextEntity.Box.To2D().Overlaps(nextBox) && entity.Box.OverlapsZ(nextEntity.Box))
                            return GridIterationStatus.Stop;
                    }

                    entityNode = entityNode.Next;
                }
                
                return GridIterationStatus.Continue;
            }
        }
        
        private void HandleStepIfNeeded(Entity entity, Entity other)
        {
            if (!entity.Box.Overlaps(other.Box)) 
                return;
            
            Precondition(entity.Box.Bottom >= other.Box.Top - entity.Definition.Properties.StepHeight, "Entity too high to step up onto");
            SetEntityOnFloorOrEntity(entity, other.Box.Top);
        }

        private void MoveTo(Entity entity, Vec2D nextPosition)
        {
            entity.UnlinkFromWorld();

            Vec2D previousPosition = entity.Position.To2D();
            entity.SetXY(nextPosition);

            LinkToWorld(entity);

            for (int i = 0; i < entity.IntersectSpecialLines.Count; i++)
                CheckLineSpecialActivation(entity, entity.IntersectSpecialLines[i], previousPosition);

            for (int i = 0; i < entity.IntersectEntities.Count; i++)
                HandleStepIfNeeded(entity, entity.IntersectEntities[i]);
        }

        private void CheckLineSpecialActivation(Entity entity, Line line, Vec2D previousPosition)
        {
            if (!line.Special.CanActivate(entity, line.Flags, ActivationContext.CrossLine))
                return;

            bool fromFront = line.Segment.OnRight(previousPosition);
            if (fromFront != line.Segment.OnRight(entity.Position.To2D()))
            {
                if (line.Special.IsTeleport() && !fromFront)
                    return;

                m_entityActivatedSpecialArgs.ActivationContext = ActivationContext.CrossLine;
                m_entityActivatedSpecialArgs.Entity = entity;
                m_entityActivatedSpecialArgs.ActivateLineSpecial = line;
                EntityActivatedSpecial?.Invoke(this, m_entityActivatedSpecialArgs);
            }
        }

        private void HandleSlide(Entity entity, ref Vec2D stepDelta, ref int movesLeft)
        {
            if (FindClosestBlockingLine(entity, stepDelta, out MoveInfo moveInfo))
            {
                if (MoveCloseToBlockingLine(entity, stepDelta, moveInfo, out Vec2D residualStep))
                {
                    ReorientToSlideAlong(entity, moveInfo.BlockingLine, residualStep, ref stepDelta, ref movesLeft);
                    return;
                }
            }

            if (AttemptAxisMove(entity, stepDelta, Axis2D.Y))
                return;
            if (AttemptAxisMove(entity, stepDelta, Axis2D.X))
                return;

            // If we cannot find the line or thing that is blocking us, then we
            // are fully done moving horizontally.
            ClearVelocityXY(entity);
            stepDelta.X = 0;
            stepDelta.Y = 0;
            movesLeft = 0;
        }
        
        private BoxCornerTracers CalculateCornerTracers(Box2D currentBox, Vec2D stepDelta)
        {
            Vec2D[] corners;
            
            if (stepDelta.X >= 0)
            {
                corners = stepDelta.Y >= 0 ? 
                    new[] { currentBox.TopLeft, currentBox.TopRight, currentBox.BottomRight } : 
                    new[] { currentBox.TopRight, currentBox.BottomRight, currentBox.BottomLeft };
            }
            else
            {
                corners = stepDelta.Y >= 0 ? 
                    new[] { currentBox.TopRight, currentBox.TopLeft, currentBox.BottomLeft } : 
                    new[] { currentBox.TopLeft, currentBox.BottomLeft, currentBox.BottomRight };
            }

            Seg2DBase first = new Seg2DBase(corners[0], corners[0] + stepDelta);
            Seg2DBase second = new Seg2DBase(corners[1], corners[1] + stepDelta);
            Seg2DBase third = new Seg2DBase(corners[2], corners[2] + stepDelta);
            return new BoxCornerTracers(first, second, third);
        }

        private void CheckCornerTracerIntersection(Seg2DBase cornerTracer, Entity entity, ref MoveInfo moveInfo)
        {
            bool hit = false;
            double hitTime = double.MaxValue;
            Line? blockingLine = null;
            
            m_blockmap.Iterate(cornerTracer, CheckForTracerHit);
            
            if (hit && hitTime < moveInfo.LineIntersectionTime)
            {
                moveInfo.IntersectionFound = true;
                moveInfo.LineIntersectionTime = hitTime;
                moveInfo.BlockingLine = blockingLine;
            }

            GridIterationStatus CheckForTracerHit(Block block)
            {
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    
                    if (cornerTracer.Intersection(line.Segment, out double time) && 
                        LineBlocksEntity(entity, line) &&
                        time < hitTime)
                    {
                        hit = true;
                        hitTime = time;
                        blockingLine = line;  
                    }
                }
                
                return GridIterationStatus.Continue;
            }
        }

        private bool FindClosestBlockingLine(Entity entity, Vec2D stepDelta, out MoveInfo moveInfo)
        {
            moveInfo = MoveInfo.Empty();

            // We shoot out 3 tracers from the corners in the direction we're
            // travelling to see if there's a blocking line as follows:
            //    _  _
            //    /| /|   If we're travelling northeast, then from the
            //   /  /_    top right corners of the bounding box we will
            //  o--o /|   shoot out tracers in the direction we are going
            //  |  |/     to step to see if we hit anything
            //  o--o
            //
            // This obviously can miss things, but this is how vanilla does it
            // and we want to have compatibility with the mods that use.
            Box2D currentBox = entity.Box.To2D();
            BoxCornerTracers tracers = CalculateCornerTracers(currentBox, stepDelta);
            CheckCornerTracerIntersection(tracers.First, entity, ref moveInfo);
            CheckCornerTracerIntersection(tracers.Second, entity, ref moveInfo);
            CheckCornerTracerIntersection(tracers.Third, entity, ref moveInfo);
            
            return moveInfo.IntersectionFound;
        }

        private bool MoveCloseToBlockingLine(Entity entity, Vec2D stepDelta, MoveInfo moveInfo, out Vec2D residualStep)
        {
            Precondition(moveInfo.LineIntersectionTime >= 0, "Blocking line intersection time should never be negative");
            Precondition(moveInfo.IntersectionFound, "Should not be moving close to a line if we didn't hit one");
            
            // If it's close enough that stepping back would move us further
            // back than we currently are (or move us nowhere), we don't need
            // to do anything. This also means the residual step is equal to
            // the entire step since we're not stepping anywhere.
            if (moveInfo.LineIntersectionTime <= SlideStepBackTime)
            {
                residualStep = stepDelta;
                return true;
            }

            double t = moveInfo.LineIntersectionTime - SlideStepBackTime;
            Vec2D usedStepDelta = stepDelta * t;
            residualStep = stepDelta - usedStepDelta;

            Vec2D closeToLinePosition = entity.Position.To2D() + usedStepDelta;
            if (CanMoveTo(entity, closeToLinePosition))
            {
                MoveTo(entity, closeToLinePosition);
                return true;
            }
            
            return false;
        }

        private void ReorientToSlideAlong(Entity entity, Line blockingLine, Vec2D residualStep, ref Vec2D stepDelta, 
            ref int movesLeft)
        {
            // Our slide direction depends on if we're going along with the
            // line or against the line. If the dot product is negative, it
            // means we are facing away from the line and should slide in
            // the opposite direction from the way the line is pointing.
            // TODO: We can cache the Unit() for the line for perf reasons.
            Vec2D unitDirection = blockingLine.Segment.Delta.Unit();
            if (stepDelta.Dot(unitDirection) < 0)
                unitDirection = -unitDirection;
            
            // Because we moved up to the wall, it's almost always the case
            // that we didn't make 100% of a step. For example if we have some
            // movement of 5 map units towards a wall and run into the wall at
            // 3 (leaving 2 map units unhandled), we want to work that residual
            // map unit movement into the existing step length. The following
            // does that by finding the total movement scalar and applying it
            // to the direction we need to slide.
            //
            // We also must take into account that we're adding some scalar to
            // another scalar, which means we'll end up with usually a larger
            // one. This means our step delta could grow beyond the size of the
            // radius of the entity and cause it to skip lines in pathological
            // situations. I haven't encountered such a case yet but it is at
            // least theoretically possible this can happen. Because of this,
            // the movesLeft is incremented by 1 to make sure the stepDelta
            // at the end of this function stays smaller than the radius.
            // TODO: If we have the unit vector, is projection overkill? Can we
            //       just multiply by the component instead?
            Vec2D stepProjection = stepDelta.Projection(unitDirection);
            Vec2D residualProjection = residualStep.Projection(unitDirection);

            // TODO: This is almost surely not how it's done, but it feels okay
            //       enough right now to leave as is.
            entity.Velocity.X = stepProjection.X * Friction;
            entity.Velocity.Y = stepProjection.Y * Friction;
            
            double totalRemainingDistance = ((stepProjection * movesLeft) + residualProjection).Length();
            movesLeft += 1;
            stepDelta = unitDirection * totalRemainingDistance / movesLeft;
        }

        private bool AttemptAxisMove(Entity entity, Vec2D stepDelta, Axis2D axis)
        {
            if (axis == Axis2D.X)
            {
                Vec2D nextPosition = entity.Position.To2D() + new Vec2D(stepDelta.X, 0);
                if (CanMoveTo(entity, nextPosition))
                {
                    MoveTo(entity, nextPosition);
                    entity.Velocity.Y = 0;
                    stepDelta.Y = 0;
                    return true;
                }                
            }
            else
            {
                Vec2D nextPosition = entity.Position.To2D() + new Vec2D(0, stepDelta.Y);
                if (CanMoveTo(entity, nextPosition))
                {
                    MoveTo(entity, nextPosition);
                    entity.Velocity.X = 0;
                    stepDelta.X = 0;
                    return true;
                }    
            }

            return false;
        }

        private void MoveXY(Entity entity)
        {
            if (entity.Velocity.To2D() == Vec2D.Zero)
                return;

            PerformMoveXY(entity);
            ApplyFriction(entity);
            StopXYMovementIfSmall(entity);
        }

        private void MoveZ(Entity entity)
        {
            if (entity.Player != null && entity.IsFlying)
            {
                entity.Velocity.Z *= Friction;
            }
            else if (entity.OnGround)
            {
                if (entity.JumpDelayTicks > 0)
                    entity.JumpDelayTicks--;
            }
            else
            {
                entity.Velocity.Z -= Gravity;
            }
            
            // TODO: Check if any entities are in the way of our movement.

            entity.SetZ(entity.Position.Z + entity.Velocity.Z);
            ClampBetweenFloorAndCeiling(entity);
        }
    }
}