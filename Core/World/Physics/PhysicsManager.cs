using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Grids;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.RandomGenerators;
using Helion.World.Blockmap;
using Helion.World.Bsp;
using Helion.World.Entities;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics.Blockmap;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Physics;

readonly record struct SectorMoveEntityData(Entity Entity, double SaveZ, double PrevSaveZ, bool WasCrushing);

/// <summary>
/// Responsible for handling all the physics and collision detection in a
/// world.
/// </summary>
public sealed class PhysicsManager
{
    private const int MaxSlides = 3;
    private const double SlideStepBackTime = 1.0 / 32.0;
    private const double MinMovementThreshold = 0.01;
    private const double SetEntityToFloorSpeedMax = 8;
    private const double MinMoveFactor = 32 / 65536.0;
    private const double DefaultMoveFactor = 1.0;
    private const double MudMoveFactorLow = 15000 / 65536.0;
    private const double MudMoveFactorMed = MudMoveFactorLow * 2;
    private const double MudMoveFactorHigh = MudMoveFactorLow * 4;

    public const double MaxMoveXY = 30;
    public static readonly double LowestPossibleZ = Fixed.Lowest().ToDouble();

    public BlockmapTraverser BlockmapTraverser;
    public TryMoveData TryMoveData = new();
    public bool EnableMaxMoveXY = true;

    private IWorld m_world;
    private CompactBspTree m_bspTree;
    private BlockMap m_blockmap;
    private UniformGrid<Block> m_blockmapGrid;
    private Block[] m_blockmapBlocks;
    private EntityManager m_entityManager;
    private IRandom m_random;
    private int[] m_checkedBlockLines;
    private bool m_alwaysStickEntitiesToFloor;
    private readonly LineOpening m_lineOpening = new();
    private readonly DynamicArray<Entity> m_crushEntities = new();
    private readonly DynamicArray<Entity> m_sectorMoveEntities = new();
    private readonly DynamicArray<SectorMoveEntityData> m_sectorMoveEntitiesData = new();
    private readonly DynamicArray<Entity> m_onEntities = new();
    private readonly Comparison<Entity> m_sectorMoveOrderComparer = new(SectorEntityMoveOrderCompare);
    private readonly DynamicArray<Entity> m_stackCrush = new();
    private readonly DynamicArray<Entity> m_clampIgnoreEntities = new();

    private MoveLinkData m_moveLinkData;
    private CanPassData m_canPassData;
    private StackEntityTraverseData m_stackData;
    private Entity m_clampIgnoreEntity;
    private readonly Func<Entity, GridIterationStatus> m_canPassTraverseFunc;
    private readonly Func<Entity, GridIterationStatus> m_sectorMoveLinkClampAction;
    private readonly Func<Entity, GridIterationStatus> m_stackEntityTraverseAction;
    private readonly Func<Entity, GridIterationStatus> m_ignoreClampEntityTraverseAction;

    public PhysicsManager(IWorld world, CompactBspTree bspTree, BlockMap blockmap, IRandom random, bool alwaysStickEntitiesToFloor)
    {
        m_world = world;
        m_bspTree = bspTree;
        m_blockmap = blockmap;
        m_blockmapGrid = blockmap.Blocks;
        m_blockmapBlocks = m_blockmapGrid.Blocks;
        m_entityManager = world.EntityManager;
        m_random = random;
        BlockmapTraverser = new BlockmapTraverser(world, m_blockmap);
        m_checkedBlockLines = new int[m_world.Lines.Count];
        m_sectorMoveLinkClampAction = new(HandleSectorMoveLinkClamp);
        m_stackEntityTraverseAction = new(HandleStackEntityTraverse);
        m_canPassTraverseFunc = new(CanPassTraverse);
        m_ignoreClampEntityTraverseAction = new(IgnoreClampEntityTraverse);
        m_alwaysStickEntitiesToFloor = alwaysStickEntitiesToFloor;
        m_clampIgnoreEntity = null!;
    }

    public void UpdateTo(IWorld world, CompactBspTree bspTree, BlockMap blockmap, IRandom random, bool alwaysStickEntitiesToFloor)
    {
        m_world = world;
        m_bspTree = bspTree;
        m_blockmap = blockmap;
        m_blockmapGrid = blockmap.Blocks;
        m_blockmapBlocks = m_blockmapGrid.Blocks;
        m_entityManager = world.EntityManager;
        m_random = random;
        m_alwaysStickEntitiesToFloor = alwaysStickEntitiesToFloor;
        BlockmapTraverser.UpdateTo(world, blockmap);
        if (world.Lines.Count > m_checkedBlockLines.Length)
            m_checkedBlockLines = new int[m_world.Lines.Count];
    }

