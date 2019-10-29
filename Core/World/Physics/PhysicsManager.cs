using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Boxes;
using Helion.Util.Geometry.Segments;
using Helion.Util.Geometry.Vectors;
using Helion.Util.RandomGenerators;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using Helion.World.Special.SectorMovement;
using NLog;
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
        private const double Gravity = 1.0;
        private const double Friction = 0.90625;
        private const double SlideStepBackTime = 1.0 / 32.0;
        private const double MinMovementThreshold = 0.06;
        private const double EntityUseDistance = 64.0; // TODO: Remove when we get decorate!
        private const double SetEntityToFloorSpeedMax = 9;
        private const double MaxPitch = 80.0 * Math.PI / 180.0;
        private const double MinPitch = -80.0 * Math.PI / 180.0;

        public static readonly double LowestPossibleZ = Fixed.Lowest().ToDouble();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly BspTree m_bspTree;
        private readonly BlockMap m_blockmap;
        private readonly SoundManager m_soundManager;
        private readonly EntityManager m_entityManager;
        private readonly BlockmapTraverser m_blockmapTraverser;

        private LineOpening m_lineOpening = new LineOpening();
        private IRandom m_random;

        private DateTime m_shootTest = DateTime.Now;

        /// <summary>
        /// Fires when an entity activates a line special with use or by crossing a line.
        /// </summary>
        public event EventHandler<EntityActivateSpecialEventArgs>? EntityActivatedSpecial;

        /// <summary>
        /// Fires when the player executes the use command but hits a non-special blocking line.
        /// </summary>
        public event EventHandler<Entity>? PlayerUseFail;

        /// <summary>
        /// Creates a new physics manager which utilizes the arguments for any
        /// collision detection or linking to the world.
        /// </summary>
        /// <param name="bspTree">The BSP tree for the world.</param>
        /// <param name="blockmap">The blockmap for the world.</param>
        /// <param name="soundManager">The sound manager to play sounds from.</param>
        /// <param name="entityManager">entity manager.</param>
        /// <param name="random">Random number generator to use.</param>
        public PhysicsManager(BspTree bspTree, BlockMap blockmap, SoundManager soundManager, EntityManager entityManager, IRandom random)
        {
            m_bspTree = bspTree;
            m_blockmap = blockmap;
            m_soundManager = soundManager;
            m_entityManager = entityManager;
            m_random = random;
            m_blockmapTraverser = new BlockmapTraverser(m_blockmap);
        }

        /// <summary>
        /// Links an entity to the world.
        /// </summary>
        /// <param name="entity">The entity to link.</param>
        public void LinkToWorld(Entity entity)
        {
            if (!entity.Flags.NoBlockmap)
                m_blockmap.Link(entity);
            
            LinkToSectorsAndEntities(entity);
            
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

        public SectorMoveStatus MoveSectorZ(Sector sector, SectorPlane sectorPlane, SectorMoveType moveType, 
            MoveDirection direction, double speed, double destZ, CrushData? crush)
        {
            // Save the Z value because we are only checking if the dest is valid
            // If the move is invalid because of a blocking entity then it will not be set to destZ
            SectorMoveStatus status = SectorMoveStatus.Success;
            double startZ = sectorPlane.Z;
            sectorPlane.PrevZ = startZ;
            sectorPlane.Z = destZ;
            sectorPlane.Plane?.MoveZ(destZ - startZ);

            // Move lower entities first to handle stacked entities
            var entities = sector.Entities.OrderBy(x => x.Box.Bottom).ToList();

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                entity.SaveZ = entity.Position.Z;

                // At slower speeds we need to set entities to the floor
                // Otherwise the player will fall and hit the floor repeatedly creating a weird bouncing effect
                if (moveType == SectorMoveType.Floor && direction == MoveDirection.Down && -speed < SetEntityToFloorSpeedMax &&
                    entity.OnGround && !entity.IsFlying && entity.HighestFloorSector == sector)
                {
                    if (entity.OnEntity == null)
                        entity.SetZ(destZ, false);
                    else
                        entity.SetZ(entity.OnEntity.Box.Top, false);
                }

                ClampBetweenFloorAndCeiling(entity);
            }

            for (int i = 0; i < entities.Count; i++)
            {
                Entity entity = entities[i];
                SetEntityBoundsZ(entity);

                if ((moveType == SectorMoveType.Ceiling && direction == MoveDirection.Up) || (moveType == SectorMoveType.Floor && direction == MoveDirection.Down))
                    continue;

                if (!entity.Flags.CanPass || !entity.Flags.Solid)
                    continue;

                double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;

                if (thingZ + entity.Height > entity.LowestCeilingZ)
                {
                    if (crush != null)
                    {
                        status = SectorMoveStatus.Crush;
                        if (CrusherShouldContinue(status, crush))
                            continue;
                    }

                    // Set the sector Z to the difference of the blocked height
                    double diff = Math.Abs(startZ - destZ) - (thingZ + entity.Height - entity.LowestCeilingZ);
                    if (speed < 0)
                        diff = -diff;

                    sectorPlane.Z = startZ + diff;
                    sectorPlane.Plane?.MoveZ(startZ - destZ + diff);

                    // Entity blocked movement, reset all entities in moving sector after resetting sector Z
                    foreach (var relinkEntity in entities)
                    {
                        relinkEntity.UnlinkFromWorld();
                        relinkEntity.SetZ(relinkEntity.SaveZ + diff, false);
                        LinkToWorld(relinkEntity);
                    }

                    return SectorMoveStatus.Blocked;
                }
            }

            return status;
        }

        private static bool CrusherShouldContinue(SectorMoveStatus status, CrushData? crush)
        {
            return crush != null && 
                   crush.CrushMode == ZDoomCrushMode.DoomWithSlowDown &&
                   status == SectorMoveStatus.Crush;
        }

        /// <summary>
        /// Executes use logic on the entity. EntityUseActivated event will
        /// fire if the entity activates a line special or is in range to hit
        /// a blocking line. PlayerUseFail will fire if the entity is a player
        /// and we hit a block line but didn't activate a special.
        /// </summary>
        /// <remarks>
        /// If the line has a special and we are hitting the front then we
        /// can use it (player Z does not apply here). If there's a LineOpening
        /// with OpeningHeight less than or equal to 0, it's a closed sector.
        /// The special line behind it cannot activate until the sector has an
        /// opening.
        /// </remarks>
        /// <param name="entity">The entity to execute use.</param>
        public void EntityUse(Entity entity)
        {
            Line? activateLine = null;
            bool hitBlockLine = false;
            Vec2D start = entity.Position.To2D();
            Vec2D end = new Vec2D(start.X + (Math.Cos(entity.AngleRadians) * EntityUseDistance), start.Y + (Math.Sin(entity.AngleRadians) * EntityUseDistance));
            List<BlockmapIntersect> intersections = m_blockmapTraverser.GetBlockmapIntersections(new Seg2D(start, end), BlockmapTraverseFlags.Lines);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];
                if (bi.Line != null && bi.Line.Segment.OnRight(start))
                {
                    if (bi.Line.HasSpecial)
                    {
                        activateLine = bi.Line;
                        break;
                    }

                    if (bi.Line.Back == null)
                    {
                        hitBlockLine = true;
                        break;
                    }

                    LineOpening opening = GetLineOpening(bi.Line.Front.Sector, bi.Line.Back.Sector);
                    if (opening.OpeningHeight <= 0)
                    {
                        hitBlockLine = true;
                        break;
                    }

                    // Keep checking if hit two-sided blocking line - this way the PlayerUserFail will be raised if no line special is hit
                    if (!opening.CanPassOrStepThrough(entity))
                        hitBlockLine = true;
                }
            }

            if (activateLine != null && activateLine.Special.CanActivate(entity, activateLine.Flags, ActivationContext.UseLine))
            {
                var args = new EntityActivateSpecialEventArgs(ActivationContext.UseLine, entity, activateLine);
                EntityActivatedSpecial?.Invoke(this, args);
            }
            else if (hitBlockLine && entity is Player)
            {
                PlayerUseFail?.Invoke(this, entity);
            }
        }

        public void FireProjectile(Entity shooter, double pitch, double distance, bool autoAim, string projectClassName)
        {
            if (DateTime.Now.Subtract(m_shootTest).TotalMilliseconds < 500)
                return;

            m_shootTest = DateTime.Now;

            if (autoAim)
            {
                Vec3D start = shooter.AttackPosition;
                Vec3D end = start + Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, distance);
                if (GetAutoAimAngle(shooter, start, end, out double autoAimPitch))
                    pitch = autoAimPitch;
            }

            var projectile = m_entityManager.DefinitionComposer.GetByName(projectClassName);
            if (projectile != null)
            {
                // TODO doom has a special wall explosion check
                Entity rocket = m_entityManager.Create(projectile, shooter.AttackPosition, shooter.AngleRadians, 0);
                rocket.Owner = shooter;
                rocket.Velocity = Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, rocket.Definition.Properties.Speed);
            }
        }

        public void FireHitscanBullets(Entity shooter, int bulletCount, double spreadAngleRadians, double spreadPitchRadians, double pitch, double distance, bool autoAim)
        {
            if (DateTime.Now.Subtract(m_shootTest).TotalMilliseconds < 200)
                return;

            m_shootTest = DateTime.Now;

            if (autoAim)
            {
                Vec3D start = shooter.AttackPosition;
                Vec3D end = start + Vec3D.UnitTimesValue(shooter.AngleRadians, pitch, distance);
                if (GetAutoAimAngle(shooter, start, end, out double autoAimPitch))
                    pitch = autoAimPitch;
            }

            if (!shooter.Refire && bulletCount == 1)
            {
                int damage = 5 * ((m_random.NextByte() % 3) + 1);
                FireHitscan(shooter, shooter.AngleRadians, pitch, distance, damage);
            }
            else
            {
                for (int i = 0; i < bulletCount; i++)
                {
                    int damage = 5 * ((m_random.NextByte() % 3) + 1);
                    double angle = shooter.AngleRadians + (m_random.NextDiff() * spreadAngleRadians / 255);
                    double newPitch = pitch + (m_random.NextDiff() * spreadPitchRadians / 255);
                    FireHitscan(shooter, angle, newPitch, distance, damage);
                }
            }
        }

        public void FireHitscan(Entity shooter, double angle, double pitch, double distance, int damage)
        {
            Vec3D start = shooter.AttackPosition;
            Vec3D end = start + Vec3D.UnitTimesValue(angle, pitch, distance);
            Vec3D intersect = new Vec3D(0, 0, 0);

            BlockmapIntersect? bi = FireHitScan(shooter, start, end, pitch, ref intersect);

            if (bi != null)
            {
                Line? line = bi.Value.Line;
                if (line != null && line.HasSpecial && line.Special.CanActivate(shooter, line.Flags, ActivationContext.ProjectileHitLine))
                {
                    var args = new EntityActivateSpecialEventArgs(ActivationContext.ProjectileHitLine, shooter, line);
                    EntityActivatedSpecial?.Invoke(this, args);
                }

                // Only move closer on a line hit
                if (bi.Value.Entity == null && bi.Value.Sector == null)
                    MoveIntersectCloser(start, ref intersect, angle, bi.Value.Distance2D);
                DebugHitscanTest(bi.Value, intersect);

                if (bi.Value.Entity != null)
                    DamageEntity(bi.Value.Entity, shooter, damage);
            }
        }

        public BlockmapIntersect? FireHitScan(Entity shooter, Vec3D start, Vec3D end, double pitch, ref Vec3D intersect)
        {
            double floorZ, ceilingZ;
            Seg2D seg = new Seg2D(start.To2D(), end.To2D());
            List<BlockmapIntersect> intersections = m_blockmapTraverser.GetBlockmapIntersections(seg, 
                BlockmapTraverseFlags.Entities | BlockmapTraverseFlags.Lines, 
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];

                if (bi.Line != null)
                {
                    intersect = bi.Intersection.To3D(start.Z + (Math.Tan(pitch) * bi.Distance2D));

                    if (bi.Line.Back == null)
                    {
                        floorZ = bi.Line.Front.Sector.ToFloorZ(intersect);
                        ceilingZ = bi.Line.Front.Sector.ToCeilingZ(intersect);

                        if (intersect.Z > floorZ && intersect.Z < ceilingZ)
                            return bi;

                        if (IsSkyClipOneSided(bi.Line.Front.Sector, floorZ, ceilingZ, intersect))
                            return null;

                        GetSectorPlaneIntersection(start, end, bi.Line.Front.Sector, floorZ, ceilingZ, ref intersect);
                        bi.Sector = bi.Line.Front.Sector;
                        return bi;
                    }

                    GetOrderedSectors(bi, start, out Sector front, out Sector back);
                    if (IsSkyClipTwoSided(front, back, intersect))
                        return null;

                    floorZ = front.ToFloorZ(intersect);
                    ceilingZ = front.ToCeilingZ(intersect);

                    if (intersect.Z < floorZ || intersect.Z > ceilingZ)
                    {
                        GetSectorPlaneIntersection(start, end, front, floorZ, ceilingZ, ref intersect);
                        bi.Sector = front;
                        return bi;
                    }

                    LineOpening opening = GetLineOpening(bi.Line.Front.Sector, bi.Line.Back.Sector);
                    if ((opening.FloorZ > intersect.Z && intersect.Z > floorZ) || (opening.CeilingZ < intersect.Z && intersect.Z < ceilingZ))
                        return bi;
                }
                else if (bi.Entity != null && !ReferenceEquals(shooter, bi.Entity) && bi.Entity.Box.Intersects(start, end, ref intersect))
                {
                    return bi;
                }
            }

            return null;
        }

        private static void GetSectorPlaneIntersection(in Vec3D start, in Vec3D end, Sector sector, double floorZ, double ceilingZ, ref Vec3D intersect)
        {
            if (intersect.Z < floorZ)
            {
                sector.Floor.Plane.Intersects(start, end, ref intersect);
                intersect.Z = sector.ToFloorZ(intersect);
            }
            else if (intersect.Z > ceilingZ)
            {
                sector.Ceiling.Plane.Intersects(start, end, ref intersect);
                intersect.Z = sector.ToCeilingZ(intersect) - 4;
            }
        }

        private static void GetOrderedSectors(BlockmapIntersect bi, in Vec3D start, out Sector front, out Sector back)
        {
            if (bi.Line!.Segment.OnRight(start))
            {
                front = bi.Line.Front.Sector;
                back = bi.Line.Back!.Sector;
            }
            else
            {
                front = bi.Line.Back!.Sector;
                back = bi.Line.Front.Sector;
            }
        }

        private void DamageEntity(Entity target, Entity source, int damage)
        {
            if (!target.Flags.Shootable)
                return;

            double angle = source.Position.Angle(target.Position);
            double thrust = damage * source.Definition.Properties.ProjectileKickBack * 0.125 / target.Properties.Mass;

            if (damage < 40 && damage > target.Health &&
                target.Position.Z - source.Position.Z > 64 && (m_random.NextByte() & 1) != 0)
            {
                angle += Math.PI;
                thrust *= 4;
            }

            target.Velocity += Vec3D.UnitTimesValue(angle, 0.0, thrust);
            target.Health -= damage;

            if (target.Health <= 0)
            {
                SetEntityDeath(target);
            }
            else
            {
                if (m_random.NextByte() < target.Properties.PainChance)
                    target.SetPainState();
            }
        }

        private void SetEntityDeath(Entity entity)
        {
            if (entity.Health < -entity.Properties.Health && entity.HasXDeathState())
                entity.SetXDeathState();
            else
                entity.SetDeathState();

            entity.Health = 0;
            entity.Height = entity.Definition.Properties.Height / 4.0;
        }

        public void RadiusExplosion(Entity source, int radius)
        {
            // Barrels can't use ZDoom physics - TODO better way to check?
            bool zdoomPhysics = source.Definition.Name != "ExplosiveBarrel";
            Vec2D pos2D = source.Position.To2D();
            Vec2D radius2D = new Vec2D(radius, radius);
            Box2D explosionBox = new Box2D(pos2D - radius2D, pos2D + radius2D);

            List<BlockmapIntersect> intersections = m_blockmapTraverser.GetBlockmapIntersections(explosionBox);
            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];
                if (bi.Entity != null && CheckLineOfSight(source, bi.Entity))
                    ApplyExplosionDamageAndThrust(source, bi.Entity, radius, zdoomPhysics);
            }
        }

        private void ApplyExplosionDamageAndThrust(Entity source, Entity entity, double radius, bool zdoomPhysics)
        {
            double dx = Math.Abs(entity.Position.X - source.Position.X);
            double dy = Math.Abs(entity.Position.Y - source.Position.Y);
            double distance = Math.Max(dx, dy);

            if (zdoomPhysics)
            {
                if (source.Position.Z < entity.Position.Z || source.Position.Z >= entity.Box.Top)
                {
                    double dz;
                    if (source.Position.Z > entity.Position.Z)
                        dz = entity.Box.Top - source.Position.Z;
                    else
                        dz = entity.Position.Z - source.Position.Z;

                    if (distance <= entity.Radius)
                        distance = dz;
                    else
                    {
                        distance -= entity.Radius;
                        distance = Math.Sqrt((distance * distance) + (dz * dz));
                    }
                }
                else
                {
                    distance -= entity.Radius;
                    if (distance < 0.0f)
                        distance = 0.0f;
                }
            }
            else
            {
                distance -= entity.Radius;
            }

            int damage = (int)(radius - distance);

            if (damage <= 0)
                return;

            DamageEntity(entity, source, damage);

            if (zdoomPhysics)
            {
                // ZDoom applies extra x/y thrust (because reasons?) and incredibly janky z thrust
                double angle = source.Position.Angle(entity.Position);
                double thrust = damage * 0.5 / entity.Properties.Mass;
                double thrustZ = (entity.Position.Z + (entity.Height / 2.0) - source.Position.Z) * thrust;

                thrustZ *= (entity == source.Owner ? 0.8 : 0.5);

                entity.Velocity.X += Math.Cos(angle) * thrust;
                entity.Velocity.Y += Math.Sin(angle) * thrust;
                entity.Velocity.Z += thrustZ;
            }
        }

        private bool CheckLineOfSight(Entity entity, Entity other)
        {
            Vec2D start = entity.Position.To2D();
            Vec2D end = other.Position.To2D();

            if (start == end)
                return true;

            Seg2D seg = new Seg2D(start, end);
            double distance2D = start.Distance(end);
            double topPitch = entity.Position.Pitch(other.Position.Z + other.Height, distance2D);
            double bottomPitch = entity.Position.Pitch(other.Position.Z, distance2D);

            List<BlockmapIntersect> intersections = m_blockmapTraverser.GetBlockmapIntersections(seg, BlockmapTraverseFlags.Lines);
            return GetBlockmapTraversalPitch(intersections, entity.Position, entity, topPitch, bottomPitch, out _) != TraversalPitchStatus.Blocked;
        }

        private bool IsSkyClipOneSided(Sector sector, double ceilingZ, double floorZ, in Vec3D intersect)
        {
            if (intersect.Z > ceilingZ && TextureManager.Instance.IsSkyTexture(sector.Ceiling.TextureHandle))
                return true;
            else if (intersect.Z < floorZ && TextureManager.Instance.IsSkyTexture(sector.Floor.TextureHandle))
                return true;

            return false;
        }

        private bool IsSkyClipTwoSided(Sector front, Sector back, in Vec3D intersect)
        {
            bool isFrontCeilingSky = TextureManager.Instance.IsSkyTexture(front.Ceiling.TextureHandle);
            bool isBackCeilingSky = TextureManager.Instance.IsSkyTexture(back.Ceiling.TextureHandle);

            if (isFrontCeilingSky && isBackCeilingSky && intersect.Z > back.ToCeilingZ(intersect))
                return true;

            if (isFrontCeilingSky && intersect.Z > front.ToCeilingZ(intersect))
                return true;

            if (TextureManager.Instance.IsSkyTexture(front.Floor.TextureHandle) && intersect.Z < front.ToFloorZ(intersect))
                return true;

            return false;
        }

        public bool GetAutoAimAngle(Entity shooter, Vec3D start, Vec3D end, out double pitch)
        {
            Seg2D seg = new Seg2D(start.To2D(), end.To2D());

            List<BlockmapIntersect> intersections = m_blockmapTraverser.GetBlockmapIntersections(seg, 
                BlockmapTraverseFlags.Entities | BlockmapTraverseFlags.Lines,
                BlockmapTraverseEntityFlags.Shootable | BlockmapTraverseEntityFlags.Solid);

            return GetBlockmapTraversalPitch(intersections, start, shooter, MaxPitch, MinPitch, out pitch) == TraversalPitchStatus.PitchSet;
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
        
        private static bool EntityCanBlockEntity(Entity entity, Entity other)
        {
            if (ReferenceEquals(entity, other) || entity.Owner == other || !other.Flags.Solid)
                return false;
            return other.Flags.Solid;
        }
        
        private static bool EntityBlocksEntityZ(Entity entity, Entity other)
        {
            double maxStepHeight = entity is Player ? entity.Properties.MaxStepHeight : 0.0;
            return other.Box.Top - entity.Box.Bottom > maxStepHeight || 
                   entity.LowestCeilingZ - other.Box.Top < entity.Height;
        }
        
        private static bool PreviouslyClipped(Entity entity, Entity other)
        {
            // TODO: Can we get around making 2 new boxes here?
            EntityBox box = new EntityBox(entity.PrevPosition, entity.Radius, entity.Height);
            EntityBox otherBox = new EntityBox(other.PrevPosition, other.Radius, other.Height);
            return box.Overlaps(otherBox);
        }

        private enum TraversalPitchStatus
        {
            Blocked,
            PitchSet,
            PitchNotSet,
        }

        private TraversalPitchStatus GetBlockmapTraversalPitch(List<BlockmapIntersect> intersections, Vec3D start, Entity startEntity, double topPitch, double bottomPitch, out double pitch)
        {
            pitch = 0.0;

            for (int i = 0; i < intersections.Count; i++)
            {
                BlockmapIntersect bi = intersections[i];

                if (bi.Line != null)
                {
                    if (bi.Line.Back == null)
                        return TraversalPitchStatus.Blocked;

                    LineOpening opening = GetLineOpening(bi.Line.Front.Sector, bi.Line.Back.Sector);
                    if (opening.FloorZ < opening.CeilingZ)
                    {
                        double sectorPitch = start.Pitch(opening.FloorZ, bi.Distance2D);
                        if (sectorPitch > bottomPitch)
                            bottomPitch = sectorPitch;

                        sectorPitch = start.Pitch(opening.CeilingZ, bi.Distance2D);
                        if (sectorPitch < topPitch)
                            topPitch = sectorPitch;

                        if (topPitch <= bottomPitch)
                            return TraversalPitchStatus.Blocked;
                    }
                    else
                    {
                        return TraversalPitchStatus.Blocked;
                    }
                }
                else if (bi.Entity != null && !ReferenceEquals(startEntity, bi.Entity))
                {
                    double thingTopPitch = start.Pitch(bi.Entity.Box.Max.Z, bi.Distance2D);
                    double thingBottomPitch = start.Pitch(bi.Entity.Box.Min.Z, bi.Distance2D);

                    if (thingBottomPitch > topPitch)
                        return TraversalPitchStatus.Blocked;
                    if (thingTopPitch < bottomPitch)
                        return TraversalPitchStatus.Blocked;

                    if (thingTopPitch < topPitch)
                        topPitch = thingTopPitch;
                    if (thingBottomPitch > bottomPitch)
                        bottomPitch = thingBottomPitch;

                    pitch = (bottomPitch + topPitch) / 2.0;
                    return TraversalPitchStatus.PitchSet;
                }
            }

            return TraversalPitchStatus.PitchNotSet;
        }

        private bool LineBlocksEntity(Entity entity, Line line)
        {
            if (line.BlocksEntity(entity))
                return true;
            if (line.Back == null)
                return false;

            LineOpening opening = GetLineOpening(line.Front.Sector, line.Back.Sector);
            return !opening.CanPassOrStepThrough(entity);
        }

        private void DebugHitscanTest(in BlockmapIntersect bi, Vec3D intersect)
        {
            string className = bi.Entity == null || (bi.Entity != null && bi.Entity.Definition.Flags.NoBlood) ? "BulletPuff" : "Blood";
            CreateTestPuff(intersect, className);
        }

        private void CreateTestPuff(Vec3D intersect, string className)
        {
            var puff = m_entityManager.DefinitionComposer.GetByName(className);
            if (puff != null)
                m_entityManager.Create(puff, intersect, 0.0, 0);
        }

        private void MoveIntersectCloser(in Vec3D start, ref Vec3D intersect, double angle, double distXY)
        {
            distXY -= 2.0;
            intersect.X = start.X + (Math.Cos(angle) * distXY);
            intersect.Y = start.Y + (Math.Sin(angle) * distXY);
        }

        private LineOpening GetLineOpening(Sector front, Sector back)
        {
            m_lineOpening.Set(front, back);
            return m_lineOpening;
        }

        private void SetEntityOnFloorOrEntity(Entity entity, double floorZ, bool smoothZ)
        {
            if (!entity.OnGround && entity is Player player)
                player.SetHitZ(IsHardHitZ(entity));
            
            Entity? lastOnEntity = entity.OnEntity;
            entity.OnEntity = entity.IntersectEntities.FirstOrDefault(x => x.Box.Top == entity.Box.Bottom);

            // Additionally check to smooth camera when stepping up to an entity
            entity.SetZ(floorZ, smoothZ || lastOnEntity != entity.OnEntity);
            entity.OnGround = true;

            // For now we remove any negative velocity. If upward velocity is
            // reset to zero then the jump we apply to players is lost and they
            // can never jump. Maybe we want to fix this in the future by doing
            // application of jumping after the XY movement instead of before?
            entity.Velocity.Z = Math.Max(0, entity.Velocity.Z);
        }
        
        private bool IsHardHitZ(Entity entity) => entity.Velocity.Z < -(Gravity * 8);

        private void ClampBetweenFloorAndCeiling(Entity entity)
        {
            // TODO fixme
            if (entity.Definition.Name == "BulletPuff")
                return;
            if (entity.NoClip && entity.IsFlying)
                return;

            bool clipped = false;

            Sector? lastSector = entity.HighestFloorSector;
            SetEntityBoundsZ(entity);
            bool smoothZ = lastSector != entity.HighestFloorSector;

            double lowestCeil = entity.LowestCeilingZ;
            double highestFloor = entity.HighestFloorZ;

            if (entity.Box.Top > lowestCeil)
            {
                clipped = true;
                entity.SetZ(lowestCeil - entity.Height, smoothZ);
                entity.Velocity.Z = 0;
            }

            if (entity.Box.Bottom <= highestFloor)
            {
                clipped = true;
                SetEntityOnFloorOrEntity(entity, highestFloor, smoothZ);
            }
            else
            {
                entity.OnGround = false;
            }

            if (clipped)
                HandleEntityHit(entity);
        }

        private void SetEntityBoundsZ(Entity entity)
        {
            Sector highestFloor = entity.Sector;
            Sector lowestCeiling = entity.Sector;
            double highestFloorZ = highestFloor.ToFloorZ(entity.Position);
            double lowestCeilZ = lowestCeiling.ToCeilingZ(entity.Position);
            
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

            entity.OnEntity = null;

            foreach (Entity intersectEntity in entity.IntersectEntities)
            {
                // Check if we are stuck inside this entity and skip because it
                // is invalid for setting floor/ceiling.
                if (PreviouslyClipped(entity, intersectEntity))
                    continue;

                bool above = entity.PrevPosition.Z >= intersectEntity.Box.Top;
                bool below = entity.PrevPosition.Z < intersectEntity.Box.Bottom;
                bool clipped = false;
                if (above && entity.Box.Bottom < intersectEntity.Box.Top)
                    clipped = true;
                else if (below && entity.Box.Top > intersectEntity.Box.Bottom)
                    clipped = true;

                if (above)
                {
                    // Need to check clipping coming from above, if we're above
                    // or clipped through then this is our floor.
                    if ((clipped || entity.Box.Bottom >= intersectEntity.Box.Top) && intersectEntity.Box.Top > highestFloorZ)
                        highestFloorZ = intersectEntity.Box.Top;
                }
                else if (below)
                {
                    // Same check as above but checking clipping the ceiling.
                    if ((clipped || entity.Box.Top <= intersectEntity.Box.Bottom) && intersectEntity.Box.Bottom < lowestCeilZ)
                        lowestCeilZ = intersectEntity.Box.Bottom;
                }

                // Need to check if we can step up to this floor.
                if (entity.Box.Bottom + entity.Properties.MaxStepHeight >= intersectEntity.Box.Top && intersectEntity.Box.Top > highestFloorZ)
                    highestFloorZ = intersectEntity.Box.Top;
            }

            entity.HighestFloorZ = highestFloorZ;
            entity.LowestCeilingZ = lowestCeilZ;
            entity.HighestFloorSector = highestFloor;
            entity.LowestCeilingSector = lowestCeiling;
        }

        private void LinkToSectorsAndEntities(Entity entity)
        {
            Precondition(entity.SectorNodes.Empty(), "Forgot to unlink entity from blockmap");
            
            Subsector centerSubsector = m_bspTree.ToSubsector(entity.Position.To2D());
            Sector centerSector = centerSubsector.Sector;
            HashSet<Sector> sectors = new HashSet<Sector> { centerSector };
            HashSet<Entity> entities = new HashSet<Entity>();
            HashSet<Subsector> subsectors = new HashSet<Subsector> { centerSubsector };
            
            // TODO: Can we replace this by iterating over the blocks were already in?
            Box2D box = entity.Box.To2D();
            m_blockmap.Iterate(box, EntitySectorOverlapFinder);

            entity.Sector = centerSector;
            entity.IntersectSectors = sectors.ToList();
            entity.IntersectEntities = entities.ToList();
            entity.IntersectSubsectors = subsectors.ToList();

            if (!entity.Flags.NoSector && !entity.NoClip)
            {
                for (int i = 0; i < entity.IntersectSectors.Count; i++)
                    entity.SectorNodes.Add(entity.IntersectSectors[i].Link(entity));
                for (int i = 0; i < entity.IntersectSubsectors.Count; i++)
                    entity.SubsectorNodes.Add(entity.IntersectSubsectors[i].Link(entity));
            }

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
                            if (line.HasSpecial && !FindLine(entity.IntersectSpecialLines, line.Id))
                                entity.IntersectSpecialLines.Add(line);
                        }
                        
                        foreach (Subsector subsector in line.Subsectors)
                            subsectors.Add(subsector);
                        
                        sectors.Add(line.Front.Sector);
                        if (line.Back != null)
                            sectors.Add(line.Back.Sector);
                    }
                }

                LinkableNode<Entity>? entityNode = block.Entities.Head;
                while (entityNode != null)
                {
                    Entity nextEntity = entityNode.Value;
                    if (EntityCanBlockEntity(entity, nextEntity) && nextEntity.Box.Overlaps2D(entity.Box))
                        entities.Add(nextEntity);

                    entityNode = entityNode.Next;
                }
                
                return GridIterationStatus.Continue;
            }
        }

        private bool FindLine(List<Line> lines, int id)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Id == id)
                    return true;
            }

            return false;
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

            // TODO: Temporary until we know how this actually works.
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

                if (entity.Flags.SlidesOnWalls && slidesLeft > 0)
                {
                    HandleSlide(entity, ref stepDelta, ref movesLeft);
                    slidesLeft--;
                    continue;
                }

                HandleEntityHit(entity);
                ClearVelocityXY(entity);
                break;
            }
        }

        private void HandleEntityHit(Entity entity)
        {
            if (entity.Flags.Missile)
            {
                if (entity.BlockingEntity != null && entity.Properties.Damage > 0)
                {
                    int damage = ((m_random.NextByte() % 8) + 1) * entity.Properties.Damage;
                    DamageEntity(entity.BlockingEntity, entity, damage);
                }

                SetEntityDeath(entity);
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
            Box2D nextBox = Box2D.CopyToOffset(nextPosition, entity.Radius);
            return !m_blockmap.Iterate(nextBox, CheckForBlockers);

            GridIterationStatus CheckForBlockers(Block block)
            {
                for (int i = 0; i < block.Lines.Count; i++)
                {
                    Line line = block.Lines[i];
                    if (line.Segment.Intersects(nextBox) && LineBlocksEntity(entity, line))
                        return GridIterationStatus.Stop;
                }

                if (entity.Flags.Solid || entity.Flags.Missile)
                {
                    LinkableNode<Entity>? entityNode = block.Entities.Head;
                    while (entityNode != null)
                    {
                        Entity nextEntity = entityNode.Value;

                        if (EntityCanBlockEntity(entity, nextEntity) && nextEntity.Box.Overlaps2D(nextBox) &&
                            entity.Box.OverlapsZ(nextEntity.Box))
                        {
                            if (EntityBlocksEntityZ(entity, nextEntity))
                            {
                                entity.BlockingEntity = nextEntity;
                                return GridIterationStatus.Stop;
                            }

                            entity.IntersectEntities.Add(nextEntity);
                        }

                        entityNode = entityNode.Next;
                    }
                }
                
                return GridIterationStatus.Continue;
            }
        }

        private void MoveTo(Entity entity, Vec2D nextPosition)
        {
            entity.UnlinkFromWorld();

            Vec2D previousPosition = entity.Position.To2D();
            entity.SetXY(nextPosition);

            LinkToWorld(entity);

            for (int i = 0; i < entity.IntersectSpecialLines.Count; i++)
                CheckLineSpecialActivation(entity, entity.IntersectSpecialLines[i], previousPosition);
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

                EntityActivateSpecialEventArgs args = new EntityActivateSpecialEventArgs(
                    ActivationContext.CrossLine, entity, line);
                EntityActivatedSpecial?.Invoke(this, args);
            }
        }

        private void HandleSlide(Entity entity, ref Vec2D stepDelta, ref int movesLeft)
        {
            if (FindClosestBlockingLine(entity, stepDelta, out MoveInfo moveInfo))
            {
                if (MoveCloseToBlockingLine(entity, stepDelta, moveInfo, out Vec2D residualStep))
                {
                    ReorientToSlideAlong(entity, moveInfo.BlockingLine!, residualStep, ref stepDelta, ref movesLeft);
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
                moveInfo = MoveInfo.From(blockingLine!, hitTime);

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
            if (entity.IsFlying)
                entity.Velocity.Z *= Friction;
            else if (!entity.OnGround && !entity.Flags.NoGravity)
                entity.Velocity.Z -= Gravity;

            if (entity.Velocity.Z == 0)
                return;

            entity.SetZ(entity.Position.Z + entity.Velocity.Z, false);
            ClampBetweenFloorAndCeiling(entity);
        }
    }
}