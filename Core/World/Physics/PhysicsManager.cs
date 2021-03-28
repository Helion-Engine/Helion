using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Geometry;
using Helion.Geometry.Grids;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.RandomGenerators;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;
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
        private const double Friction = 0.90625;
        private const double SlideStepBackTime = 1.0 / 32.0;
        private const double MinMovementThreshold = 0.06;
        private const double SetEntityToFloorSpeedMax = 9;

        public static readonly double LowestPossibleZ = Fixed.Lowest().ToDouble();

        public BlockmapTraverser BlockmapTraverser { get; private set; }

        private readonly IWorld m_world;
        private readonly BspTree m_bspTree;
        private readonly BlockMap m_blockmap;
        private readonly EntityManager m_entityManager;
        private readonly WorldSoundManager m_soundManager;
        private readonly IRandom m_random;
        private readonly LineOpening m_lineOpening = new LineOpening();
        private readonly TryMoveData m_tryMoveData = new TryMoveData();

        /// <summary>
        /// Creates a new physics manager which utilizes the arguments for any
        /// collision detection or linking to the world.
        /// </summary>
        /// <param name="world">The world to operate on.</param>
        /// <param name="bspTree">The BSP tree for the world.</param>
        /// <param name="blockmap">The blockmap for the world.</param>
        /// <param name="soundManager">The sound manager to play sounds from.</param>
        /// <param name="entityManager">entity manager.</param>
        /// <param name="random">Random number generator to use.</param>
        public PhysicsManager(IWorld world, BspTree bspTree, BlockMap blockmap, WorldSoundManager soundManager, EntityManager entityManager, IRandom random)
        {
            m_world = world;
            m_bspTree = bspTree;
            m_blockmap = blockmap;
            m_soundManager = soundManager;
            m_entityManager = entityManager;
            m_random = random;
            BlockmapTraverser = new BlockmapTraverser(m_blockmap);
        }

        /// <summary>
        /// Links an entity to the world.
        /// </summary>
        /// <param name="entity">The entity to link.</param>
        /// <param name="tryMove">Optional data used for when linking during movement.</param>
        /// <param name="clampToLinkedSectors">If the entity should be clamped between linked sectors. If false then on the current Sector ceiling/floor will be used. (Doom compatibility).</param>
        public void LinkToWorld(Entity entity, TryMoveData? tryMove = null, bool clampToLinkedSectors = true)
        {
            if (!entity.Flags.NoBlockmap)
                m_blockmap.Link(entity);

            LinkToSectors(entity, tryMove);

            ClampBetweenFloorAndCeiling(entity, clampToLinkedSectors);
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

        public SectorMoveStatus MoveSectorZ(Sector sector, SectorPlane sectorPlane, SectorPlaneType moveType, 
            double speed, double destZ, CrushData? crush)
        {
            // Save the Z value because we are only checking if the dest is valid
            // If the move is invalid because of a blocking entity then it will not be set to destZ
            List<Entity> crushEntities = new List<Entity>();
            Entity? highestBlockEntity = null;
            double? highestBlockHeight = 0.0;
            SectorMoveStatus status = SectorMoveStatus.Success;
            double startZ = sectorPlane.Z;
            sectorPlane.PrevZ = startZ;
            sectorPlane.Z = destZ;
            sectorPlane.Plane.MoveZ(destZ - startZ);

            // Move lower entities first to handle stacked entities
            var entities = sector.Entities.OrderBy(x => x.Box.Bottom).ToList();

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                entity.SaveZ = entity.Position.Z;
                entity.PrevSaveZ = entity.PrevPosition.Z;

                // At slower speeds we need to set entities to the floor
                // Otherwise the player will fall and hit the floor repeatedly creating a weird bouncing effect
                if (moveType == SectorPlaneType.Floor && startZ > destZ && -speed < SetEntityToFloorSpeedMax &&
                    entity.OnGround && !entity.Flags.NoGravity && entity.HighestFloorSector == sector)
                {
                    entity.SetZ(entity.OnEntity?.Box.Top ?? destZ, false);
                    // Setting this so SetEntityBoundsZ does not mess with forcing this entity to to the floor
                    // Otherwise this is a problem with the instant lift hack
                    entity.PrevPosition.Z = entity.Position.Z;
                }

                ClampBetweenFloorAndCeiling(entity);

                double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;
                if (thingZ + entity.Height > entity.LowestCeilingZ)
                {
                    if (moveType == SectorPlaneType.Ceiling)
                        PushDownBlockingEntities(entity);
                    // Clipped something that wasn't directly on this entity before the move and now it will be
                    // Push the entity up, and the next loop will verify it is legal
                    else
                        PushUpBlockingEntity(entity);
                }
            }

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                ClampBetweenFloorAndCeiling(entity);
                entity.PrevPosition.Z = entity.PrevSaveZ;
                // This allows the player to pickup items like the original
                if (entity is Player)
                    IsPositionValid(entity, entity.Position.XY, m_tryMoveData);

                if ((moveType == SectorPlaneType.Ceiling && startZ < destZ) || 
                    (moveType == SectorPlaneType.Floor && startZ > destZ))
                    continue;

                double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;

                if (thingZ + entity.Height > entity.LowestCeilingZ)
                {
                    if (entity.Flags.Dropped)
                    {
                        m_entityManager.Destroy(entity);
                        continue;
                    }

                    // Need to gib things even when not crushing and do not count as blocking
                    if (entity.Flags.Corpse && !entity.Flags.DontGib)
                    {
                        SetToGiblets(entity);
                        continue;
                    }

                    if (!entity.Flags.Solid)
                        continue;

                    if (crush != null)
                    {
                        if (crush.CrushMode == ZDoomCrushMode.Hexen)
                        {
                            highestBlockEntity = entity;
                            highestBlockHeight = entity.Height;
                        }

                        status = SectorMoveStatus.Crush;
                        crushEntities.Add(entity);
                    }
                    else
                    {
                        highestBlockEntity = entity;
                        highestBlockHeight = entity.Height;
                        status = SectorMoveStatus.Blocked;
                    }
                }
            }

            if (highestBlockEntity != null && highestBlockHeight.HasValue && !highestBlockEntity.IsDead)
            {
                double thingZ = highestBlockEntity.OnGround ? highestBlockEntity.HighestFloorZ : highestBlockEntity.Position.Z;
                // Set the sector Z to the difference of the blocked height
                double diff = Math.Abs(startZ - destZ) - (thingZ + highestBlockHeight.Value - highestBlockEntity.LowestCeilingZ);
                if (speed < 0)
                    diff = -diff;

                sectorPlane.Z = startZ + diff;
                sectorPlane.Plane.MoveZ(startZ - destZ + diff);

                // Entity blocked movement, reset all entities in moving sector after resetting sector Z
                foreach (var relinkEntity in entities)
                {
                    // Check for entities that may be dead from being crushed
                    if (relinkEntity.IsDisposed)
                        continue;
                    relinkEntity.UnlinkFromWorld();
                    relinkEntity.SetZ(relinkEntity.SaveZ + diff, false);
                    LinkToWorld(relinkEntity);
                }
            }

            if (crush != null && crushEntities.Count > 0)
                CrushEntities(crushEntities, sector, crush);

            return status;
        }

        private void CrushEntities(List<Entity> crushEntities, Sector sector, CrushData crush)
        {
            if (crush.Damage == 0 || (m_world.Gametick & 3) != 0)
                return;

            // Check for stacked entities, so we can crush the stack
            List<Entity> stackCrush = new List<Entity>();
            foreach (Entity checkEntity in sector.Entities)
            {
                if (checkEntity.OverEntity != null && crushEntities.Contains(checkEntity.OverEntity))
                    stackCrush.Add(checkEntity);
            }

            stackCrush = stackCrush.Union(crushEntities).Distinct().ToList();

            foreach (Entity crushEntity in stackCrush)
            {
                if (!crushEntity.IsDead && m_world.DamageEntity(crushEntity, null, crush.Damage))
                {
                    Vec3D pos = crushEntity.Position;
                    pos.Z += crushEntity.Height / 2;
                    Entity? blood = m_entityManager.Create(crushEntity.GetBloodType(), pos);
                    if (blood != null)
                    {
                        blood.Velocity.X += m_random.NextDiff() / 16.0;
                        blood.Velocity.Y += m_random.NextDiff() / 16.0;
                    }
                }
            }
        }

        private void SetToGiblets(Entity entity)
        {
            if (!entity.SetCrushState())
            {
                m_entityManager.Destroy(entity);
                m_entityManager.Create("REALGIBS", entity.Position);
            }
        }

        private static void PushUpBlockingEntity(Entity pusher)
        {
            if (!(pusher.LowestCeilingObject is Entity))
                return;

            Entity entity = (Entity)pusher.LowestCeilingObject;
            entity.SetZ(pusher.Box.Top, false);
        }

        private static void PushDownBlockingEntities(Entity pusher)
        {
            // Because of how ClampBetweenFloorAndCeiling works, try to push down the entire stack and stop when something clips a floor
            if (pusher.HighestFloorObject is Sector && pusher.HighestFloorZ > pusher.LowestCeilingZ - pusher.Height)
                return;

            pusher.SetZ(pusher.LowestCeilingZ - pusher.Height, false);

            if (pusher.OnEntity != null)
            {
                Entity? current = pusher.OnEntity;
                while (current != null)
                {
                    if (current.HighestFloorObject is Sector && current.HighestFloorZ > pusher.Box.Bottom - current.Height)
                        return;

                    current.SetZ(pusher.Box.Bottom - current.Height, false);
                    pusher = current;
                    current = pusher.OnEntity;
                }
            }
        }

        public void HandleEntityDeath(Entity deathEntity)
        {
            if (deathEntity.OnEntity != null || deathEntity.OverEntity != null)
                HandleStackedEntityPhysics(deathEntity);
        }

        private static int CalculateSteps(Vec2D velocity, double radius)
        {
            Invariant(radius > 0.5, "Actor radius too small for safe XY physics movement");

            // We want to pick some atomic distance to keep moving our bounding
            // box. It can't be bigger than the radius because we could end up
            // skipping over a line.
            double moveDistance = radius - 0.5;
            double biggerAxis = Math.Max(Math.Abs(velocity.X), Math.Abs(velocity.Y));
            return (int)(biggerAxis / moveDistance) + 1;
        }

        private static void ApplyFriction(Entity entity)
        {
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

        private enum LineBlock
        {
            NoBlock,
            BlockStopChecking,
            BlockContinueIfFloat,
        }

        private LineBlock LineBlocksEntity(Entity entity, in Vec2D position, Line line, TryMoveData? tryMove)
        {
            if (line.BlocksEntity(entity))
                return LineBlock.BlockStopChecking;
            if (line.Back == null)
                return LineBlock.NoBlock;

            LineOpening opening = GetLineOpening(position, line);
            tryMove?.SetIntersectionData(opening);

            if (opening.CanPassOrStepThrough(entity))
                return LineBlock.NoBlock;

            return LineBlock.BlockContinueIfFloat;
        }

        public LineOpening GetLineOpening(in Vec2D position, Line line)
        {
            m_lineOpening.Set(position, line);
            return m_lineOpening;
        }

        private void SetEntityOnFloorOrEntity(Entity entity, double floorZ, bool smoothZ)
        {
            // Additionally check to smooth camera when stepping up to an entity
            entity.SetZ(floorZ, smoothZ);

            // For now we remove any negative velocity. If upward velocity is
            // reset to zero then the jump we apply to players is lost and they
            // can never jump. Maybe we want to fix this in the future by doing
            // application of jumping after the XY movement instead of before?
            entity.Velocity.Z = Math.Max(0, entity.Velocity.Z);
        }

        private void ClampBetweenFloorAndCeiling(Entity entity, bool clampToLinkedSectors = true)
        {
            // TODO fixme
            if (entity.Definition.Name == "BulletPuff")
                return;
            if (entity.Flags.NoClip && entity.Flags.NoGravity)
                return;

            object lastHighestFloorObject = entity.HighestFloorObject;
            SetEntityBoundsZ(entity, clampToLinkedSectors);

            double lowestCeil = entity.LowestCeilingZ;
            double highestFloor = entity.HighestFloorZ;
            double floorZ = entity.Sector.ToFloorZ(entity.Position);

            if (lowestCeil - entity.Height >= floorZ && entity.Box.Top > lowestCeil)
            {
                entity.Velocity.Z = 0;
                entity.SetZ(lowestCeil - entity.Height, false);

                if (entity.LowestCeilingObject is Entity blockEntity)
                    entity.BlockingEntity = blockEntity;
                else
                    entity.BlockingSectorPlane = entity.LowestCeilingSector.Ceiling;
            }

            bool clippedFloor = entity.Box.Bottom < highestFloor;
            if (entity.Box.Bottom <= highestFloor)
            {
                if (entity.HighestFloorObject is Entity highestEntity &&
                    highestEntity.Box.Top <= entity.Box.Bottom + entity.GetMaxStepHeight())
                {
                    entity.OnEntity = highestEntity;
                }

                if (entity.OnEntity != null)
                    entity.OnEntity.OverEntity = entity;

                SetEntityOnFloorOrEntity(entity, highestFloor, lastHighestFloorObject != entity.HighestFloorObject);

                if (clippedFloor)
                {
                    if (entity.HighestFloorObject is Entity blockEntity)
                        entity.BlockingEntity = blockEntity;
                    else
                        entity.BlockingSectorPlane = entity.HighestFloorSector.Floor;
                }
            }
        }

        private void SetEntityBoundsZ(Entity entity, bool clampToLinkedSectors)
        {
            Sector highestFloor = entity.Sector;
            Sector lowestCeiling = entity.Sector;
            Entity? highestFloorEntity = null;
            Entity? lowestCeilingEntity = null;
            double highestFloorZ = highestFloor.ToFloorZ(entity.Position);
            double lowestCeilZ = lowestCeiling.ToCeilingZ(entity.Position);

            entity.OnEntity = null;
            entity.ClippedWithEntity = false;

            if (clampToLinkedSectors)
            {
                foreach (Sector sector in entity.IntersectSectors)
                {
                    double floorZ = sector.ToFloorZ(entity.Position);
                    if (floorZ > highestFloorZ)
                    {
                        highestFloor = sector;
                        highestFloorZ = floorZ;
                    }

                    double ceilZ = sector.ToCeilingZ(entity.Position);
                    if (ceilZ < lowestCeilZ)
                    {
                        lowestCeiling = sector;
                        lowestCeilZ = ceilZ;
                    }
                }
            }

            // Only check against other entities if CanPass is set (height sensitive clip detection)
            if (entity.Flags.CanPass && !entity.Flags.NoClip)
            {
                // Get intersecting entities here - They are not stored in the entity because other entities can move around after this entity has linked
                List<BlockmapIntersect> intersections = BlockmapTraverser.GetBlockmapIntersections(entity.Box.To2D(),
                    BlockmapTraverseFlags.Entities, BlockmapTraverseEntityFlags.Solid | BlockmapTraverseEntityFlags.NotCorpse);

                for (int i = 0; i < intersections.Count; i++)
                {
                    Entity? intersectEntity = intersections[i].Entity;
                    if (intersectEntity == null || ReferenceEquals(entity, intersectEntity) || intersectEntity.Flags.NoClip)
                        continue;

                    bool above = entity.PrevPosition.Z >= intersectEntity.Box.Top;
                    bool below = entity.PrevPosition.Z + entity.Height <= intersectEntity.Box.Bottom;
                    bool clipped = false;
                    if (above && entity.Box.Bottom < intersectEntity.Box.Top)
                        clipped = true;
                    else if (below && entity.Box.Top > intersectEntity.Box.Bottom)
                        clipped = true;

                    if (!above && !below && !clampToLinkedSectors && !intersectEntity.Flags.ActLikeBridge)
                    {
                        entity.ClippedWithEntity = true;
                        continue;
                    }

                    if (above)
                    {
                        // Need to check clipping coming from above, if we're above
                        // or clipped through then this is our floor.
                        if ((clipped || entity.Box.Bottom >= intersectEntity.Box.Top) && intersectEntity.Box.Top > highestFloorZ)
                        {
                            highestFloorEntity = intersectEntity;
                            highestFloorZ = intersectEntity.Box.Top;
                        }
                    }
                    else if (below)
                    {
                        // Same check as above but checking clipping the ceiling.
                        if ((clipped || entity.Box.Top <= intersectEntity.Box.Bottom) && intersectEntity.Box.Bottom < lowestCeilZ)
                        {
                            lowestCeilingEntity = intersectEntity;
                            lowestCeilZ = intersectEntity.Box.Bottom;
                        }
                    }

                    // Need to check if we can step up to this floor.
                    if (entity.Box.Bottom + entity.GetMaxStepHeight() >= intersectEntity.Box.Top && intersectEntity.Box.Top > highestFloorZ)
                    {
                        highestFloorEntity = intersectEntity;
                        highestFloorZ = intersectEntity.Box.Top;
                    }
                }

                DataCache.Instance.FreeBlockmapIntersectList(intersections);
            }

            entity.HighestFloorZ = highestFloorZ;
            entity.LowestCeilingZ = lowestCeilZ;
            entity.HighestFloorSector = highestFloor;
            entity.LowestCeilingSector = lowestCeiling;

            if (highestFloorEntity != null && highestFloorEntity.Box.Top > highestFloor.ToFloorZ(entity.Position))
                entity.HighestFloorObject = highestFloorEntity;
            else
                entity.HighestFloorObject = highestFloor;

            if (lowestCeilingEntity != null && lowestCeilingEntity.Box.Top < lowestCeiling.ToCeilingZ(entity.Position))
                entity.LowestCeilingObject = lowestCeilingEntity;
            else
                entity.LowestCeilingObject = lowestCeiling;

            entity.CheckOnGround();
        }

        private void LinkToSectors(Entity entity, TryMoveData? tryMove)
        {
            Precondition(entity.SectorNodes.Empty(), "Forgot to unlink entity from blockmap");

            Subsector centerSubsector = m_bspTree.ToSubsector(entity.Position);
            Sector centerSector = centerSubsector.Sector;
            HashSet<Sector> sectors = DataCache.Instance.GetSectorSet();
            sectors.Add(centerSector);

            // TODO: Can we replace this by iterating over the blocks were already in?
            Box2D box = entity.Box.To2D();
            m_blockmap.Iterate(box, SectorOverlapFinder);

            entity.Sector = centerSector;
            foreach (Sector sector in sectors)
                entity.IntersectSectors.Add(sector);

            if (!entity.Flags.NoSector)
            {
                for (int i = 0; i < entity.IntersectSectors.Count; i++)
                    entity.SectorNodes.Add(entity.IntersectSectors[i].Link(entity));

                entity.SubsectorNode = centerSubsector.Link(entity);
            }

            DataCache.Instance.FreeSectorSet(sectors);

            GridIterationStatus SectorOverlapFinder(Block block)
            {
                // Doing iteration over enumeration for performance reasons.
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.Segment.Intersects(box))
                    {
                        if (tryMove != null && !entity.Flags.NoClip && line.HasSpecial)
                            tryMove.AddIntersectSpecialLine(line);

                        sectors.Add(line.Front.Sector);

                        if (line.Back != null)
                            sectors.Add(line.Back.Sector);
                    }
                }

                return GridIterationStatus.Continue;
            }
        }

        private void ClearVelocityXY(Entity entity)
        {
            entity.Velocity.X = 0;
            entity.Velocity.Y = 0;
        }

        public TryMoveData TryMoveXY(Entity entity, Vec2D position, bool stepMove = true)
        {
            m_tryMoveData.SetPosition(position);
            if (entity.Flags.NoClip)
            {
                HandleNoClip(entity, position);
                m_tryMoveData.Success = true;
                return m_tryMoveData;
            }

            if (entity.ClippedWithEntity && !entity.OnGround && entity.IsClippedWithEntity())
            {
                m_tryMoveData.Success = false;
                entity.Velocity = Vec3D.Zero;
                return m_tryMoveData;
            }

            if (entity.IsCrushing())
            {
                m_tryMoveData.Success = false;
                return m_tryMoveData;
            }

            bool success = true;

            if (stepMove)
            {
                // We advance in small steps that are smaller than the radius of
                // the actor so we don't skip over any lines or things due to fast
                // entity speed.
                int slidesLeft = MaxSlides;
                Vec2D velocity = position - entity.Position.XY;
                int numMoves = CalculateSteps(velocity, entity.Radius);
                Vec2D stepDelta = velocity / numMoves;

                for (int movesLeft = numMoves; movesLeft > 0; movesLeft--)
                {
                    if (stepDelta == Vec2D.Zero || m_world.WorldState == WorldState.Exit)
                        break;

                    Vec2D nextPosition = entity.Position.XY + stepDelta;

                    if (IsPositionValid(entity, nextPosition, m_tryMoveData))
                    {
                        entity.MoveLinked = true;
                        MoveTo(entity, nextPosition, m_tryMoveData);
                        continue;
                    }

                    if (entity.Flags.SlidesOnWalls && slidesLeft > 0)
                    {
                        HandleSlide(entity, ref stepDelta, ref movesLeft, m_tryMoveData);
                        slidesLeft--;
                        success = false;
                        continue;
                    }

                    success = false;
                    ClearVelocityXY(entity);
                    break;
                }
            }
            else
            {
                success = IsPositionValid(entity, position, m_tryMoveData);
                if (success)
                {
                    entity.MoveLinked = true;
                    MoveTo(entity, position, m_tryMoveData);
                }
            }

            if (success && entity.OverEntity != null)
                HandleStackedEntityPhysics(entity);

            m_tryMoveData.Success = success;
            return m_tryMoveData;
        }

        private void HandleStackedEntityPhysics(Entity entity)
        {
            Entity? currentOverEntity = entity.OverEntity;

            if (entity.OnEntity != null)
                ClampBetweenFloorAndCeiling(entity.OnEntity);

            while (currentOverEntity != null)
            {
                foreach (var relinkEntity in entity.Sector.Entities)
                {
                    if (relinkEntity.OnEntity == entity)
                        ClampBetweenFloorAndCeiling(relinkEntity, false);
                }

                entity = currentOverEntity;
                Entity? next = currentOverEntity.OverEntity;
                if (currentOverEntity.OverEntity != null && currentOverEntity.OverEntity.OnEntity != entity)
                    currentOverEntity.OverEntity = null;
                currentOverEntity = next;
            }
        }

        private void HandleNoClip(Entity entity, Vec2D position)
        {
            entity.UnlinkFromWorld();
            entity.SetXY(position);
            LinkToWorld(entity);
        }

        public bool IsPositionValid(Entity entity, Vec2D position, TryMoveData tryMove)
        {
            if (!entity.Flags.Float && !(entity is Player) && entity.OnEntity != null && !entity.OnEntity.Flags.ActLikeBridge)
                return false;

            tryMove.Success = true;
            tryMove.LowestCeilingZ = entity.LowestCeilingZ;
            if (entity.HighestFloorObject is Entity highFloorEntity)
            {
                tryMove.HighestFloorZ = highFloorEntity.Box.Top;
                tryMove.DropOffZ = entity.Sector.ToFloorZ(position);
            }
            else
            {
                tryMove.HighestFloorZ = tryMove.DropOffZ = entity.Sector.ToFloorZ(position);
            }

            Box2D nextBox = Box2D.CopyToOffset(position, entity.Radius);
            entity.BlockingLine = null;
            entity.BlockingEntity = null;
            entity.BlockingSectorPlane = null;
            m_blockmap.Iterate(nextBox, CheckForBlockers);

            if (entity.BlockingLine != null && entity.BlockingLine.BlocksEntity(entity))
            {
                tryMove.Success = false;
                return false;
            }

            if (tryMove.LowestCeilingZ - tryMove.HighestFloorZ < entity.Height || entity.BlockingEntity != null)
            {
                tryMove.Success = false;
                return false;
            }

            tryMove.CanFloat = true;

            if (!entity.CheckDropOff(tryMove))
                tryMove.Success = false;

            return tryMove.Success;

            GridIterationStatus CheckForBlockers(Block block)
            {
                // This may need to come after if we want to do plasma bumping.
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.Segment.Intersects(nextBox))
                    {
                        if (!entity.Flags.NoClip && line.HasSpecial)
                            tryMove.AddIntersectSpecialLine(line);

                        LineBlock blockType = LineBlocksEntity(entity, position, line, tryMove);
                        if (blockType != LineBlock.NoBlock)
                        {
                            entity.BlockingLine = line;
                            tryMove.Success = false;
                            // Only keep checking if entity floats
                            // Block floating check needs all intersecting LineOpenings
                            if (blockType == LineBlock.BlockStopChecking || (blockType == LineBlock.BlockContinueIfFloat && !entity.Flags.Float))
                                return GridIterationStatus.Stop;
                        }
                    }
                }

                if (entity.Flags.Solid || entity.Flags.Missile)
                {
                    for (LinkableNode<Entity>? entityNode = block.Entities.Head; entityNode != null;)
                    {
                        Entity nextEntity = entityNode.Value;
                        if (ReferenceEquals(entity, nextEntity))
                        {
                            entityNode = entityNode.Next;
                            continue;
                        }

                        if (nextEntity.Box.Overlaps2D(nextBox))
                        {
                            tryMove.IntersectEntities2D.Add(nextEntity);
                            bool overlapsZ = entity.Box.OverlapsZ(nextEntity.Box);

                            if (overlapsZ && entity.Flags.Pickup && nextEntity.Definition.IsType(EntityDefinitionType.Inventory))
                            {
                                // Set the next node - this pickup can be removed from the list
                                entityNode = entityNode.Next;
                                m_world.PerformItemPickup(entity, nextEntity);
                                continue;
                            }
                            else if (entity.CanBlockEntity(nextEntity) && BlocksEntityZ(entity, nextEntity, tryMove, overlapsZ))
                            {
                                tryMove.Success = false;
                                entity.BlockingEntity = nextEntity;
                                return GridIterationStatus.Stop;
                            }
                        }

                        entityNode = entityNode.Next;
                    }
                }

                return GridIterationStatus.Continue;
            }
        }

        private bool BlocksEntityZ(Entity entity, Entity other, TryMoveData tryMove, bool overlapsZ)
        {
            if (ReferenceEquals(this, other))
                return false;

            if (entity.Position.Z + entity.Height > other.Position.Z)
            {
                // This entity is higher than the other entity and requires step up checking
                m_lineOpening.SetTop(tryMove, other);
            }
            else
            {
                // This entity is within the other entity's Z or below
                m_lineOpening.SetBottom(tryMove, other);
            }

            tryMove.SetIntersectionData(m_lineOpening);

            // If blocking and monster, do not check step passing below. Monsters can't step onto other things.
            if (overlapsZ && entity.Flags.IsMonster)
                return true;

            if (!overlapsZ)
                return false;

            return !m_lineOpening.CanPassOrStepThrough(entity);
        }

        public void MoveTo(Entity entity, Vec2D nextPosition, TryMoveData tryMove)
        {
            entity.UnlinkFromWorld();

            Vec2D previousPosition = entity.Position.XY;
            entity.SetXY(nextPosition);

            LinkToWorld(entity, tryMove);

            for (int i = 0; i < tryMove.IntersectSpecialLines.Count; i++)
                CheckLineSpecialActivation(entity, tryMove.IntersectSpecialLines[i], previousPosition);
        }

        private void CheckLineSpecialActivation(Entity entity, Line line, Vec2D previousPosition)
        {
            if (!m_world.CanActivate(entity, line, ActivationContext.CrossLine))
                return;

            bool fromFront = line.Segment.OnRight(previousPosition);
            if (fromFront != line.Segment.OnRight(entity.Position.XY))
            {
                if (line.Special.IsTeleport() && !fromFront)
                    return;

                m_world.ActivateSpecialLine(entity, line, ActivationContext.CrossLine);
            }
        }

        private void HandleSlide(Entity entity, ref Vec2D stepDelta, ref int movesLeft, TryMoveData tryMove)
        {
            if (FindClosestBlockingLine(entity, stepDelta, out MoveInfo moveInfo) &&
                MoveCloseToBlockingLine(entity, stepDelta, moveInfo, out Vec2D residualStep, tryMove))
            {
                ReorientToSlideAlong(entity, moveInfo.BlockingLine!, residualStep, ref stepDelta, ref movesLeft);
                return;
            }

            if (AttemptAxisMove(entity, stepDelta, Axis2D.Y, tryMove))
                return;
            if (AttemptAxisMove(entity, stepDelta, Axis2D.X, tryMove))
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
            Vec2D position = entity.Position.XY;
            m_blockmap.Iterate(cornerTracer, CheckForTracerHit);

            if (hit && hitTime < moveInfo.LineIntersectionTime)
                moveInfo = MoveInfo.From(blockingLine!, hitTime);

            GridIterationStatus CheckForTracerHit(Block block)
            {
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];

                    if (cornerTracer.Intersection(line.Segment, out double time) &&
                        LineBlocksEntity(entity, position, line, null) != LineBlock.NoBlock &&
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

        private bool MoveCloseToBlockingLine(Entity entity, Vec2D stepDelta, MoveInfo moveInfo, out Vec2D residualStep, TryMoveData tryMove)
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

            Vec2D closeToLinePosition = entity.Position.XY + usedStepDelta;
            if (IsPositionValid(entity, closeToLinePosition, tryMove))
            {
                MoveTo(entity, closeToLinePosition, tryMove);
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

        private bool AttemptAxisMove(Entity entity, Vec2D stepDelta, Axis2D axis, TryMoveData tryMove)
        {
            if (axis == Axis2D.X)
            {
                Vec2D nextPosition = entity.Position.XY + new Vec2D(stepDelta.X, 0);
                if (IsPositionValid(entity, nextPosition, tryMove))
                {
                    MoveTo(entity, nextPosition, tryMove);
                    entity.Velocity.Y = 0;
                    stepDelta.Y = 0;
                    return true;
                }
            }
            else
            {
                Vec2D nextPosition = entity.Position.XY + new Vec2D(0, stepDelta.Y);
                if (IsPositionValid(entity, nextPosition, tryMove))
                {
                    MoveTo(entity, nextPosition, tryMove);
                    entity.Velocity.X = 0;
                    stepDelta.X = 0;
                    return true;
                }
            }

            return false;
        }

        private void MoveXY(Entity entity)
        {
            if (entity.Velocity.XY == Vec2D.Zero)
                return;

            TryMoveData tryMove = TryMoveXY(entity, (entity.Position + entity.Velocity).XY);
            if (!tryMove.Success)
                m_world.HandleEntityHit(entity, entity.Velocity, tryMove);
            if (entity.ShouldApplyFriction())
                ApplyFriction(entity);
            StopXYMovementIfSmall(entity);
        }

        private void MoveZ(Entity entity)
        {
            if (m_world.WorldState == WorldState.Exit)
                return;

            if (entity.Flags.NoGravity && entity.ShouldApplyFriction())
                entity.Velocity.Z *= Friction;
            if (entity.ShouldApplyGravity())
                entity.Velocity.Z -= m_world.Gravity;

            double floatZ = entity.GetEnemyFloatMove();
            if (entity.Velocity.Z == 0 && floatZ == 0)
                return;

            Vec3D previousVelocity = entity.Velocity;
            double newZ = entity.Position.Z + entity.Velocity.Z + floatZ;
            entity.SetZ(newZ, false);

            // Passing MoveLinked emulates some vanilla functionality where things are not checked against linked sectors when they haven't moved
            ClampBetweenFloorAndCeiling(entity, entity.MoveLinked);

            if (entity.IsBlocked())
                m_world.HandleEntityHit(entity, previousVelocity, null);

            if (entity.OverEntity != null)
                HandleStackedEntityPhysics(entity);
        }
    }
}