    static int SectorEntityMoveOrderCompare(Entity? x, Entity? y)
    {
        if (x == null || y == null)
            return 1;

        int compare = x.Position.Z.CompareTo(y.Position.Z);

        if (compare == 0)
            compare = x.Id.CompareTo(y.Id);

        return compare;
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

        m_world.RenderBlockmap.RenderLink(entity);

        // Needs to be added to the sector list even with NoSector flag.
        // Doom used blockmap to manage things for sector movement.
        LinkToSectors(entity, tryMove);
        ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: true, clampToLinkedSectors, tryMove);
    }

    /// <summary>
    /// Performs all the movement logic on the entity.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    public void Move(Entity entity)
    {
        entity.BlockingEntity = null;
        entity.BlockingLine = null;
        entity.BlockingSectorPlane = null;
        MoveXY(entity);
        MoveZ(entity);
        entity.Flags.IgnoreDropOff = false;
    }

    public SectorMoveStatus MoveSectorZ(double speed, double destZ, SectorMoveSpecial moveSpecial)
    {
        Sector sector = moveSpecial.Sector;
        SectorPlane sectorPlane = moveSpecial.SectorPlane;
        SectorMoveData moveData = moveSpecial.MoveData;
        SectorPlaneFace moveType = moveSpecial.MoveData.SectorMoveType;
        double startZ = sectorPlane.Z;
        if (!m_world.Config.Compatibility.VanillaSectorPhysics && IsSectorMovementBlocked(sector, startZ, destZ, moveSpecial))
            return SectorMoveStatus.BlockedAndStop;

        // Save the Z value because we are only checking if the dest is valid
        // If the move is invalid because of a blocking entity then it will not be set to destZ
        Entity? highestBlockEntity = null;
        double? highestBlockHeight = 0.0;
        bool highestBlockEntityWasCrushing = false;
        SectorMoveStatus status = SectorMoveStatus.Success;
        sectorPlane.PrevZ = startZ;
        sectorPlane.SetZ(destZ);

        bool isCompleted = moveSpecial.IsFinalDestination(destZ);
        if (!m_world.Config.Compatibility.VanillaSectorPhysics && IsSectorMovementBlocked(sector, startZ, destZ, moveSpecial))
        {
            FixPlaneClip(sector, sectorPlane, moveType);
            status = SectorMoveStatus.BlockedAndStop;
        }

        // Move lower entities first to handle stacked entities
        // Ordering by Id is only required for EntityRenderer nudging to prevent z-fighting
        GetSectorMoveOrderedEntities(m_sectorMoveEntities, sector);
        m_sectorMoveEntitiesData.Clear();
        for (int i = 0; i < m_sectorMoveEntities.Length; i++)
        {
            Entity entity = m_sectorMoveEntities[i];
            var sectorMoveEntityData = new SectorMoveEntityData(entity, entity.Position.Z, entity.PrevPosition.Z, entity.IsCrushing());
            m_sectorMoveEntitiesData.Add(sectorMoveEntityData);

            // At slower speeds we need to set entities to the floor
            // Otherwise the player will fall and hit the floor repeatedly creating a weird bouncing effect
            if (moveType == SectorPlaneFace.Floor && startZ > destZ && (m_alwaysStickEntitiesToFloor || SpeedShouldStickToFloor(speed)) &&
                entity.OnGround && entity.HighestFloorSector == sector)
            {
                double top = destZ;
                if (entity.OnEntity.Entity != null)
                    top = entity.OnEntity.Entity.Position.Z + entity.OnEntity.Entity.Height;
                entity.Position.Z = top;
                // Setting this so SetEntityBoundsZ does not mess with forcing this entity to to the floor
                // Otherwise this is a problem with the instant lift hack
                entity.PrevPosition.Z = entity.Position.Z;
            }

            // If the move distance is higher than entity height (usually instant floors) then check entities this entity is clipped with.
            // They can't be processed for 3d checks because it will incorrectly block sector movement.
            // See InstantMoveSectorNotBlockedByClippedEntities
            if (!sectorMoveEntityData.WasCrushing && Math.Abs(startZ - destZ) >= entity.Height)
                SetClampIgnoreEntities(entity);

            ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: false, clampToLinkedSectors: SectorMoveLinkedClampCheck(entity));

            double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;
            if (thingZ + entity.GetClampHeight() > entity.LowestCeilingZ)
            {
                if (moveType == SectorPlaneFace.Ceiling)
                    PushDownBlockingEntities(entity);
                // Clipped something that wasn't directly on this entity before the move and now it will be
                // Push the entity up, and the next loop will verify it is legal
                else
                    PushUpBlockingEntity(entity);

                m_world.HandleEntityClipPlane(entity, sectorPlane);
            }
        }

        for (int i = 0; i < m_sectorMoveEntities.Length; i++)
        {
            Entity entity = m_sectorMoveEntities[i];
            if (entity.IsDisposed)
                continue;

            ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: false, clampToLinkedSectors: SectorMoveLinkedClampCheck(entity));
            var entityMoveData = m_sectorMoveEntitiesData[i];
            entity.PrevPosition.Z = entityMoveData.PrevSaveZ;
            // This allows the player to pickup items like the original
            if (entity.IsPlayer && !entity.Flags.NoClip)
                IsPositionValid(entity, entity.Position.X, entity.Position.Y, TryMoveData);

            if ((moveType == SectorPlaneFace.Ceiling && startZ < destZ) ||
                (moveType == SectorPlaneFace.Floor && startZ > destZ))
                continue;

            double thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;

            if (thingZ + entity.GetClampHeight() > entity.LowestCeilingZ)
            {
                if (entity.Flags.Dropped)
                {
                    m_entityManager.Destroy(entity);
                    continue;
                }

                // Need to gib things even when not crushing and do not count as blocking
                if (entity.Flags.Corpse && !entity.Flags.DontGib && entity.Health <= 0)
                {
                    SetToGiblets(entity);
                    continue;
                }

                // Doom checked against shootable instead of solid...
                if (!entity.Flags.Shootable)
                    continue;

                if (moveData.Crush != null)
                {
                    if (moveData.Crush.Value.CrushMode == ZDoomCrushMode.Hexen || moveData.Crush.Value.Damage == 0)
                    {
                        highestBlockEntity = entity;
                        highestBlockHeight = entity.Height;
                        highestBlockEntityWasCrushing = entityMoveData.WasCrushing;
                    }

                    status = SectorMoveStatus.Crush;
                    m_crushEntities.Add(entity);
                }
                else if (CheckSectorMoveBlock(entity, moveType, entityMoveData.SaveZ))
                {
                    highestBlockEntity = entity;
                    highestBlockHeight = entity.Height;
                    highestBlockEntityWasCrushing = entityMoveData.WasCrushing;
                    status = SectorMoveStatus.Blocked;
                }
            }
        }

        if (highestBlockEntity != null && highestBlockHeight.HasValue && !highestBlockEntity.IsDead)
        {
            double diff = 0;
            // Set the sector Z to the difference of the blocked height (only works if not being crushed)
            // Could probably do something fancy to figure this out if the entity is being crushed, but this is quite rare
            if ((moveData.Flags & SectorMoveFlags.EntityBlockMovement) != 0 || highestBlockEntityWasCrushing || isCompleted)
            {
                sectorPlane.SetZ(startZ);
            }
            else
            {
                double thingZ = highestBlockEntity.OnGround ? highestBlockEntity.HighestFloorZ : highestBlockEntity.Position.Z;
                // Floor cannot be higher than ceiling for this reset
                if (moveType == SectorPlaneFace.Floor)
                    destZ = Math.Clamp(destZ, double.MinValue, sector.Ceiling.Z);
                else
                    destZ = Math.Clamp(destZ, sector.Floor.Z, double.MaxValue);

                diff = Math.Abs(startZ - destZ) - (thingZ + highestBlockHeight.Value - highestBlockEntity.LowestCeilingZ);
                if (destZ < startZ)
                    diff = -diff;
                sectorPlane.SetZ(startZ + diff);
            }

            // Entity blocked movement, reset all entities in moving sector after resetting sector Z
            for (int i = 0; i < m_sectorMoveEntities.Length; i++)
            {
                Entity relinkEntity = m_sectorMoveEntities[i];
                // Check for entities that may be dead from being crushed
                if (relinkEntity.IsDisposed)
                    continue;
                relinkEntity.UnlinkFromWorld();
                relinkEntity.Position.Z = m_sectorMoveEntitiesData[i].SaveZ + diff;
                LinkToWorld(relinkEntity);
            }
        }

        if (moveData.Crush != null && m_crushEntities.Length > 0)
            CrushEntities(m_crushEntities, sector, moveData.Crush.Value);

        m_clampIgnoreEntities.Clear();
        m_crushEntities.Clear();
        m_sectorMoveEntities.Clear();

        // If an entity is blocking this and the destination is blocked then we need to stop to match vanilla behavior.
        if (isCompleted && status == SectorMoveStatus.Blocked)
            return SectorMoveStatus.BlockedAndStop;

        return status;
    }

    private void SetClampIgnoreEntities(Entity entity)
    {
        m_clampIgnoreEntity = entity;
        m_clampIgnoreEntities.Clear();
        BlockmapTraverser.EntityTraverse(entity.GetBox2D(), m_ignoreClampEntityTraverseAction);
    }

    private GridIterationStatus IgnoreClampEntityTraverse(Entity checkEntity)
    {
        if (!checkEntity.Flags.Solid)
            return GridIterationStatus.Continue;

        double currentZ = checkEntity.Position.Z;
        // Find the original Z value if this entity is currently being moved by a sector.
        for (int i = 0; i < m_sectorMoveEntitiesData.Length; i++)
        {
            if (m_sectorMoveEntitiesData[i].Entity != checkEntity)
                continue;
            currentZ = m_sectorMoveEntitiesData[i].SaveZ;
            break;
        }

        double saveZ = checkEntity.Position.Z;
        checkEntity.Position.Z = currentZ;
        if (m_clampIgnoreEntity.OverlapsZ(checkEntity))
            m_clampIgnoreEntities.Add(checkEntity);
        checkEntity.Position.Z = saveZ;
        return GridIterationStatus.Continue;
    }

    private bool SectorMoveLinkedClampCheck(Entity entity)
    {
        // If not move linked check if this thing would pop up and would clip into another entity.
        // Otherwise allow it to pop up and match vanilla doom behavior.
        if (entity.MoveLinked || entity.Flags.NoClip)
            return true;

        GetEntityClampValues(entity, entity.IntersectSectors, true, null, out Sector highestFloor, out _, out _, out _);

        if (highestFloor == entity.HighestFloorSector)
            return true;

        m_moveLinkData.Entity = entity;
        m_moveLinkData.Success = true;
        m_moveLinkData.Height = highestFloor.ToFloorZ(entity.Position) + entity.Height;

        m_world.BlockmapTraverser.EntityTraverse(entity.GetBox2D(), m_sectorMoveLinkClampAction);
        return m_moveLinkData.Success;
    }

    private GridIterationStatus HandleSectorMoveLinkClamp(Entity checkEntity)
    {
        if (!checkEntity.Flags.Solid || checkEntity.Flags.Corpse || checkEntity.Flags.NoClip || m_moveLinkData.Entity.Id == checkEntity.Id)
            return GridIterationStatus.Continue;

        if (m_moveLinkData.Height > checkEntity.Position.Z)
        {
            m_moveLinkData.Success = false;
            return GridIterationStatus.Stop;
        }
        return GridIterationStatus.Continue;
    }

    private void GetSectorMoveOrderedEntities(DynamicArray<Entity> entities, Sector sector)
    {
        LinkableNode<Entity>? node = sector.Entities.Head;
        while (node != null)
        {
            var entity = node.Value;
            // Doom did this by blockmap so do not add things with NoBlockmap
            if (!entity.Flags.NoBlockmap && EntityHasMovementSector(entity, sector))
                m_sectorMoveEntities.Add(entity);
            node = node.Next;
        }
        entities.Sort(m_sectorMoveOrderComparer);
    }

    private static bool EntityHasMovementSector(Entity entity, Sector sector)
    {
        for (int i = 0; i < entity.IntersectSectors.Length; i++)
            if (entity.IntersectSectors[i] == sector)
                return true;

        return false;
    }

    // Constants and logic from WinMBF.
    // Credit to Lee Killough et al.
    public static double GetMoveFactor(Entity entity)
    {
        double sectorFriction = GetFrictionFromSectors(entity);
        double moveFactor = DefaultMoveFactor;

        if (sectorFriction != Constants.DefaultFriction)
        {
            if (sectorFriction >= Constants.DefaultFriction)
                moveFactor = (0x10092 - sectorFriction * 65536.0) * 0x70 / 0x158 / 65536.0;
            else
                moveFactor = (sectorFriction * 65536.0 - 0xDB34) * 0xA / 0x80 / 65536.0;

            moveFactor = Math.Clamp(moveFactor, MinMoveFactor, double.MaxValue);
            // The move factor was based on 2048 being default in Boom.
            moveFactor /= 2048.0 / 65536.0;
        }

        if (sectorFriction < Constants.DefaultFriction)
        {
            double momentum = entity.Velocity.XY.Length();
            if (momentum > MudMoveFactorHigh)
                moveFactor *= 8;
            else if (momentum > MudMoveFactorMed)
                moveFactor *= 4;
            else if (momentum > MudMoveFactorLow)
                moveFactor *= 2;
        }

        return moveFactor;
    }

    private static bool IsSectorMovementBlocked(Sector sector, double startZ, double destZ, SectorMoveSpecial moveSpecial)
    {
        if (moveSpecial.MoveData.SectorMoveType == SectorPlaneFace.Floor && destZ < startZ)
            return false;

        if (moveSpecial.MoveData.SectorMoveType == SectorPlaneFace.Ceiling && destZ > startZ)
            return false;

        if (moveSpecial.StartClipped)
            return false;

        return sector.Ceiling.Z < sector.Floor.Z;
    }

    private static void FixPlaneClip(Sector sector, SectorPlane sectorPlane, SectorPlaneFace moveType)
    {
        if (moveType == SectorPlaneFace.Floor)
        {
            sectorPlane.SetZ(sector.Ceiling.Z);
            return;
        }

        sectorPlane.SetZ(sector.Floor.Z);
    }

    private static bool SpeedShouldStickToFloor(double speed) =>
        -speed <= SetEntityToFloorSpeedMax || -speed == SectorMoveData.InstantToggleSpeed;

    private static bool CheckSectorMoveBlock(Entity entity, SectorPlaneFace moveType, double saveZ)
    {
        // If the entity was pushed up by a floor and changed it's z pos then this floor is blocked
        if (moveType == SectorPlaneFace.Ceiling || saveZ != entity.Position.Z)
            return true;

        return false;
    }

    private void CrushEntities(DynamicArray<Entity> crushEntities, Sector sector, in CrushData crush)
    {
        if (crush.Damage == 0 || (m_world.Gametick & 3) != 0)
            return;

        // Check for stacked entities, so we can crush the stack
        LinkableNode<Entity>? node = sector.Entities.Head;
        while (node != null)
        {
            Entity checkEntity = node.Value;
            if (checkEntity.OverEntity.Entity != null && ContainsEntity(crushEntities, checkEntity.OverEntity.Entity))
                m_stackCrush.Add(checkEntity);
            node = node.Next;
        }

        for (int i = 0; i < crushEntities.Length; i++)
        {
            if (ContainsEntity(m_stackCrush, crushEntities[i]))
                continue;
            m_stackCrush.Add(crushEntities[i]);
        }

        for (int i = 0; i < m_stackCrush.Length; i++)
        {
            Entity crushEntity = m_stackCrush[i];
            m_world.HandleEntityHit(crushEntity, crushEntity.Velocity, null);

            if (!crushEntity.IsDead && m_world.DamageEntity(crushEntity, null, crush.Damage, DamageType.Normal) &&
                !crushEntity.Flags.NoBlood && !crushEntity.IsDisposed)
            {
                Vec3D pos = crushEntity.Position;
                pos.Z += crushEntity.Height / 2;
                Entity? blood = m_entityManager.Create(crushEntity.GetBloodDefinition(), pos, 0, 0, 0);
                if (blood != null)
                {
                    blood.Velocity.X += m_random.NextDiff() / 16.0;
                    blood.Velocity.Y += m_random.NextDiff() / 16.0;
                }
            }
        }

        m_stackCrush.Clear();
    }

    private static bool ContainsEntity(DynamicArray<Entity> entities, Entity entity)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i] == entity)
                return true;
        }

        return false;
    }

    private void SetToGiblets(Entity entity)
    {
        if (entity.SetCrushState())
        {
            entity.Flags.CrushGiblets = true;
            return;
        }

        m_entityManager.Destroy(entity);
        m_entityManager.Create("REALGIBS", entity.Position);
    }

    private static void PushUpBlockingEntity(Entity pusher)
    {
        if (pusher.LowestCeilingObject is not Entity)
            return;

        Entity entity = (Entity)pusher.LowestCeilingObject;
        entity.Position.Z = pusher.Position.Z + pusher.Height;
    }

    private static void PushDownBlockingEntities(Entity pusher)
    {
        // Because of how ClampBetweenFloorAndCeiling works, try to push down the entire stack and stop when something clips a floor
        if (pusher.HighestFloorObject is Sector && pusher.HighestFloorZ > pusher.LowestCeilingZ - pusher.Height)
            return;

        pusher.Position.Z = pusher.LowestCeilingZ - pusher.Height;

        if (pusher.OnEntity.Entity != null)
        {
            Entity? current = pusher.OnEntity.Entity;
            while (current != null)
            {
                if (current.HighestFloorObject is Sector && current.HighestFloorZ > pusher.Position.Z - current.Height)
                    return;

                current.Position.Z = pusher.Position.Z - current.Height;
                pusher = current;
                current = pusher.OnEntity.Entity;
            }
        }
    }

    public void HandleEntityDeath(Entity deathEntity)
    {
        if (deathEntity.OnEntity.Entity != null || deathEntity.OverEntity.Entity != null)
            StackedEntityMoveZ(deathEntity);
    }

    private static void ApplyFriction(Entity entity)
    {
        double sectorFriction = GetFrictionFromSectors(entity);
        entity.Velocity.X *= sectorFriction;
        entity.Velocity.Y *= sectorFriction;
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
        BlockContinue,
    }

    private unsafe LineBlock LineBlocksEntity(Entity entity, double x, double y, BlockLine* line, TryMoveData? tryMove)
    {
        if (Line.BlocksEntity(entity, line->OneSided, line->Flags, WorldStatic.Mbf21))
            return LineBlock.BlockStopChecking;

        LineOpening opening;
        if (tryMove != null)
        {
            opening = GetLineOpeningWithDropoff(x, y, line);
            tryMove.SetIntersectionData(opening);
        }
        else
        {
            opening = GetLineOpening(line->Line);
        }

        if (opening.CanPassOrStepThrough(entity))
            return LineBlock.NoBlock;

        return LineBlock.BlockContinue;
    }

    public LineOpening GetLineOpening(Line line)
    {
        m_lineOpening.Set(line);
        return m_lineOpening;
    }


    public unsafe LineOpening GetLineOpeningWithDropoff(double x, double y, BlockLine* line)
    {
        Sector front = line->FrontSector;
        Sector back = line->BackSector!;
        if (front.Ceiling.Z < back.Ceiling.Z)
        {
            m_lineOpening.CeilingZ = front.Ceiling.Z;
            m_lineOpening.CeilingSector = front;
        }
        else
        {
            m_lineOpening.CeilingZ = back.Ceiling.Z;
            m_lineOpening.CeilingSector = back;
        }

        if (front.Floor.Z > back.Floor.Z)
        {
            m_lineOpening.FloorZ = front.Floor.Z;
            m_lineOpening.FloorSector = front;
        }
        else
        {
            m_lineOpening.FloorZ = back.Floor.Z;
            m_lineOpening.FloorSector = back;
        }

        m_lineOpening.OpeningHeight = m_lineOpening.CeilingZ - m_lineOpening.FloorZ;

        double dot = (line->Segment.Delta.X * (y - line->Segment.Start.Y)) - (line->Segment.Delta.Y * (x - line->Segment.Start.X));
        if (dot <= 0)
            m_lineOpening.DropOffZ = back.Floor.Z;
        else
            m_lineOpening.DropOffZ = front.Floor.Z;

        return m_lineOpening;
    }

    private static void SetEntityOnFloorOrEntity(Entity entity, double floorZ, bool smoothZ)
    {
        // Additionally check to smooth camera when stepping up to an entity
        if (entity.PlayerObj != null && smoothZ)
            entity.PlayerObj.SetAndSmoothZ(floorZ);
        else
            entity.Position.Z = floorZ;

        // For now we remove any negative velocity. If upward velocity is
        // reset to zero then the jump we apply to players is lost and they
        // can never jump. Maybe we want to fix this in the future by doing
        // application of jumping after the XY movement instead of before?
        entity.Velocity.Z = Math.Max(0, entity.Velocity.Z);
    }

    private void ClampBetweenFloorAndCeiling(Entity entity, DynamicArray<Sector>? intersectSectors, bool smoothZ, bool clampToLinkedSectors = true,
        TryMoveData? tryMove = null)
    {
        Invariant(intersectSectors == null || ReferenceEquals(entity.IntersectSectors, intersectSectors), $"Intersect sectors not owned by entity.");

        if (entity.IsDisposed || entity.Definition.IsBulletPuff)
            return;
        if (entity.Flags.NoClip && entity.Flags.NoGravity)
            return;

        double prevHighestFloorZ = entity.HighestFloorZ;
        Entity? prevOnEntity = entity.OnEntity.Entity;
        SetEntityBoundsZ(entity, intersectSectors, clampToLinkedSectors, tryMove);
        entity.SetOnEntity(null);

        double lowestCeil = entity.LowestCeilingZ;
        double highestFloor = entity.HighestFloorZ;

        // short.MinValue checks are to emulate the fixed point overflow required for mikoportals.
        if (entity.Position.Z + entity.Height > lowestCeil || highestFloor <= short.MinValue)
        {
            entity.Velocity.Z = 0;
            entity.Position.Z = lowestCeil - entity.GetClampHeight();

            if (highestFloor > short.MinValue)
            {
                if (entity.LowestCeilingObject is Entity blockEntity)
                    entity.BlockingEntity = blockEntity;
                else
                    entity.BlockingSectorPlane = entity.LowestCeilingSector.Ceiling;
            }
        }

        bool clippedFloor = entity.Position.Z <= highestFloor;
        if (entity.Position.Z <= highestFloor && highestFloor < short.MaxValue)
        {
            if (entity.HighestFloorObject is Entity highestEntity &&
                highestEntity.Position.Z + highestEntity.Height <= entity.Position.Z + entity.GetMaxStepHeight())
            {
                entity.SetOnEntity(highestEntity);
            }

            for (int i = 0; i < m_onEntities.Length; i++)
                m_onEntities[i].SetOverEntity(entity);

            if (clippedFloor)
            {
                if (entity.HighestFloorObject is Entity blockEntity)
                    entity.BlockingEntity = blockEntity;
                else if (entity.BlockingSectorPlane == null && entity.Velocity.Z < 0)
                    entity.BlockingSectorPlane = entity.HighestFloorSector.Floor;
            }

            SetEntityOnFloorOrEntity(entity, highestFloor, smoothZ && prevHighestFloorZ != entity.HighestFloorZ);
        }

        if (prevOnEntity != null && prevOnEntity != entity.OnEntity.Entity)
            prevOnEntity.SetOverEntity(null);

        entity.CheckOnGround();
        m_onEntities.Clear();
    }

    private void SetEntityBoundsZ(Entity entity, DynamicArray<Sector>? intersectSectors, bool clampToLinkedSectors, TryMoveData? tryMove)
    {
        Entity? highestFloorEntity = null;
        Entity? lowestCeilingEntity = null;

        entity.SetOnEntity(null);

        GetEntityClampValues(entity, intersectSectors, clampToLinkedSectors, tryMove, out Sector highestFloor, out Sector lowestCeiling, 
            out double highestFloorZ, out double lowestCeilZ);

        if (WorldStatic.InfinitelyTallThings)
        {
            entity.HighestFloorZ = highestFloorZ;
            entity.LowestCeilingZ = lowestCeilZ;
            entity.HighestFloorSector = highestFloor;
            entity.LowestCeilingSector = lowestCeiling;
            entity.HighestFloorObject = highestFloor;
            entity.LowestCeilingObject = lowestCeiling;
            return;
        }

        // Only check against other entities if CanPass is set (height sensitive clip detection)
        if (entity.Flags.CanPass && !entity.Flags.NoClip)
        {
            m_canPassData.Entity = entity;
            m_canPassData.HighestFloorEntity = highestFloorEntity;
            m_canPassData.LowestCeilingEntity = lowestCeilingEntity;
            m_canPassData.EntityTopZ = entity.Position.Z + entity.Height;
            m_canPassData.HighestFloorZ = highestFloorZ;
            m_canPassData.LowestCeilZ = lowestCeilZ;
            m_canPassData.ClampToLinkedSectors = clampToLinkedSectors;

            if (tryMove == null)
            {
                // Get intersecting entities here - They are not stored in the entity because other entities can move around after this entity has linked
                m_world.BlockmapTraverser.EntityTraverse(entity.GetBox2D(), m_canPassTraverseFunc);
            }
            else
            {
                for (int i = 0; i < tryMove.IntersectEntities2D.Length; i++)
                    CanPassTraverse(tryMove.IntersectEntities2D[i]);
            }

            highestFloorEntity = m_canPassData.HighestFloorEntity;
            lowestCeilingEntity = m_canPassData.LowestCeilingEntity;
            highestFloorZ = m_canPassData.HighestFloorZ;
            lowestCeilZ = m_canPassData.LowestCeilZ;
        }

        entity.HighestFloorZ = highestFloorZ;
        entity.LowestCeilingZ = lowestCeilZ;
        entity.HighestFloorSector = highestFloor;
        entity.LowestCeilingSector = lowestCeiling;

        if (highestFloorEntity != null && highestFloorEntity.Position.Z + highestFloorEntity.Height > highestFloor.ToFloorZ(entity.Position))
            entity.HighestFloorObject = highestFloorEntity;
        else
            entity.HighestFloorObject = highestFloor;

        if (lowestCeilingEntity != null && lowestCeilingEntity.Position.Z + lowestCeilingEntity.Height < lowestCeiling.ToCeilingZ(entity.Position))
            entity.LowestCeilingObject = lowestCeilingEntity;
        else
            entity.LowestCeilingObject = lowestCeiling;
    }

    private GridIterationStatus CanPassTraverse(Entity intersectEntity)
    {
        var entity = m_canPassData.Entity;
        if (!intersectEntity.Flags.Solid || intersectEntity.Flags.Corpse || intersectEntity.Flags.NoClip || entity.Id == intersectEntity.Id)
            return GridIterationStatus.Continue;

        for (int i = 0; i < m_clampIgnoreEntities.Length; i++)
        {
            if (m_clampIgnoreEntities[i] == intersectEntity)
                return GridIterationStatus.Continue;
        }

        double intersectTopZ = intersectEntity.Position.Z + intersectEntity.Height;
        if (entity.Flags.Missile && WorldStatic.MissileClip)
            intersectTopZ = intersectEntity.GetMissileClipHeight(true);
        bool above = entity.PrevPosition.Z >= intersectTopZ;
        bool below = entity.PrevPosition.Z + entity.Height <= intersectEntity.Position.Z;
        bool clipped = false;
        bool addedOnEntity = false;
        if (above && entity.Position.Z < intersectTopZ)
            clipped = true;
        else if (below && m_canPassData.EntityTopZ > intersectEntity.Position.Z)
            clipped = true;

        if (!above && !below && !m_canPassData.ClampToLinkedSectors && !intersectEntity.Flags.ActLikeBridge)
            return GridIterationStatus.Continue;

        if (above)
        {
            // Need to check clipping coming from above, if we're above
            // or clipped through then this is our floor.
            if ((clipped || entity.Position.Z >= intersectTopZ) && intersectTopZ >= m_canPassData.HighestFloorZ)
            {
                addedOnEntity = true;
                if (m_canPassData.HighestFloorEntity != null && m_canPassData.HighestFloorEntity.Position.Z + m_canPassData.HighestFloorEntity.Height < m_canPassData.HighestFloorZ)
                    m_onEntities.Clear();

                m_canPassData.HighestFloorEntity = intersectEntity;
                m_canPassData.HighestFloorZ = intersectTopZ;
                m_onEntities.Add(m_canPassData.HighestFloorEntity);
            }
        }
        else if (below)
        {
            // Same check as above but checking clipping the ceiling.
            if ((clipped || m_canPassData.EntityTopZ <= intersectEntity.Position.Z) && intersectEntity.Position.Z < m_canPassData.LowestCeilZ)
            {
                m_canPassData.LowestCeilingEntity = intersectEntity;
                m_canPassData.LowestCeilZ = intersectEntity.Position.Z;
            }
        }

        // Need to check if we can step up to this floor.
        if (entity.Position.Z + entity.GetMaxStepHeight() >= intersectTopZ && intersectTopZ >= m_canPassData.HighestFloorZ && !addedOnEntity)
        {
            if (m_canPassData.HighestFloorEntity != null && m_canPassData.HighestFloorEntity.Position.Z + m_canPassData.HighestFloorEntity.Height < m_canPassData.HighestFloorZ)
                m_onEntities.Clear();

            m_canPassData.HighestFloorEntity = intersectEntity;
            m_canPassData.HighestFloorZ = intersectTopZ;
            m_onEntities.Add(m_canPassData.HighestFloorEntity);
        }

        return GridIterationStatus.Continue;
    }

    private static void GetEntityClampValues(Entity entity, DynamicArray<Sector>? intersectSectors,
        bool clampToLinkedSectors, TryMoveData? tryMove, out Sector highestFloor, out Sector lowestCeiling, out double highestFloorZ, out double lowestCeilZ)
    {
        if (!clampToLinkedSectors)
        {
            highestFloor = entity.Sector;
            lowestCeiling = entity.Sector;
            highestFloorZ = highestFloor.Floor.Z;
            lowestCeilZ = lowestCeiling.Ceiling.Z;
            return;
        }

        if (tryMove != null && tryMove.LowestCeiling != null && tryMove.HighestFloor != null)
        {
            highestFloor = tryMove.HighestFloor;
            lowestCeiling = tryMove.LowestCeiling;
            highestFloorZ = tryMove.HighestFloorZ;
            lowestCeilZ = tryMove.LowestCeilingZ;
            return;
        }

        if (intersectSectors == null)
        {
            highestFloor = entity.HighestFloorSector;
            lowestCeiling = entity.LowestCeilingSector;
            highestFloorZ = entity.HighestFloorZ;
            lowestCeilZ = entity.LowestCeilingZ;
            return;
        }

        highestFloor = entity.Sector;
        lowestCeiling = entity.Sector;
        highestFloorZ = highestFloor.Floor.Z;
        lowestCeilZ = lowestCeiling.Ceiling.Z;
        for (int i = 0; i < intersectSectors.Length; i++)
        {
            Sector sector = intersectSectors[i];
            double floorZ = sector.Floor.Z;

            if (floorZ < short.MinValue)
            {
                highestFloor = sector;
                highestFloorZ = floorZ;
            }

            if (floorZ > highestFloorZ && highestFloorZ > short.MinValue)
            {
                highestFloor = sector;
                highestFloorZ = floorZ;
            }

            double ceilZ = sector.Ceiling.Z;
            if (ceilZ < lowestCeilZ)
            {
                lowestCeiling = sector;
                lowestCeilZ = ceilZ;
            }
        }
    }

    private unsafe void LinkToSectors(Entity entity, TryMoveData? tryMove)
    {
        Precondition(entity.SectorNodes.Empty(), "Forgot to unlink entity from blockmap");

        int checkCounter = ++WorldStatic.CheckCounter;
        Subsector centerSubsector;
        if (tryMove != null && tryMove.Subsector != null && tryMove.Success)
            centerSubsector = tryMove.Subsector;
        else
            centerSubsector = m_bspTree.Subsectors[m_bspTree.ToSubsectorIndex(entity.Position.X, entity.Position.Y)];

        Sector centerSector = centerSubsector.Sector;
        centerSector.CheckCount = checkCounter;
        if (tryMove != null)
        {
            int intersectSectorLength = 0;
            entity.IntersectSectors.EnsureCapacity(tryMove.IntersectSectors.Length);
            entity.SectorNodes.EnsureCapacity(tryMove.IntersectSectors.Length);
            for (int i = 0; i < tryMove.IntersectSectors.Length; i++)
            {
                var sector = tryMove.IntersectSectors[i];
                if (sector.CheckCount == checkCounter)
                    continue;
                sector.CheckCount = checkCounter;
                entity.IntersectSectors.Data[intersectSectorLength] = sector;
                entity.SectorNodes.Data[intersectSectorLength++] = sector.Link(entity);
            }

            entity.IntersectSectors.Length = intersectSectorLength;
            entity.SectorNodes.Length = intersectSectorLength;
        }
        else
        {
            Box2D box = entity.GetBox2D();
            var it = m_blockmapGrid.CreateBoxIteration(box);
            for (int by = it.BlockStart.Y; by <= it.BlockEnd.Y; by++)
            {
                for (int bx = it.BlockStart.X; bx <= it.BlockEnd.X; bx++)
                {
                    Block block = m_blockmapBlocks[by * it.Width + bx];
                    for (int i = 0; i < block.BlockLineCount; i++)
                    {
                        fixed (BlockLine* line = &block.BlockLines[i])
                        {
                            if (m_checkedBlockLines[line->LineId] == checkCounter)
                                continue;

                            m_checkedBlockLines[line->LineId] = checkCounter;

                            if (line->Segment.Intersects(box))
                            {
                                // Doomism: Ignore for moving sectors if blocked by flags only.
                                if (Line.BlocksEntity(entity, line->OneSided, line->Flags, WorldStatic.Mbf21))
                                    goto doneLinkToSectors;

                                if (line->FrontSector.CheckCount != checkCounter)
                                {
                                    Sector sector = line->FrontSector;
                                    sector.CheckCount = checkCounter;
                                    entity.IntersectSectors.Add(sector);
                                    entity.SectorNodes.Add(sector.Link(entity));
                                }

                                if (line->BackSector != null && line->BackSector!.CheckCount != checkCounter)
                                {
                                    Sector sector = line->BackSector!;
                                    sector.CheckCount = checkCounter;
                                    entity.IntersectSectors.Add(sector);
                                    entity.SectorNodes.Add(sector.Link(entity));
                                }
                            }
                        }
                    }
                }
            }
        }
doneLinkToSectors:
        entity.Subsector = centerSubsector;
        entity.Sector = centerSector;
        entity.IntersectSectors.Add(centerSector);
        entity.SectorNodes.Add(centerSector.Link(entity));
    }

    private static void ClearVelocityXY(Entity entity)
    {
        entity.Velocity.X = 0;
        entity.Velocity.Y = 0;
    }

    public TryMoveData TryMoveXY(Entity entity, double x, double y)
    {
        TryMoveData.SetPosition(x, y);
        if (entity.Flags.NoClip)
        {
            entity.UnlinkFromWorld();
            entity.Position.X = x;
            entity.Position.Y = y;
            LinkToWorld(entity);
            TryMoveData.Success = true;
            return TryMoveData;
        }

        Vec2D stepDelta = new(x - entity.Position.X, y - entity.Position.Y);
        if (stepDelta.X == 0 && stepDelta.Y == 0)
        {
            TryMoveData.Success = true;
            return TryMoveData;
        }

        if (entity.IsCrushing())
        {
            TryMoveData.Success = false;
            return TryMoveData;
        }

        // We advance in small steps that are smaller than the radius of
        // the actor so we don't skip over any lines or things due to fast
        // entity speed.
        int slidesLeft = MaxSlides;
        int numMoves = 1;
        if (entity.Radius > 0.5)
        {
            double moveDistance = entity.Radius - 0.5;
            double biggerAxis = Math.Max(Math.Abs(stepDelta.X), Math.Abs(stepDelta.Y));
            numMoves = (int)(biggerAxis / moveDistance) + 1;

            if (numMoves > 1)
            {
                stepDelta.X = stepDelta.X / numMoves;
                stepDelta.Y = stepDelta.Y / numMoves;
            }
        }

        bool success = true;
        Vec3D saveVelocity = entity.Velocity;
        bool stacked = !WorldStatic.InfinitelyTallThings && (entity.OnEntity.Entity != null || entity.OverEntity.Entity != null);
        Line? slideBlockLine = null;
        Entity? slideBlockEntity = null;

        for (int movesLeft = numMoves; movesLeft > 0; movesLeft--)
        {
            if ((stepDelta.X == 0 && stepDelta.Y == 0) || m_world.WorldState == WorldState.Exit)
                break;

            double nextX = entity.Position.X + stepDelta.X;
            double nextY = entity.Position.Y + stepDelta.Y;
            if (IsPositionValid(entity, nextX, nextY, TryMoveData))
            {
                entity.MoveLinked = true;
                MoveTo(entity, nextX, nextY, TryMoveData);
                if (entity.Flags.Teleported)
                    return TryMoveData;

                m_world.HandleEntityIntersections(entity, saveVelocity, TryMoveData);
                continue;
            }

            if (entity.Flags.SlidesOnWalls && slidesLeft > 0)
            {
                // BlockingLine and BlockingEntity will get cleared on HandleSlide(IsPositionValid) calls.
                // Carry them over so other functions after TryMoveXY can use them for verification.
                var blockingLine = entity.BlockingLine;
                var blockingEntity = entity.BlockingEntity;
                HandleSlide(entity, ref stepDelta, ref movesLeft, TryMoveData);
                entity.BlockingLine = blockingLine;
                entity.BlockingEntity = blockingEntity;
                if (slideBlockLine == null && blockingLine != null)
                    slideBlockLine = blockingLine;
                if (slideBlockEntity == null && blockingEntity != null)
                    slideBlockEntity = blockingEntity;
                slidesLeft--;
                success = false;
                continue;
            }

            success = false;
            if (ShouldClearSlide(TryMoveData))
                ClearVelocityXY(entity);
            break;
        }

        if (stacked && entity.Flags.CanPass && !entity.Flags.NoClip)
        {
            Box2D previousBox = new(entity.PrevPosition.X, entity.PrevPosition.Y, entity.Properties.Radius);
            m_stackData.Entity = entity;
            m_stackData.EntityBottomZ = entity.Position.Z;
            m_stackData.EntityTopZ = entity.Position.Z + entity.Height;
            m_world.BlockmapTraverser.EntityTraverse(previousBox, m_stackEntityTraverseAction);
        }

        if (!success)
        {
            if (slideBlockEntity != null && entity.BlockingEntity == null)
                entity.BlockingEntity = slideBlockEntity;
            if (slideBlockLine != null && entity.BlockingLine == null)
                entity.BlockingLine = slideBlockLine;
            m_world.HandleEntityHit(entity, saveVelocity, TryMoveData);
        }

        TryMoveData.Success = success;
        return TryMoveData;
    }

    private GridIterationStatus HandleStackEntityTraverse(Entity entity)
    {
        if (entity.OnEntity.Entity == m_stackData.Entity || entity.OverEntity.Entity == entity ||
            entity.Position.Z == m_stackData.EntityTopZ || entity.Position.Z + entity.Height == m_stackData.EntityBottomZ)
        {
            ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors,
                smoothZ: false, clampToLinkedSectors: entity.MoveLinked);
        }

        return GridIterationStatus.Continue;
    }

    private void StackedEntityMoveZ(Entity entity)
    {
        Entity? currentOverEntity = entity.OverEntity.Entity;

        if (entity.OverEntity.Entity != null && entity.OverEntity.Entity.Position.Z > entity.Position.Z + entity.Height)
            entity.SetOverEntity(null);

        if (entity.OnEntity.Entity != null)
        {
            Entity onEntity = entity.OnEntity.Entity;
            ClampBetweenFloorAndCeiling(onEntity, onEntity.IntersectSectors,
                smoothZ: false, clampToLinkedSectors: onEntity.MoveLinked);
        }

        while (currentOverEntity != null)
        {
            LinkableNode<Entity>? node = entity.Sector.Entities.Head;
            while (node != null)
            {
                Entity relinkEntity = node.Value;
                if (relinkEntity.OnEntity.Entity == entity)
                    ClampBetweenFloorAndCeiling(relinkEntity, relinkEntity.IntersectSectors, false);
                node = node.Next;
            }

            entity = currentOverEntity;
            Entity? next = currentOverEntity.OverEntity.Entity;
            if (currentOverEntity.OverEntity.Entity != null && currentOverEntity.OverEntity.Entity.OnEntity.Entity != entity)
                currentOverEntity.SetOverEntity(null);
            currentOverEntity = next;
        }
    }

    private const int PositionValidFlags1 = EntityFlags.SpecialFlag | EntityFlags.SolidFlag | EntityFlags.ShootableFlag;
    private const int PositionValidFlags2 = EntityFlags.TouchyFlag;

    public unsafe bool IsPositionValid(Entity entity, double x, double y, TryMoveData tryMove)
    {
        if (!entity.Flags.Float && !entity.IsPlayer && entity.OnEntity.Entity != null && !entity.OnEntity.Entity.Flags.ActLikeBridge)
            return false;

        tryMove.Success = true;
        tryMove.LowestCeiling = entity.Sector;
        tryMove.HighestFloor = entity.Sector;
        tryMove.LowestCeilingZ = entity.LowestCeilingZ;
        tryMove.Subsector = null;
        tryMove.IntersectSectors.Length = 0;
        tryMove.IntersectEntities2D.Length = 0;

        if (entity.HighestFloorObject is Entity highFloorEntity)
        {
            tryMove.HighestFloorZ = highFloorEntity.Position.Z + highFloorEntity.Height;
            tryMove.DropOffZ = entity.Sector.Floor.Z;
        }
        else
        {
            tryMove.Subsector = m_bspTree.ToSubsector(x, y);
            tryMove.HighestFloorZ = tryMove.DropOffZ = tryMove.Subsector.Sector.Floor.Z;
            tryMove.LowestCeilingZ = tryMove.Subsector.Sector.Ceiling.Z;
        }

        entity.BlockingLine = null;
        entity.BlockingEntity = null;
        
        int checkCounter = ++WorldStatic.CheckCounter;
        bool isMissile = entity.Flags.Missile;
        bool checkEntities = entity.Flags.Solid || entity.Flags.Missile;
        bool canPickup = entity.Flags.Pickup;
        Entity? nextEntity;
                
        var boxMinX = x - entity.Radius;
        var boxMaxX = x + entity.Radius;
        var boxMinY = y - entity.Radius;
        var boxMaxY = y + entity.Radius;
        int blockStartX = Math.Max(0, (int)((boxMinX - m_blockmapGrid.Bounds.Min.X) / m_blockmapGrid.Dimension));
        int blockStartY = Math.Max(0, (int)((boxMinY - m_blockmapGrid.Bounds.Min.Y) / m_blockmapGrid.Dimension));
        int blockEndX = Math.Min((int)((boxMaxX - m_blockmapGrid.Bounds.Min.X) / m_blockmapGrid.Dimension), m_blockmapGrid.Width - 1);
        int blockEndY = Math.Min((int)((boxMaxY - m_blockmapGrid.Bounds.Min.Y) / m_blockmapGrid.Dimension), m_blockmapGrid.Height - 1);
        int intersectSectorLength = 0;

        for (int by = blockStartY; by <= blockEndY; by++)
        {
            for (int bx = blockStartX; bx <= blockEndX; bx++)
            {
                Block block = m_blockmapBlocks[by * m_blockmapGrid.Width + bx];
                if (checkEntities)
                {               
                    for (var entityNode = block.Entities.Head; entityNode != null; entityNode = entityNode.Next)
                    {
                        nextEntity = entityNode.Value;
                        if (nextEntity.BlockmapCount == checkCounter)
                            continue;

                        nextEntity.BlockmapCount = checkCounter;

                        if ((nextEntity.Flags.Flags1 & PositionValidFlags1) == 0 && (nextEntity.Flags.Flags2 & PositionValidFlags2) == 0)
                            continue;

                        var blockDist = nextEntity.Radius + entity.Radius;
                        if (Math.Abs(nextEntity.Position.X - x) >= blockDist || Math.Abs(nextEntity.Position.Y - y) >= blockDist)
                            continue;

                        if (entity == nextEntity)
                            continue;

                        tryMove.IntersectEntities2D.Add(nextEntity);
                        bool overlapsZ = isMissile ?
                            entity.OverlapsMissileClipZ(nextEntity, WorldStatic.MissileClip) : entity.OverlapsZ(nextEntity);

                        // Note: Flags.Special is set when the definition is applied using Definition.IsType(EntityDefinitionType.Inventory)
                        // This flag can be modified by dehacked
                        if (overlapsZ && canPickup && nextEntity.Flags.Special)
                        {
                            // Set the next node - this pickup can be removed from the list
                            m_world.PerformItemPickup(entity, nextEntity);
                            continue;
                        }

                        if (entity.CanBlockEntity(nextEntity) && BlocksEntityZ(entity, nextEntity, tryMove, overlapsZ))
                        {
                            tryMove.Success = false;
                            entity.BlockingEntity = nextEntity;
                            tryMove.BlockingEntity = nextEntity;
                            goto doneIsPositionValid;
                        }
                    }
                }

                tryMove.IntersectSectors.EnsureCapacity(intersectSectorLength + block.BlockLineCount * 2);

                for (int i = 0; i < block.BlockLineCount; i++)
                {
                    fixed (BlockLine* blockLine = &block.BlockLines[i])
                    {
                        if (m_checkedBlockLines[blockLine->LineId] == checkCounter)
                            continue;

                        m_checkedBlockLines[blockLine->LineId] = checkCounter;
                        if (blockLine->Segment.Intersects(boxMinX, boxMinY, boxMaxX, boxMaxY))
                        {
                            LineBlock blockType = LineBlocksEntity(entity, x, y, blockLine, tryMove);

                            Line line = blockLine->Line;
                            if (blockType != LineBlock.NoBlock)
                            {
                                entity.BlockingLine = line;
                                tryMove.BlockingLine = line;
                                tryMove.Success = false;
                                if (!entity.Flags.NoClip && line.HasSpecial)
                                    tryMove.ImpactSpecialLines.Add(line);
                                if (blockType == LineBlock.BlockStopChecking)
                                    goto doneIsPositionValid;
                            }

                            if (!entity.Flags.NoClip && line.HasSpecial)
                            {
                                if (blockType == LineBlock.NoBlock)
                                    tryMove.IntersectSpecialLines.Add(line);
                                else
                                    tryMove.ImpactSpecialLines.Add(line);
                            }

                            tryMove.IntersectSectors.Data[intersectSectorLength++] = blockLine->FrontSector;
                            if (blockLine->BackSector != blockLine->FrontSector)
                                tryMove.IntersectSectors.Data[intersectSectorLength++] = blockLine->BackSector!;
                        }
                    }
                }
            }
        }


    doneIsPositionValid:
        tryMove.IntersectSectors.Length = intersectSectorLength;

        if (entity.BlockingLine != null && Line.BlocksEntity(entity, entity.BlockingLine.Back == null, entity.BlockingLine.Flags, WorldStatic.Mbf21))
        {
            tryMove.Subsector = null;
            tryMove.Success = false;
            return false;
        }

        if (tryMove.LowestCeilingZ - tryMove.HighestFloorZ < entity.Height || entity.BlockingEntity != null)
        {
            tryMove.Subsector = null;
            tryMove.Success = false;
            return false;
        }

        tryMove.CanFloat = true;

        if (!entity.CheckDropOff(tryMove))
        {
            tryMove.Subsector = null;
            tryMove.Success = false;
        }

        return tryMove.Success;
    }

    private bool BlocksEntityZ(Entity entity, Entity other, TryMoveData tryMove, bool overlapsZ)
    {
        if (WorldStatic.InfinitelyTallThings && !entity.Flags.Missile && !other.Flags.Missile)
            return true;

        if (entity.Position.Z + entity.Height > other.Position.Z)
        {
            // This entity is higher than the other entity and requires step up checking
            m_lineOpening.SetTop(tryMove, other, entity.Flags.Missile && WorldStatic.MissileClip);
        }
        else
        {
            // This entity is within the other entity's Z or below
            m_lineOpening.SetBottom(tryMove, other);
        }

        tryMove.SetIntersectionData(m_lineOpening);

        bool isPlayer = entity.IsPlayer;
        // If blocking and not a player, do not check step passing below. Non-players can't step onto other things.
        if (overlapsZ && !isPlayer)
            return true;

        if (!overlapsZ)
            return false;

        return !m_lineOpening.CanPassOrStepThrough(entity);
    }

    public void MoveTo(Entity entity, double x, double y, TryMoveData tryMove)
    {
        entity.UnlinkFromWorld();

        double prevX = entity.Position.X;
        double prevY = entity.Position.Y;
        entity.Position.X = x;
        entity.Position.Y = y;

        LinkToWorld(entity, tryMove);

        if (entity.Flags.Teleport || entity.Flags.NoClip)
            return;

        for (int i = tryMove.IntersectSpecialLines.Length - 1; i >= 0 && i < tryMove.IntersectSpecialLines.Length; i--)
        {
            if (entity.Flags.Teleported)
                break;

            var line = tryMove.IntersectSpecialLines[i];
            bool fromFront = line.Segment.PerpDot(prevX, prevY) <= 0;
            if (fromFront != (line.Segment.PerpDot(entity.Position.X, entity.Position.Y) <= 0))
            {
                if (line.Special.IsTeleport() && !fromFront)
                    continue;

                if (!m_world.CanActivate(entity, line, ActivationContext.CrossLine))
                    continue;

                m_world.ActivateSpecialLine(entity, line, ActivationContext.CrossLine, fromFront);
            }
        }
    }

    private void HandleSlide(Entity entity, ref Vec2D stepDelta, ref int movesLeft, TryMoveData tryMove)
    {
        if (FindClosestBlockingLine(entity, stepDelta, out MoveInfo moveInfo) &&
            MoveCloseToBlockingLine(entity, stepDelta, moveInfo, out Vec2D residualStep, tryMove) &&
            !entity.Flags.Teleported)
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
        if (ShouldClearSlide(tryMove))
            ClearVelocityXY(entity);
        stepDelta.X = 0;
        stepDelta.Y = 0;
        movesLeft = 0;
    }

    private unsafe void CheckCornerTracerIntersection(Seg2D cornerTracer, Entity entity, ref MoveInfo moveInfo)
    {
        bool hit = false;
        double hitTime = double.MaxValue;
        Line? blockingLine = null;
        
        BlockmapSegIterator<Block> it = m_blockmap.Iterate(cornerTracer);
        var block = it.Next();
        while (block != null)
        {
            for (int i = 0; i < block.BlockLineCount; i++)
            {
                fixed (BlockLine* line = &block.BlockLines[i])
                {
                    if (cornerTracer.Intersection(line->Segment, out double time) && time > 0 && time < 1 &&
                        LineBlocksEntity(entity, entity.Position.X, entity.Position.Y, line, null) != LineBlock.NoBlock &&
                        time < hitTime)
                    {
                        hit = true;
                        hitTime = time;
                        blockingLine = line->Line;
                    }
                }
            }
            block = it.Next();
        }

        if (hit && hitTime < moveInfo.LineIntersectionTime)
            moveInfo = MoveInfo.From(blockingLine!, hitTime);
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
        Span<Vec2D> corners = stackalloc Vec2D[3];
        if (stepDelta.X >= 0)
        {
            if (stepDelta.Y >= 0)
            {
                corners[0].X = entity.Position.X - entity.Radius;
                corners[0].Y = entity.Position.Y + entity.Radius;

                corners[1].X = entity.Position.X + entity.Radius;
                corners[1].Y = entity.Position.Y + entity.Radius;

                corners[2].X = entity.Position.X + entity.Radius;
                corners[2].Y = entity.Position.Y - entity.Radius;
            }
            else
            {
                corners[0].X = entity.Position.X + entity.Radius;
                corners[0].Y = entity.Position.Y + entity.Radius;

                corners[1].X = entity.Position.X + entity.Radius;
                corners[1].Y = entity.Position.Y - entity.Radius;

                corners[2].X = entity.Position.X - entity.Radius;
                corners[2].Y = entity.Position.Y - entity.Radius;
            }
        }
        else
        {
            if (stepDelta.Y >= 0)
            {
                corners[0].X = entity.Position.X + entity.Radius;
                corners[0].Y = entity.Position.Y + entity.Radius;

                corners[1].X = entity.Position.X - entity.Radius;
                corners[1].Y = entity.Position.Y + entity.Radius;

                corners[2].X = entity.Position.X - entity.Radius;
                corners[2].Y = entity.Position.Y - entity.Radius;
            }
            else
            {
                corners[0].X = entity.Position.X - entity.Radius;
                corners[0].Y = entity.Position.Y + entity.Radius;

                corners[1].X = entity.Position.X - entity.Radius;
                corners[1].Y = entity.Position.Y - entity.Radius;

                corners[2].X = entity.Position.X + entity.Radius;
                corners[2].Y = entity.Position.Y - entity.Radius;
            }
        }

        Seg2D first = new Seg2D(corners[0], new Vec2D(corners[0].X + stepDelta.X, corners[0].Y + stepDelta.Y));
        Seg2D second = new Seg2D(corners[1], new Vec2D(corners[1].X + stepDelta.X, corners[1].Y + stepDelta.Y));
        Seg2D third = new Seg2D(corners[2], new Vec2D(corners[2].X + stepDelta.X, corners[2].Y + stepDelta.Y));

        CheckCornerTracerIntersection(first, entity, ref moveInfo);
        CheckCornerTracerIntersection(second, entity, ref moveInfo);
        CheckCornerTracerIntersection(third, entity, ref moveInfo);

        return moveInfo.IntersectionFound;
    }

    private bool MoveCloseToBlockingLine(Entity entity, Vec2D stepDelta, in MoveInfo moveInfo, out Vec2D residualStep, TryMoveData tryMove)
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

        tryMove.IntersectEntities2D.Length = 0;
        tryMove.IntersectSectors.Length = 0;
        Vec2D closeToLinePosition = entity.Position.XY + usedStepDelta;
        if (IsPositionValid(entity, closeToLinePosition.X, closeToLinePosition.Y, tryMove))
        {
            MoveTo(entity, closeToLinePosition.X, closeToLinePosition.Y, tryMove);
            return true;
        }

        return false;
    }

    private static void ReorientToSlideAlong(Entity entity, Line blockingLine, Vec2D residualStep, ref Vec2D stepDelta,
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
        entity.Velocity.X = stepProjection.X * Constants.DefaultFriction;
        entity.Velocity.Y = stepProjection.Y * Constants.DefaultFriction;

        double totalRemainingDistance = ((stepProjection * movesLeft) + residualProjection).Length();
        movesLeft += 1;
        stepDelta = unitDirection * totalRemainingDistance / movesLeft;
    }

    private bool AttemptAxisMove(Entity entity, Vec2D stepDelta, Axis2D axis, TryMoveData tryMove)
    {
        tryMove.IntersectEntities2D.Length = 0;
        tryMove.IntersectSectors.Length = 0;
        if (axis == Axis2D.X)
        {
            double nextX = entity.Position.X + stepDelta.X;
            if (IsPositionValid(entity, nextX, entity.Position.Y, tryMove))
            {
                MoveTo(entity, nextX, entity.Position.Y, tryMove);
                if (ShouldClearSlide(tryMove))
                    entity.Velocity.Y = 0;
                stepDelta.Y = 0;
                return true;
            }
        }
        else
        {
            double nextY = entity.Position.Y + stepDelta.Y;
            if (IsPositionValid(entity, entity.Position.X, nextY, tryMove))
            {
                MoveTo(entity, entity.Position.X, nextY, tryMove);
                if (ShouldClearSlide(tryMove))
                    entity.Velocity.X = 0;
                stepDelta.X = 0;
                return true;
            }
        }

        return false;
    }

    private static bool ShouldClearSlide(TryMoveData tryMove)
    {
        return !WorldStatic.VanillaMovementPhysics && tryMove.BlockingEntity != null;
    }

    private void MoveXY(Entity entity)
    {
        if (entity.IsDisposed)
            return;

        // Doom checked skull fly here. This is required to match dehacked functionality if the velocity is cleared but didn't actually hit anything.
        if (entity.Velocity.X == 0 && entity.Velocity.Y == 0)
        {
            if (entity.Flags.Skullfly)
                m_world.HandleEntityHit(entity, entity.Velocity, TryMoveData);
            return;
        }

        if (EnableMaxMoveXY)
        {
            if (entity.Velocity.X > MaxMoveXY || entity.Velocity.X < -MaxMoveXY)
                entity.Velocity.X = MathHelper.Clamp(entity.Velocity.X, -MaxMoveXY, MaxMoveXY);
            if (entity.Velocity.Y > MaxMoveXY || entity.Velocity.Y < -MaxMoveXY)
                entity.Velocity.Y = MathHelper.Clamp(entity.Velocity.Y, -MaxMoveXY, MaxMoveXY);
        }

        TryMoveXY(entity, entity.Position.X + entity.Velocity.X, entity.Position.Y + entity.Velocity.Y);
        if (entity.ShouldApplyFriction())
            ApplyFriction(entity);
        StopXYMovementIfSmall(entity);
    }

    private static double GetFrictionFromSectors(Entity entity)
    {
        if (entity.Flags.NoClip)
            return Constants.DefaultFriction;

        double lowestFriction = double.MaxValue;
        for (int i = 0; i < entity.IntersectSectors.Length; i++)
        {
            Sector sector = entity.IntersectSectors[i];
            if (entity.Position.Z != sector.ToFloorZ(entity.Position))
                continue;

            if (sector.Friction < lowestFriction && (sector.SectorEffect & SectorEffect.Friction) != 0)
                lowestFriction = sector.Friction;
        }

        if (lowestFriction == double.MaxValue)
            return Constants.DefaultFriction;

        return lowestFriction;
    }

    private void MoveZ(Entity entity)
    {
        if (entity.IsDisposed || m_world.WorldState == WorldState.Exit)
            return;

        // Have to check this first. Doom modifies the position first and then velocity.
        // This means z velocity isn't applied until the next tick after moving off a ledge.
        // Adds z velocity on the first tick, then adds -2 on the second instead of -1 on the first and -1 on the second.
        bool noVelocity = entity.Velocity.Z == 0;
        bool shouldApplyGravity = entity.ShouldApplyGravity();
        if (noVelocity && !shouldApplyGravity && !entity.Flags.Float && entity.OnEntity.Entity == null)
            return;

        bool stacked = entity.OnEntity.Entity != null || entity.OverEntity.Entity != null;
        if (entity.Flags.NoGravity && entity.ShouldApplyFriction())
            entity.Velocity.Z *= Constants.DefaultFriction;
        if (shouldApplyGravity)
            entity.Velocity.Z -= m_world.Gravity * entity.Properties.Gravity;

        double floatZ = entity.GetEnemyFloatMove();
        // Only return if OnEntity is null. Need to apply clamping to prevent issues with this entity floating when the entity beneath is no longer blocking.
        if (noVelocity && floatZ == 0 && entity.OnEntity.Entity == null)
            return;

        Vec3D previousVelocity = entity.Velocity;
        double newZ = entity.Position.Z + entity.Velocity.Z + floatZ;
        entity.Position.Z = newZ;

        // Passing MoveLinked emulates some vanilla functionality where things are not checked against linked sectors when they haven't moved
        ClampBetweenFloorAndCeiling(entity, null, smoothZ: true, entity.MoveLinked);

        if (entity.IsBlocked())
            m_world.HandleEntityHit(entity, previousVelocity, null);

        if (stacked)
            StackedEntityMoveZ(entity);
    }
}
