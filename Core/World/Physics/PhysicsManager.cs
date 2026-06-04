using Helion.Geometry;
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
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics.Blockmap;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using System;
using System.Runtime.CompilerServices;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Physics;

public struct MoveFactor(double moveFactor, double friction)
{
    public double Factor = moveFactor;
    public double Friction = friction;
}
readonly record struct SectorMoveEntityData(Entity Entity, double SaveZ, double PrevSaveZ, bool WasCrushing);

/// <summary>
/// Responsible for handling all the physics and collision detection in a
/// world.
/// </summary>
public sealed class PhysicsManager
{
    private const int MaxSlides = 3;
    private const double SlideStepBackTime = 1.0 / 32.0;
    private const double MinMovement = 0.0625;
    private const double SetEntityToFloorSpeedMax = 8;
    private const double MinMoveFactor = 32 / 65536.0;
    private const double MudMoveFactorLow = 15000 / 65536.0;
    private const double MudMoveFactorMed = MudMoveFactorLow * 2;
    private const double MudMoveFactorHigh = MudMoveFactorLow * 4;

    public const double MaxMoveXY = 30;
    public static readonly double LowestPossibleZ = Fixed.Lowest().ToDouble();

    public BlockmapTraverser BlockmapTraverser;
    public TryMoveData TryMoveData = new();
    public bool EnableMaxMoveXY = true;

    private IWorld m_world;
    private DataCache m_dataCache;
    private CompactBspTree m_bspTree;
    private BlockMap m_blockmap;
    private EntityManager m_entityManager;
    private IRandom m_random;
    private bool m_alwaysStickEntitiesToFloor;
    private readonly LineOpening m_lineOpening = new();
    private readonly LineOpening m_entityOpening = new();
    private readonly LineOpening m_testOpeningFront = new();
    private readonly LineOpening m_testOpeningBack = new();
    private readonly DynamicArray<Entity> m_crushEntities = new();
    private readonly DynamicArray<Entity> m_sectorMoveEntities = new();
    private readonly DynamicArray<Entity> m_sectorMoveEntitiesNoBlockMap = new();
    private readonly DynamicArray<SectorMoveEntityData> m_sectorMoveEntitiesData = new();
    private readonly DynamicArray<Entity> m_onEntities = new();
    private readonly Comparison<Entity> m_sectorMoveOrderComparer = new(SectorEntityMoveOrderCompare);
    private readonly DynamicArray<Entity> m_stackCrush = new();
    private readonly DynamicArray<Entity> m_clampIgnoreEntities = new();
    private readonly Sector m_testMoveSector3D = Sector.CreateDefault();

    private MoveLinkData m_moveLinkData;
    private CanPassData m_canPassData;
    private Entity m_clampIgnoreEntity;
    private bool m_stepMoving;
    private readonly Func<Entity, GridIterationStatus> m_canPassTraverseFunc;
    private readonly Func<Entity, GridIterationStatus> m_sectorMoveLinkClampAction;
    private readonly Func<Entity, GridIterationStatus> m_ignoreClampEntityTraverseAction;

    public PhysicsManager(IWorld world, CompactBspTree bspTree, BlockMap blockmap, IRandom random, bool alwaysStickEntitiesToFloor)
    {
        m_world = world;
        m_dataCache = world.DataCache;
        m_bspTree = bspTree;
        m_blockmap = blockmap;
        m_entityManager = world.EntityManager;
        m_random = random;
        BlockmapTraverser = new BlockmapTraverser(world, m_blockmap);
        m_sectorMoveLinkClampAction = new(HandleSectorMoveLinkClamp);
        m_canPassTraverseFunc = new(CanPassTraverse);
        m_ignoreClampEntityTraverseAction = new(IgnoreClampEntityTraverse);
        m_alwaysStickEntitiesToFloor = alwaysStickEntitiesToFloor;
        m_clampIgnoreEntity = null!;
    }

    public void UpdateTo(IWorld world, CompactBspTree bspTree, BlockMap blockmap, IRandom random, bool alwaysStickEntitiesToFloor)
    {
        m_world = world;
        m_dataCache = world.DataCache;
        m_bspTree = bspTree;
        m_blockmap = blockmap;
        m_entityManager = world.EntityManager;
        m_random = random;
        m_alwaysStickEntitiesToFloor = alwaysStickEntitiesToFloor;
        BlockmapTraverser.UpdateTo(world, blockmap);
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

    public void LinkToWorld(Entity entity, TryMoveData? tryMove = null, bool clampToLinkedSectors = true, bool checkLastBlock = false)
    {
        if (entity.Id < 0)
            return;

        if (!entity.Flags.NoBlockmap())
            m_blockmap.Link(entity, checkLastBlock);

        m_world.RenderBlockmap.RenderLink(entity);

        // Needs to be added to the sector list even with NoSector flag.
        // Doom used blockmap to manage things for sector movement.
        LinkToSectors(entity, tryMove);
        ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: true, clampToLinkedSectors, tryMove: tryMove);
    }

    /// <summary>
    /// Performs all the movement logic on the entity.
    /// </summary>
    /// <param name="entity">The entity to move.</param>
    public void Move(Entity entity)
    {
        entity.BlockingEntity = null;
        entity.BlockingBlockLineIndex = -1;
        entity.BlockingSectorPlane = null;
        entity.BlockingSector3D = null;
        MoveXY(entity);
        MoveZ(entity);
        entity.Flags.ClearIgnoreDropOff();
    }

    public void EntityFallCheck(DynamicArray<Entity> entities)
    {
        for (int i = entities.Length - 1; i >= 0; i--)
        {
            var entity = entities.Data[i];
            if (entity == null || entity.IsDisposed)
                continue;

            var onEntity = entity.OnEntity();
            if (
                (onEntity == null && entity.HadOnEntity) 
                ||
                (onEntity != null && (onEntity.Position.Z + onEntity.Height < entity.Position.Z || 
                (onEntity.Sector3D == null && onEntity.MidTexLine == null && !onEntity.Overlaps2D(entity)))))
            {
                ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: false, clampToLinkedSectors: true);
            }
        }
    }

    public SectorMoveStatus MoveSectorZ(double speed, double destZ, SectorMoveSpecial moveSpecial, Sector sectorEntities, 
        bool checkSector3D = true, SectorPlane? resetPlane = null, bool solid = true)
    {
        var sector = moveSpecial.Sector;
        var sectorPlane = moveSpecial.SectorPlane;
        var moveData = moveSpecial.MoveData;
        var moveType = moveSpecial.MoveData.SectorMoveType;
        var startZ = sectorPlane.Z;
        if (!m_world.Config.Compatibility.VanillaSectorPhysics && IsSectorMovementBlocked(sector, startZ, destZ, moveSpecial))
            return SectorMoveStatus.Blocked | SectorMoveStatus.Stop;

        // Move lower entities first to handle stacked entities
        // Ordering by Id is only required for EntityRenderer nudging to prevent z-fighting
        m_sectorMoveEntities.Clear();
        m_sectorMoveEntitiesData.Clear();
        GetSectorMoveOrderedEntities(m_sectorMoveEntities, m_sectorMoveEntitiesNoBlockMap, sectorEntities);

        // Save the Z value because we are only checking if the dest is valid
        // If the move is invalid because of a blocking entity then it will not be set to destZ
        Entity? highestBlockEntity = null;
        double? highestBlockHeight = 0.0;
        bool highestBlockEntityWasCrushing = false;
        SectorMoveStatus status = SectorMoveStatus.Success;
        sectorPlane.PrevZ = startZ;
        sectorPlane.SetZ(destZ);

        bool isCompleted = moveSpecial.IsFinalDestination(destZ);
        // Doors can't be part of the clip check. Maps are reliant on this behavior (e.g. Going Down Turbo MAP23 invul)
        if (!moveSpecial.IsDoor && !m_world.Config.Compatibility.VanillaSectorPhysics && IsSectorMovementBlocked(sector, startZ, destZ, moveSpecial))
        {
            FixPlaneClip(sector, sectorPlane, moveType);
            status = SectorMoveStatus.Blocked | SectorMoveStatus.Stop;
        }

        if (solid)
        {
            for (int i = 0; i < m_sectorMoveEntities.Length; i++)
            {
                var entity = m_sectorMoveEntities[i];
                var sectorMoveEntityData = new SectorMoveEntityData(entity, entity.Position.Z, entity.PrevPosition.Z, entity.IsCrushing());
                m_sectorMoveEntitiesData.Add(sectorMoveEntityData);

                var prevVelocityZ = entity.Velocity.Z;
                var entityShouldStick = startZ > destZ && entity.OnGround &&
                    (m_alwaysStickEntitiesToFloor || SpeedShouldStickToFloor(speed));

                // At slower speeds we need to set entities to the floor
                // Otherwise the entity will fall and hit the floor repeatedly creating a weird bouncing effect
                if (entityShouldStick && (entity.IntersectMidTexLines.Length > 0 || moveType == SectorPlaneFace.Floor))
                {
                    var floorZ = moveType == SectorPlaneFace.Floor ? destZ : entity.Position.Z;
                    var onEntity = entity.OnEntity();
                    if (onEntity != null)
                    {
                        if (onEntity.MidTexLine != null)
                            onEntity = onEntity.MidTexLine.GetMidTexEntity(m_world);
                        else if (onEntity.Sector3D != null)
                            onEntity = onEntity.Sector3D.GetSectorEntity3D();

                        floorZ = onEntity.Position.Z + onEntity.Height;
                    }

                    // Only set for 3D sector if the on entity matches the moving floor.
                    if (moveSpecial.MoveData.Sector3D == null || onEntity?.Sector3D?.ControlSector == moveSpecial.MoveData.Sector3D.ControlSector)
                    {
                        entity.Position.Z = floorZ;
                        // Setting this so SetEntityBoundsZ does not mess with forcing this entity to to the floor
                        // Otherwise this is a problem with the instant lift hack
                        entity.PrevPosition.Z = entity.Position.Z;
                    }
                }

                // If the move distance is higher than entity height (usually instant floors) then check entities this entity is clipped with.
                // They can't be processed for 3d checks because it will incorrectly block sector movement.
                // See InstantMoveSectorNotBlockedByClippedEntities
                if (!sectorMoveEntityData.WasCrushing && Math.Abs(startZ - destZ) >= entity.Height)
                    SetClampIgnoreEntities(entity);

                ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: false, clampToLinkedSectors: SectorMoveLinkedClampCheck(entity));

                // Check for missile hitting floor/ceiling. Doom would only explode on z movement so check for z velocity.
                if (entity.Flags.Missile() && prevVelocityZ != 0)
                    m_world.HandleEntityHit(entity, entity.Velocity, null);

                var thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;
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
                var entity = m_sectorMoveEntities[i];
                if (entity.IsDisposed)
                    continue;

                ClampBetweenFloorAndCeiling(entity, entity.IntersectSectors, smoothZ: false, clampToLinkedSectors: SectorMoveLinkedClampCheck(entity));
                var entityMoveData = m_sectorMoveEntitiesData[i];
                entity.PrevPosition.Z = entityMoveData.PrevSaveZ;
                // This allows the player to pickup items like the original
                if (entity.IsPlayer && !entity.Flags.NoClip())
                    IsPositionValid(entity, entity.Position.X, entity.Position.Y, TryMoveData);

                if ((moveType == SectorPlaneFace.Ceiling && startZ < destZ) ||
                    (moveType == SectorPlaneFace.Floor && startZ > destZ))
                    continue;

                var thingZ = entity.OnGround ? entity.HighestFloorZ : entity.Position.Z;
                if (thingZ + entity.GetClampHeight() > entity.LowestCeilingZ)
                {
                    if (entity.Flags.Dropped())
                    {
                        m_entityManager.Destroy(entity);
                        continue;
                    }

                    // Need to gib things even when not crushing and do not count as blocking
                    if (entity.Flags.Corpse() && !entity.Flags.DontGib() && entity.Health <= 0)
                    {
                        SetToGiblets(entity);
                        continue;
                    }

                    // Doom checked against shootable instead of solid...
                    if (!entity.Flags.Shootable())
                        continue;

                    if (moveData.Crush != null)
                    {
                        status |= SectorMoveStatus.Crushed;

                        if (moveData.Crush.Value.CrushMode == ZDoomCrushMode.Hexen || moveData.Crush.Value.Damage == 0)
                        {
                            highestBlockEntity = entity;
                            highestBlockHeight = entity.Height;
                            highestBlockEntityWasCrushing = entityMoveData.WasCrushing;
                            status |= SectorMoveStatus.Blocked;
                        }
                        
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

            if (highestBlockEntity != null && highestBlockHeight.HasValue && !highestBlockEntity.IsDead())
            {
                double diff = 0;
                // Set the sector Z to the difference of the blocked height (only works if not being crushed)
                // Could probably do something fancy to figure this out if the entity is being crushed, but this is quite rare
                if ((moveData.Flags & SectorMoveFlags.EntityBlockMovement) != 0 || highestBlockEntityWasCrushing || isCompleted)
                {
                    sectorPlane.SetZ(startZ);
                    resetPlane?.SetZ(startZ);
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
                    var set = startZ + diff;
                    sectorPlane.SetZ(set);
                    resetPlane?.SetZ(set);
                }

                // Entity blocked movement, reset all entities in moving sector after resetting sector Z
                for (int i = 0; i < m_sectorMoveEntities.Length; i++)
                {
                    var relinkEntity = m_sectorMoveEntities[i];
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

            CheckSectorMoveMissileClip(m_sectorMoveEntitiesNoBlockMap, sector, sectorPlane, moveType);

            m_clampIgnoreEntities.Clear();
            m_crushEntities.Clear();
            m_sectorMoveEntities.Clear();
            m_sectorMoveEntitiesNoBlockMap.Clear();
        }

        // If an entity is blocking this and the destination is blocked then we need to stop to match vanilla behavior.
        if (isCompleted && status == SectorMoveStatus.Blocked)
            return SectorMoveStatus.Blocked | SectorMoveStatus.Stop;

        if (status == (SectorMoveStatus.Blocked | SectorMoveStatus.Stop))
            return status;

        if (WorldStatic.Sector3D && checkSector3D && sector.TaggedSectors3D.Length > 0)
        {
            status = TestMoveSector3D(speed, destZ, startZ, moveSpecial, sector, sectorPlane, moveType);

            if ((status & (SectorMoveStatus.Blocked | SectorMoveStatus.Stop)) != 0)
                sectorPlane.SetZ(startZ);
        }

        return status;
    }

    private void CheckSectorMoveMissileClip(DynamicArray<Entity> entities, Sector sector, SectorPlane sectorPlane, SectorPlaneFace moveType)
    {
        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities.Data[i];
            if (!entity.Flags.Missile())
                continue;

            if (WorldStatic.Sector3D && sector.Sector3D != null)
            {
                var entityTopZ = entity.Position.Z + entity.GetClampHeight();
                var entityBottomZ = entity.Position.Z;
                if (entityBottomZ <= sector.Sector3D.ControlTop.Z && entityTopZ >= sector.Sector3D.ControlBottom.Z)
                {
                    entity.BlockingSectorPlane = sector.Sector3D.ControlSector.GetSectorPlane(moveType.Flip());
                    entity.BlockingSector3D = sector.Sector3D;
                    m_world.HandleEntityHit(entity, entity.Velocity, null);
                }
                continue;
            }

            if ((moveType == SectorPlaneFace.Floor && sectorPlane.Z > entity.Position.Z) ||
                (moveType == SectorPlaneFace.Ceiling && sectorPlane.Z < entity.Position.Z + entity.GetClampHeight()))
            {
                entity.BlockingSectorPlane = sectorPlane;
                m_world.HandleEntityHit(entity, entity.Velocity, null);
            }
        }
    }

    private SectorMoveStatus TestMoveSector3D(double speed, double destZ, double startZ, SectorMoveSpecial moveSpecial, Sector sector, SectorPlane sectorPlane, SectorPlaneFace face)
    {
        for (int i = 0; i < sector.TaggedSectors3D.Length; i++)
        {
            var testFace = face.Flip();
            var sector3D = sector.TaggedSectors3D[i];
            m_testMoveSector3D.Ceiling.SetZ(sector3D.ControlTop.Z);
            m_testMoveSector3D.Floor.SetZ(sector3D.ControlBottom.Z);
            m_testMoveSector3D.Sector3D = sector3D;
            var testMovePlane = m_testMoveSector3D.GetSectorPlane(testFace);
            var testOpposingMovePlane = m_testMoveSector3D.GetSectorPlane(face);

            testMovePlane.SetZ(startZ);
            testOpposingMovePlane.SetZ(sector3D.GetOpposingPlane3D(testFace, startZ).Z);
            moveSpecial.Sector = m_testMoveSector3D;
            moveSpecial.SectorPlane = testMovePlane;
            moveSpecial.MoveData.SectorMoveType = testFace;
            moveSpecial.MoveData.Sector3D = sector3D;

            var status = MoveSectorZ(speed, destZ, moveSpecial, sector3D.ParentSector, checkSector3D: false, resetPlane: sectorPlane, solid: sector3D.IsSolid);

            moveSpecial.Sector = sector;
            moveSpecial.SectorPlane = sectorPlane;
            moveSpecial.MoveData.SectorMoveType = face;
            moveSpecial.MoveData.Sector3D = null;

            if ((status & SectorMoveStatus.Blocked) != 0)
                return status;
        }       

        return SectorMoveStatus.Success;
    }

    private void SetClampIgnoreEntities(Entity entity)
    {
        m_clampIgnoreEntity = entity;
        m_clampIgnoreEntities.Clear();
        BlockmapTraverser.EntityTraverse(entity.GetBox2D(), m_ignoreClampEntityTraverseAction);
    }

    private GridIterationStatus IgnoreClampEntityTraverse(Entity checkEntity)
    {
        if (!checkEntity.Flags.Solid())
            return GridIterationStatus.Continue;

        double currentZ = checkEntity.Position.Z;
        // Find the original Z value if this entity is currently being moved by a sector.
        for (int i = m_sectorMoveEntitiesData.Length - 1; i >= 0; i--)
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
        if (entity.MoveLinked || entity.Flags.NoClip())
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
        if (!checkEntity.Flags.Solid() || checkEntity.Flags.Corpse() || checkEntity.Flags.NoClip() || m_moveLinkData.Entity.Id == checkEntity.Id)
            return GridIterationStatus.Continue;

        if (m_moveLinkData.Height > checkEntity.Position.Z)
        {
            m_moveLinkData.Success = false;
            return GridIterationStatus.Stop;
        }
        return GridIterationStatus.Continue;
    }

    private void GetSectorMoveOrderedEntities(DynamicArray<Entity> entities, DynamicArray<Entity> noBlockMapEntities, Sector sectorEntities)
    {
        var node = sectorEntities.Entities.Head;
        while (node != null)
        {
            var entity = node.Value;
            if (!EntityHasMovementSector(entity, sectorEntities))
                continue;
            if (entity.Flags.NoBlockmap())
                noBlockMapEntities.Add(entity);
            else
                m_sectorMoveEntities.Add(entity);

            node = node.Next;
        }

        entities.Sort(m_sectorMoveOrderComparer);
    }

    private static bool EntityHasMovementSector(Entity entity, Sector sector)
    {
        for (int i = entity.IntersectSectors.Length - 1; i >= 0; i--)
            if (entity.IntersectSectors[i] == sector)
                return true;

        return false;
    }

    // Constants and logic from WinMBF.
    // Credit to Lee Killough et al.
    public static MoveFactor GetMoveFactor(Entity entity)
    {
        double sectorFriction = GetFrictionFromSectors(entity);
        double moveFactor = Constants.DefaultMoveFactor;

        if (sectorFriction != Constants.DefaultFriction)
        {
            if (sectorFriction >= Constants.DefaultFriction)
                moveFactor = (0x10092 - sectorFriction * 65536.0) * 0x70 / 0x158 / 65536.0;
            else
                moveFactor = (sectorFriction * 65536.0 - 0xDB34) * 0xA / 0x80 / 65536.0;

            moveFactor = Math.Clamp(moveFactor, MinMoveFactor, double.MaxValue);
            // The move factor was based on 2048 being default in Boom.
            moveFactor /= Constants.DefaultFrictionFactor;
        }

        if (sectorFriction < Constants.DefaultFriction)
        {
            double momentum = entity.Velocity.XY.LengthSquared();
            if (momentum > MudMoveFactorHigh * MudMoveFactorHigh)
                moveFactor *= 8;
            else if (momentum > MudMoveFactorMed * MudMoveFactorMed)
                moveFactor *= 4;
            else if (momentum > MudMoveFactorLow * MudMoveFactorLow)
                moveFactor *= 2;
        }

        return new(moveFactor, sectorFriction);
    }

    private static bool IsSectorMovementBlocked(Sector sector, double startZ, double destZ, SectorMoveSpecial moveSpecial)
    {
        if (moveSpecial.MoveData.SectorMoveType == SectorPlaneFace.Floor && destZ < startZ)
            return false;

        if (moveSpecial.MoveData.SectorMoveType == SectorPlaneFace.Ceiling && destZ > startZ)
            return false;

        if (sector.Sector3D != null)
            return sector.Sector3D.ControlTop.Z < sector.Sector3D.ControlBottom.Z;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            var checkEntity = node.Value;
            var overEntity = checkEntity.OverEntity();
            if (overEntity != null && ContainsEntity(crushEntities, overEntity))
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

            if (!crushEntity.IsDead() && m_world.DamageEntity(crushEntity, null, crush.Damage, DamageType.Normal) &&
                !crushEntity.Flags.NoBlood() && !crushEntity.IsDisposed)
            {
                Vec3D pos = crushEntity.Position;
                pos.Z += crushEntity.Height / 2;
                Entity? blood = m_entityManager.Create(crushEntity.GetBloodDefinition(), pos, 0, 0, 0, default);
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
        for (int i = entities.Length - 1; i >= 0; i--)
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
            entity.Flags.SetCrushGiblets();
            return;
        }

        m_entityManager.Destroy(entity);
        m_entityManager.Create(WorldStatic.RealGibs, entity.Position, 0, 0, 0, default);
    }

    private static void PushUpBlockingEntity(Entity pusher)
    {
        var lowCeilEntity = pusher.LowestCeilingEntity();
        if (lowCeilEntity == null)
            return;

        if (lowCeilEntity.Flags.ActLikeBridge())
            return;

        lowCeilEntity.Position.Z = pusher.Position.Z + pusher.Height;
    }

    private static void PushDownBlockingEntities(Entity pusher)
    {
        // Because of how ClampBetweenFloorAndCeiling works, try to push down the entire stack and stop when something clips a floor
        if (pusher.HighestFloorObject is Sector && pusher.HighestFloorZ > pusher.LowestCeilingZ - pusher.Height)
            return;

        pusher.Position.Z = pusher.LowestCeilingZ - pusher.Height;

        var onEntity = pusher.OnEntity();
        if (onEntity != null)
        {
            Entity? current = onEntity;
            while (current != null)
            {
                if (current.HighestFloorObject is Sector && current.HighestFloorZ > pusher.Position.Z - current.Height)
                    return;

                current.Position.Z = pusher.Position.Z - current.Height;
                pusher = current;
                current = pusher.OnEntity();
            }
        }
    }

    private LineBlock LineBlocksEntity(Entity entity, double x, double y, ref BlockLine line, TryMoveData tryMove, bool dropOff, out Sector3D? blockingSector3D)
    {
        blockingSector3D = null;
        if (Line.BlocksEntity(entity, x, y, line.Segment, line.OneSided, line.BlockFlags, WorldStatic.Mbf21))
            return LineBlock.BlockStopChecking;

        if (line.OneSided)
            return LineBlock.NoBlock;

        LineOpening opening;
        if (dropOff)
        {
            if (WorldStatic.Sector3D)
            {
                opening = GetLineOpeningWithDropoff3D(entity, x, y, ref line);
                tryMove.SetIntersectionData3D(opening, entity);
            }
            else
            {
                opening = GetLineOpeningWithDropoff(entity, x, y, ref line);
                tryMove.SetIntersectionData3D(opening, entity);
            }
        }
        else
        {
            if (WorldStatic.Sector3D)
            {
                opening = GetLineOpeningWithDropoff3D(entity, x, y, ref line);
                tryMove.SetIntersectionData3D(opening, entity);
            }
            else
            {
                opening = GetLineOpening(line.FrontSector, line.BackSector!);
            }
        }

        if (line.BlockFlags.MidTex3D && !line.OneSided && (!entity.Flags.Missile() || !line.BlockFlags.BlockMissileMidTex3D))
        {
            var midTexEntity = GetMidTexEntity(line.LineId);
            if (BlocksEntityZ(entity, midTexEntity, tryMove, entity.OverlapsZ(midTexEntity), m_entityOpening))
                return LineBlock.BlockContinue;    
        }

        if (WorldStatic.Sector3D)
        {
            if (LineBlockSector3D(entity, tryMove, line.FrontSector, ref blockingSector3D))
                return LineBlock.BlockContinue;

            if (line.BackSector != null)
            {
                if (LineBlockSector3D(entity, tryMove, line.BackSector, ref blockingSector3D))
                    return LineBlock.BlockContinue;
            }
        }

        if (opening.CanPassOrStepThrough(entity))
            return LineBlock.NoBlock;

        return LineBlock.BlockContinue;
    }

    private bool LineBlockSector3D(Entity entity, TryMoveData tryMove, Sector sector, ref Sector3D? blockingSector3D)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3D = sector.Sectors3D[i];
            if (!sector3D.IsSolid)
                continue;

            var sectorEntity = sector3D.GetSectorEntity3D();
            if (BlocksEntityZ(entity, sectorEntity, tryMove, entity.OverlapsZ(sectorEntity), m_entityOpening))
            {
                blockingSector3D = sector3D;
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entity GetMidTexEntity(int lineId) =>
        m_world.Lines[lineId].GetMidTexEntity(m_world);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LineOpening GetLineOpening(Sector front, Sector back)
    {
        m_lineOpening.Set(front, back);
        return m_lineOpening;
    }

    public LineOpening GetLineOpeningWithDropoff(Entity entity, double x, double y, ref BlockLine line)
    {
        Sector front = line.FrontSector;
        Sector back = line.BackSector!;
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
            m_lineOpening.DropOffZ = back.Floor.Z;
        }
        else
        {
            m_lineOpening.FloorZ = back.Floor.Z;
            m_lineOpening.FloorSector = back;
            m_lineOpening.DropOffZ = front.Floor.Z;
        }

        m_lineOpening.OpeningHeight = m_lineOpening.CeilingZ - m_lineOpening.FloorZ;
        m_lineOpening.HasDropOff3D = false;
        return m_lineOpening;
    }

    public LineOpening GetLineOpeningWithDropoff3D(Entity entity, double x, double y, ref BlockLine line)
    {
        var front = line.FrontSector;
        var back = line.BackSector!;

        if (front.Sectors3D.Length == 0 && back.Sectors3D.Length == 0)
            return GetLineOpeningWithDropoff(entity, x, y, ref line);

        GetLineOpening(front, back);
        m_lineOpening.DropOffZ = front.Floor.Z;
        SetOpeningPlanes3D(entity, front, back);

        if (m_testOpeningFront.FloorZ > m_testOpeningBack.FloorZ)
            m_lineOpening.DropOffZ = Math.Max(m_testOpeningBack.DropOffZ_3D, m_lineOpening.DropOffZ);
        else
            m_lineOpening.DropOffZ = Math.Max(m_testOpeningFront.DropOffZ_3D, m_lineOpening.DropOffZ);

        m_lineOpening.OpeningHeight = m_lineOpening.CeilingZ - m_lineOpening.FloorZ;
        return m_lineOpening;
    }

    private void SetOpeningPlanes3D(Entity entity, Sector front, Sector back)
    {
        SetLineOpening3D(m_testOpeningFront, front, entity, front, back);
        SetLineOpening3D(m_testOpeningBack, back, entity, front, back);

        var highestFloorOpening = m_testOpeningBack.FloorZ > m_testOpeningFront.FloorZ ? m_testOpeningBack : m_testOpeningFront;
        if (highestFloorOpening.FloorZ > m_lineOpening.FloorZ)
        {
            m_lineOpening.FloorZ = highestFloorOpening.FloorZ;
            m_lineOpening.FloorSector = highestFloorOpening.FloorSector;
        }

        var lowestCeilOpening = m_testOpeningBack.CeilingZ < m_testOpeningFront.CeilingZ ? m_testOpeningBack : m_testOpeningFront;
        if (lowestCeilOpening.CeilingZ < m_lineOpening.CeilingZ)
        {
            m_lineOpening.CeilingZ = lowestCeilOpening.CeilingZ;
            m_lineOpening.CeilingSector = lowestCeilOpening.CeilingSector;
        }
    }

    private void SetLineOpening3D(LineOpening testOpening, Sector useSector3D, Entity entity, Sector front, Sector back)
    {
        testOpening.DropOffZ_3D = m_lineOpening.DropOffZ;

        if (useSector3D.Sectors3D.Length > 0)
        {
            var anySolid = false;
            for (int i = 0; i < useSector3D.Sectors3D.Length; i++)
            {
                var sector3D = useSector3D.Sectors3D[i];
                if (!sector3D.IsSolid)
                    continue;

                anySolid = true;
                var entity3D = sector3D.GetSectorEntity3D();
                SetEntityLineOpening(entity, entity3D, TryMoveData, testOpening, false);

                var top = entity3D.Position.Z + entity3D.Height;
                if (top - entity.GetMaxStepHeight() <= entity.Position.Z && top > testOpening.DropOffZ_3D)
                    testOpening.DropOffZ_3D = top;
            }

            if (anySolid)
            {
                m_lineOpening.HasDropOff3D = true;
                return;
            }
        }

        testOpening.Set(front, back);
        testOpening.DropOffZ = testOpening.FloorZ;
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
        TryMoveData ? tryMove = null)
    {
        Invariant(intersectSectors == null || ReferenceEquals(entity.IntersectSectors, intersectSectors), $"Intersect sectors not owned by entity.");

        if (entity.IsDisposed || entity.Definition.Type == EntityType.BulletPuff)
            return;
        if (entity.Flags.NoClip() && entity.Flags.NoGravity())
            return;

        double prevHighestFloorZ = entity.HighestFloorZ;
        var prevOnEntity = entity.OnEntity();
        SetEntityBoundsZ(entity, intersectSectors, clampToLinkedSectors, tryMove);
        entity.SetOnEntity(null);

        double lowestCeil = entity.LowestCeilingZ;
        double highestFloor = entity.HighestFloorZ;

        // short.MinValue checks are to emulate the fixed point overflow required for mikoportals.
        if (entity.Position.Z + entity.Height > lowestCeil || highestFloor <= short.MinValue)
        {
            if (entity.Velocity.Z > 0)
                entity.Velocity.Z = 0;

            entity.Position.Z = lowestCeil - entity.GetClampHeight();

            if (highestFloor > short.MinValue)
                SetBlockingCeiling(entity);
        }

        bool clippedFloor = entity.Position.Z <= highestFloor;
        if (entity.Position.Z <= highestFloor && highestFloor < short.MaxValue)
        {
            var highestEntity = entity.HighestFloorEntity();
            if (highestEntity != null &&
                highestEntity.Position.Z + highestEntity.Height <= entity.Position.Z + entity.GetMaxStepHeight())
            {
                entity.SetOnEntity(highestEntity);
            }

            for (int i = m_onEntities.Length - 1; i >= 0; i--)
                m_onEntities[i].SetOverEntity(entity);

            if (clippedFloor)
                SetBlockingFloor(entity);

            SetEntityOnFloorOrEntity(entity, highestFloor, smoothZ && prevHighestFloorZ != entity.HighestFloorZ);
        }

        if (prevOnEntity != null && prevOnEntity != entity.OnEntity())
            prevOnEntity.SetOverEntity(null);

        if (WorldStatic.Sector3D)
            entity.SetWaterSubmersionLevel();

        entity.CheckOnGround();
        m_onEntities.Clear();
    }

    private static void SetBlockingFloor(Entity entity)
    {
        var blockEntity = entity.HighestFloorEntity();
        if (WorldStatic.Sector3D && blockEntity != null && blockEntity.Sector3D != null)
        {
            entity.BlockingSectorPlane = blockEntity.Sector3D.ControlTop;
            return;
        }

        if (blockEntity != null)
            entity.BlockingEntity = blockEntity;
        else if (entity.BlockingSectorPlane == null && entity.Velocity.Z < 0)
            entity.BlockingSectorPlane = entity.HighestFloorSector.Floor;
    }

    private static void SetBlockingCeiling(Entity entity)
    {
        var blockEntity = entity.LowestCeilingEntity();
        if (WorldStatic.Sector3D && blockEntity != null && blockEntity.Sector3D != null)
        {
            entity.BlockingSectorPlane = blockEntity.Sector3D.ControlBottom;
            return;
        }

        if (blockEntity != null)
            entity.BlockingEntity = blockEntity;
        else
            entity.BlockingSectorPlane = entity.LowestCeilingSector.Ceiling;
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
            bool hasIntersectMidTexLines;
            if (tryMove != null)
                hasIntersectMidTexLines = tryMove.IntersectMidTexLines.Length > 0;
            else
                hasIntersectMidTexLines = entity.IntersectMidTexLines.Length > 0;

            if (!hasIntersectMidTexLines)
            {
                entity.HighestFloorZ = highestFloorZ;
                entity.LowestCeilingZ = lowestCeilZ;
                entity.HighestFloorSector = highestFloor;
                entity.LowestCeilingSector = lowestCeiling;
                entity.HighestFloorObject = highestFloor;
                entity.LowestCeilingObject = lowestCeiling;
                return;
            }
        }

        var canPass = entity.Flags.CanPass();
        // Only check against other entities if CanPass is set (height sensitive clip detection)
        if (!entity.Flags.NoClip())
        {
            m_canPassData.Entity = entity;
            m_canPassData.HighestFloorEntity = highestFloorEntity;
            m_canPassData.LowestCeilingEntity = lowestCeilingEntity;
            m_canPassData.EntityTopZ = entity.Position.Z + entity.Height;
            m_canPassData.HighestFloorZ = highestFloorZ;
            m_canPassData.LowestCeilZ = lowestCeilZ;
            m_canPassData.LowestCeilLight3D = double.MaxValue;
            m_canPassData.CeilingSector3D = null;
            m_canPassData.ClampToLinkedSectors = clampToLinkedSectors;
            WorldStatic.CheckCounter++;

            if (tryMove == null)
            {
                // Get intersecting entities here - They are not stored in the entity because other entities can move around after this entity has linked
                if (canPass)
                    m_world.BlockmapTraverser.EntityTraverse(entity.GetBox2D(), m_canPassTraverseFunc);

                for (int i = entity.IntersectMidTexLines.Length - 1; i >= 0; i--)
                    CanPassTraverse(GetMidTexEntity(entity.IntersectMidTexLines[i]));

                if (WorldStatic.Sector3D)
                {
                    for (int i = entity.IntersectSectors.Length - 1; i >= 0; i--)
                        CanPassTraverseSector3D(entity.IntersectSectors.Data[i]);
                }
            }
            else
            {
                if (canPass)
                {
                    for (int i = tryMove.IntersectEntities2D.Length - 1; i >= 0; i--)
                        CanPassTraverse(tryMove.IntersectEntities2D[i]);
                }

                for (int i = tryMove.IntersectMidTexLines.Length - 1; i >= 0; i--)
                    CanPassTraverse(GetMidTexEntity(tryMove.IntersectMidTexLines[i]));

                if (WorldStatic.Sector3D)
                {
                    for (int i = tryMove.IntersectSectors.Length - 1; i >= 0; i--)
                        CanPassTraverseSector3D(tryMove.IntersectSectors.Data[i]);
                }
            }

            if (WorldStatic.Sector3D)
                CanPassTraverseSector3D(entity.Sector);

            highestFloorEntity = m_canPassData.HighestFloorEntity;
            lowestCeilingEntity = m_canPassData.LowestCeilingEntity;
            highestFloorZ = m_canPassData.HighestFloorZ;
            lowestCeilZ = m_canPassData.LowestCeilZ;
            entity.LightCeilingSector3D = m_canPassData.CeilingSector3D;
        }

        entity.HighestFloorZ = highestFloorZ;
        entity.LowestCeilingZ = lowestCeilZ;
        entity.HighestFloorSector = highestFloor;
        entity.LowestCeilingSector = lowestCeiling;

        // Make checks inclusive to prioritize entity over sector. Otherwise this can cause issues with monsters on 3d bridges/midtex lines dropping of when they shouldn't.
        if (highestFloorEntity != null && highestFloorEntity.Position.Z + highestFloorEntity.Height >= highestFloor.Floor.Z)
            entity.SetHighestFloorEntity(highestFloorEntity);
        else
            entity.HighestFloorObject = highestFloor;

        if (lowestCeilingEntity != null && lowestCeilingEntity.Position.Z + lowestCeilingEntity.Height < lowestCeiling.Ceiling.Z)
            entity.SetLowestCeilingEntity(lowestCeilingEntity);
        else
            entity.LowestCeilingObject = lowestCeiling;
    }

    public void SetCeilingLightSector3D(Entity entity)
    {
        m_canPassData.Entity = entity;
        m_canPassData.HighestFloorEntity = entity.HighestFloorEntity();
        m_canPassData.LowestCeilingEntity = entity.LowestCeilingEntity();
        m_canPassData.EntityTopZ = entity.Position.Z + entity.Height;
        m_canPassData.HighestFloorZ = entity.HighestFloorZ;
        m_canPassData.LowestCeilZ = entity.LowestCeilingZ;
        m_canPassData.LowestCeilLight3D = double.MaxValue;
        m_canPassData.CeilingSector3D = null;
        m_canPassData.ClampToLinkedSectors = false;

        for (int i = entity.IntersectSectors.Length - 1; i >= 0; i--)
            CanPassTraverseSector3D(entity.IntersectSectors.Data[i]);

        CanPassTraverseSector3D(entity.Sector);

        entity.LightCeilingSector3D = m_canPassData.CeilingSector3D;
    }

    private void CanPassTraverseSector3D(Sector sector)
    {
        for (int i = 0; i < sector.Sectors3D.Length; i++)
        {
            var sector3D = sector.Sectors3D[i];
            if (sector3D.CheckCount == WorldStatic.CheckCounter)
                continue;

            sector3D.CheckCount = WorldStatic.CheckCounter;
            CanPassTraverse(sector3D.GetSectorEntity3D());

            if (sector3D.ControlTop.Z < m_canPassData.LowestCeilLight3D &&
                m_canPassData.Entity.Position.Z < sector3D.ControlTop.Z)
            {
                m_canPassData.CeilingSector3D = sector3D.LightBottom;
                m_canPassData.LowestCeilLight3D = m_canPassData.LowestCeilZ;
            }
        }
    }

    private GridIterationStatus CanPassTraverse(Entity intersectEntity)
    {
        var entity = m_canPassData.Entity;
        if (!intersectEntity.Flags.Solid() || intersectEntity.Flags.Corpse() || intersectEntity.Flags.NoClip() || entity == intersectEntity)
            return GridIterationStatus.Continue;

        for (int i = m_clampIgnoreEntities.Length - 1; i >= 0; i--)
        {
            if (m_clampIgnoreEntities[i] == intersectEntity)
                return GridIterationStatus.Continue;
        }

        double intersectTopZ = intersectEntity.Position.Z + intersectEntity.Height;
        if (entity.Flags.Missile() && WorldStatic.MissileClip)
            intersectTopZ = intersectEntity.GetMissileClipHeight(true);
        bool above = entity.PrevPosition.Z >= intersectTopZ;
        bool below = entity.PrevPosition.Z + entity.Height <= intersectEntity.PrevPosition.Z;
        bool clipped = false;
        bool addedOnEntity = false;
        if (above && entity.Position.Z < intersectTopZ)
            clipped = true;
        else if (below && m_canPassData.EntityTopZ > intersectEntity.Position.Z)
            clipped = true;

        if (!above && !below && !m_canPassData.ClampToLinkedSectors && !intersectEntity.Flags.ActLikeBridge())
            return GridIterationStatus.Continue;

        if (above)
        {
            // Need to check clipping coming from above, if we're above
            // or clipped through then this is our floor.
            if ((clipped || entity.Position.Z >= intersectTopZ) && intersectTopZ >= m_canPassData.HighestFloorZ)
            {
                if (m_canPassData.HighestFloorEntity != null && m_canPassData.HighestFloorEntity.Position.Z + m_canPassData.HighestFloorEntity.Height < m_canPassData.HighestFloorZ)
                    m_onEntities.Clear();

                if (CanPassEntityFloorPriorityCheck(intersectEntity, intersectTopZ))
                {
                    m_canPassData.HighestFloorEntity = intersectEntity;
                    m_canPassData.HighestFloorZ = intersectTopZ;
                }

                if (intersectTopZ == entity.Position.Z)
                {
                    addedOnEntity = true;
                    m_onEntities.Add(intersectEntity);
                }
            }
        }
        else if (below)
        {
            // Same check as above but checking clipping the ceiling.
            if ((clipped || m_canPassData.EntityTopZ <= intersectEntity.Position.Z) && intersectEntity.Position.Z <= m_canPassData.LowestCeilZ)
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

            if (CanPassEntityFloorPriorityCheck(intersectEntity, intersectTopZ))
            {
                m_canPassData.HighestFloorEntity = intersectEntity;
                m_canPassData.HighestFloorZ = intersectTopZ;
            }

            if (intersectTopZ == entity.Position.Z)
                m_onEntities.Add(intersectEntity);
        }

        return GridIterationStatus.Continue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CanPassEntityFloorPriorityCheck(Entity entity, double intersectTopZ)
    {
        if (m_canPassData.HighestFloorEntity == null || m_canPassData.HighestFloorEntity.Position.Z + m_canPassData.HighestFloorEntity.Height != intersectTopZ)
            return true;
        // Need to prioritize bridge things over everything else when the z heights are equal.
        return !m_canPassData.Entity.Flags.ActLikeBridge() && entity.Flags.ActLikeBridge();
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
        for (int i = intersectSectors.Length - 1; i >= 0; i--)
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

    private void LinkToSectors(Entity entity, TryMoveData? tryMove)
    {
        Precondition(entity.SectorNodes.Length == 0, "Forgot to unlink entity from blockmap");
        int checkCounter = ++WorldStatic.CheckCounter;
        Subsector centerSubsector;
        if (tryMove != null && tryMove.Subsector != null && tryMove.Success)
            centerSubsector = tryMove.Subsector;
        else
            centerSubsector = m_world.ToSubsector(entity.Position.X, entity.Position.Y);

        Sector centerSector = centerSubsector.Sector;
        centerSector.CheckCount = checkCounter;
        if (tryMove != null)
        {
            int intersectSectorLength = 0;
            entity.IntersectSectors.EnsureCapacity(tryMove.IntersectSectors.Length);
            entity.SectorNodes.EnsureCapacity(tryMove.IntersectSectors.Length);
            for (int i = tryMove.IntersectSectors.Length - 1; i >= 0; i--)
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

            entity.IntersectMidTexLines.AddRange(tryMove.IntersectMidTexLines);
        }
        else
        {
            var minX = entity.Position.X - entity.Radius;
            var minY = entity.Position.Y - entity.Radius;
            var maxX = entity.Position.X + entity.Radius;
            var maxY = entity.Position.Y + entity.Radius;
            var it = m_blockmap.CreateBoxIteration(minX, minY, maxX, maxY);
            for (int by = it.BlockStartY; by <= it.BlockEndY; by++)
            {
                for (int bx = it.BlockStartX; bx <= it.BlockEndX; bx++)
                {
                    ref var block = ref m_blockmap.Lines[by * it.Width + bx];
                    int count = block.BlockLineIndex + block.BlockLineCount;
                    for (int i = block.BlockLineIndex; i < count; i++)
                    {
                        ref var line = ref m_blockmap.BlockLines[i];                        
                        if (WorldStatic.CheckedLines[line.LineId] == checkCounter)
                            continue;

                        WorldStatic.CheckedLines[line.LineId] = checkCounter;

                        if (line.Segment.Intersects(minX, minY, maxX, maxY))
                        {
                            // Doomism: Ignore for moving sectors if blocked by flags only.
                            if (Line.BlocksEntity(entity, entity.Position.X, entity.Position.Y, line.Segment, line.OneSided, line.BlockFlags, WorldStatic.Mbf21))
                                goto doneLinkToSectors;

                            if (line.BlockFlags.MidTex3D)
                                entity.IntersectMidTexLines.Add(line.LineId);

                            if (line.FrontSector.CheckCount != checkCounter)
                            {
                                Sector sector = line.FrontSector;
                                sector.CheckCount = checkCounter;
                                entity.IntersectSectors.Add(sector);
                                entity.SectorNodes.Add(sector.Link(entity));
                            }

                            if (line.BackSector != null && line.BackSector!.CheckCount != checkCounter)
                            {
                                Sector sector = line.BackSector!;
                                sector.CheckCount = checkCounter;
                                entity.IntersectSectors.Add(sector);
                                entity.SectorNodes.Add(sector.Link(entity));
                            }
                        }                        
                    }
                }
            }
        }
doneLinkToSectors:
        entity.SubsectorId = centerSubsector.Id;
        entity.Sector = centerSector;
        entity.IntersectSectors.Add(centerSector);
        entity.SectorNodes.Add(centerSector.Link(entity));
    }

    public TryMoveData TryMoveXY(Entity entity, double x, double y, Action<Entity, TryMoveData>? onMoveTo = null)
    {
        TryMoveData.Clear();
        if (entity.Flags.NoClip())
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
                stepDelta.X /= numMoves;
                stepDelta.Y /= numMoves;
            }
        }

        bool success = true;
        Vec3D saveVelocity = entity.Velocity;
        int slideBlockLineId = -1;
        Entity? slideBlockEntity = null;
        m_stepMoving = true;

        for (int movesLeft = numMoves; movesLeft > 0; movesLeft--)
        {
            if ((stepDelta.X == 0 && stepDelta.Y == 0) || m_world.WorldState == WorldState.Exit)
                break;

            double nextX = entity.Position.X + stepDelta.X;
            double nextY = entity.Position.Y + stepDelta.Y;
            if (IsPositionValid(entity, nextX, nextY, TryMoveData) && entity.CheckDropOff(TryMoveData))
            {
                TryMoveData.SubMoveSuccess = true;
                entity.MoveLinked = true;
                MoveTo(entity, nextX, nextY, TryMoveData, onMoveTo);
                if (entity.Flags.Teleported())
                    return TryMoveData;

                if (TryMoveData.HasTouchy)
                    m_world.HandleEntityIntersections(entity, saveVelocity, TryMoveData);
                continue;
            }

            if (entity.BlockingBlockLineIndex != -1 && entity.PlayerObj != null && !entity.PlayerObj.IsVooDooDoll)
            {
                ref var line = ref m_world.Blockmap.BlockLines[entity.BlockingBlockLineIndex];
                if (Line.CanMoveOutOf(entity, nextX, nextY, line.Segment, line.BackSector == null) && !line.BlockFlags.MidTex3D)
                {
                    TryMoveData.BlockedLineClearsVelocity = false;
                    continue;
                }
            }

            if (entity.Flags.SlidesOnWalls() && slidesLeft > 0)
            {
                // BlockingLine and BlockingEntity will get cleared on HandleSlide(IsPositionValid) calls.
                // Carry them over so other functions after TryMoveXY can use them for verification.
                var blockingLineId = entity.BlockingBlockLineIndex;
                var blockingEntity = entity.BlockingEntity;
                HandleSlide(entity, ref stepDelta, ref movesLeft, TryMoveData);
                entity.BlockingBlockLineIndex = blockingLineId;
                entity.BlockingEntity = blockingEntity;
                if (slideBlockLineId == -1 && blockingLineId != -1)
                    slideBlockLineId = blockingLineId;
                if (slideBlockEntity == null && blockingEntity != null)
                    slideBlockEntity = blockingEntity;
                slidesLeft--;
                success = false;
                continue;
            }

            success = false;

            if (ShouldClearSlide(entity, TryMoveData))
            {
                entity.Velocity.X = 0;
                entity.Velocity.Y = 0;
            }

            break;
        }

        // Only required for ripper entities
        if (TryMoveData.SubMoveSuccess && entity.Flags.Ripper())
            m_world.HandleFinalizeEntityIntersections(entity, TryMoveData);

        if (!success)
        {
            if (slideBlockEntity != null && entity.BlockingEntity == null)
                entity.BlockingEntity = slideBlockEntity;
            if (slideBlockLineId != -1 && entity.BlockingBlockLineIndex == -1)
                entity.BlockingBlockLineIndex = slideBlockLineId;
            m_world.HandleEntityHit(entity, saveVelocity, TryMoveData);
        }

        m_stepMoving = false;
        TryMoveData.Success = success;
        return TryMoveData;
    }

    private const int PositionValidFlags1 = EntityFlags.SpecialFlag | EntityFlags.SolidFlag | EntityFlags.ShootableFlag;
    private const int PositionValidFlags2 = EntityFlags.TouchyFlag;

    public bool IsPositionValid(Entity entity, double x, double y)
    {
        TryMoveData.Clear();
        return IsPositionValid(entity, x, y, TryMoveData);
    }

    private bool IsPositionValid(Entity entity, double x, double y, TryMoveData tryMove)
    {
        if (!WorldStatic.InfinitelyTallThings && (entity.Flags.Flags1 & EntityFlags.FloatFlag) == 0 && !entity.IsPlayer)
        {
            var onEntity = entity.OnEntity();
            if (onEntity != null && (onEntity.Flags.Flags1 & EntityFlags.ActsLikeBridgeFlag) == 0)
                return false;
        }

        tryMove.Success = true;
        tryMove.LowestCeiling = entity.Sector;
        tryMove.HighestFloor = entity.Sector;
        tryMove.Subsector = null;
        tryMove.IntersectEntities2D.Length = 0;
        tryMove.IntersectSpecialLines.Length = 0;
        tryMove.IntersectMidTexLines.Length = 0;
        tryMove.IntersectSectors.Length = 0;

        // Kind of a hack for when a player is sliding on a wall since this gets called continually
        if (!m_stepMoving)
            tryMove.ImpactSpecialLines.Length = 0;

        var blockLineIndex = -1;
        tryMove.DropOffZ_3D = double.MaxValue;
        tryMove.Subsector = m_world.ToSubsector(x, y);
        var sector = tryMove.Subsector.Sector;
        tryMove.HighestFloorZ = sector.Floor.Z;
        tryMove.LowestCeilingZ = sector.Ceiling.Z;
        tryMove.DropOffZ = sector.Floor.Z;
        tryMove.HighestValidStepFloorZ = tryMove.HighestFloorZ;

        entity.BlockingBlockLineIndex = -1;
        entity.BlockingEntity = null;
        
        int checkCounter = ++WorldStatic.CheckCounter;
        bool isMissile = entity.Flags.Missile();
        bool checkEntities = isMissile || entity.Flags.Solid();
        bool canPickup = entity.Flags.Pickup();
        Entity? nextEntity;
                
        var boxMinX = x - entity.Radius;
        var boxMaxX = x + entity.Radius;
        var boxMinY = y - entity.Radius;
        var boxMaxY = y + entity.Radius;
        int blockStartX = Math.Max(0, (int)((boxMinX - m_blockmap.Bounds.Min.X) / m_blockmap.Dimension));
        int blockStartY = Math.Max(0, (int)((boxMinY - m_blockmap.Bounds.Min.Y) / m_blockmap.Dimension));
        int blockEndX = Math.Min((int)((boxMaxX - m_blockmap.Bounds.Min.X) / m_blockmap.Dimension), m_blockmap.Width - 1);
        int blockEndY = Math.Min((int)((boxMaxY - m_blockmap.Bounds.Min.Y) / m_blockmap.Dimension), m_blockmap.Height - 1);
        int intersectSectorLength = 0;

        for (int by = blockStartY; by <= blockEndY; by++)
        {
            for (int bx = blockStartX; bx <= blockEndX; bx++)
            {
                var index = by * m_blockmap.Width + bx;
                if (checkEntities)
                {
                    ref var blockEntities = ref m_blockmap.Entities[index];
                    var entityIndices = blockEntities.EntityIndices;
                    for (int i = blockEntities.EntityIndicesLength - 1; i >= 0; i--)
                    {
                        nextEntity = m_dataCache.Entities[entityIndices[i]];
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

                        tryMove.HasTouchy = tryMove.HasTouchy || nextEntity.Flags.Touchy();
                        tryMove.IntersectEntities2D.Add(nextEntity);
                        bool overlapsZ = isMissile ?
                            entity.OverlapsMissileClipZ(nextEntity, WorldStatic.MissileClip) : entity.OverlapsZ(nextEntity);

                        // Note: Flags.Special is set when the definition is applied using Definition.IsType(EntityDefinitionType.Inventory)
                        // This flag can be modified by dehacked
                        if (overlapsZ && canPickup && nextEntity.Flags.Special())
                        {
                            if (entity.PlayerObj != null)
                                m_world.PerformItemPickup(entity, nextEntity);
                            continue;
                        }

                        if (entity.CanBlockEntity(nextEntity) && BlocksEntityZ(entity, nextEntity, tryMove, overlapsZ, m_lineOpening))
                        {
                            tryMove.Success = false;
                            entity.BlockingEntity = nextEntity;
                            tryMove.BlockingEntity = nextEntity;
                            goto doneIsPositionValid;
                        }
                    }
                }

                ref var block = ref m_blockmap.Lines[index];
                tryMove.IntersectSectors.EnsureCapacity(intersectSectorLength + block.BlockLineCount * 2);

                int count = block.BlockLineIndex + block.BlockLineCount;
                for (int i = block.BlockLineIndex; i < count; i++)
                {
                    ref var blockLine = ref m_blockmap.BlockLines[i];
                    if (WorldStatic.CheckedLines[blockLine.LineId] == checkCounter)
                        continue;

                    WorldStatic.CheckedLines[blockLine.LineId] = checkCounter;
                    if (blockLine.Segment.Intersects(boxMinX, boxMinY, boxMaxX, boxMaxY))
                    {
                        var blockType = LineBlocksEntity(entity, x, y, ref blockLine, tryMove, true, out var blockingSector3D);
                        if (blockType != LineBlock.NoBlock)
                        {
                            entity.BlockingBlockLineIndex = i;
                            entity.BlockingSector3D = blockingSector3D;
                            blockLineIndex = i;
                            tryMove.Success = false;
                            if (!entity.Flags.NoClip() && blockLine.HasSpecial)
                                tryMove.ImpactSpecialLines.Add(blockLine.LineId);
                            if (blockType == LineBlock.BlockStopChecking)
                                goto doneIsPositionValid;
                        }

                        if (blockType == LineBlock.NoBlock && !entity.Flags.NoClip())
                        {
                            if (blockLine.BlockFlags.MidTex3D)
                                tryMove.IntersectMidTexLines.Add(blockLine.LineId);

                            if (blockLine.HasSpecial)
                                tryMove.IntersectSpecialLines.Add(blockLine.LineId);
                        }

                        tryMove.IntersectSectors.Data[intersectSectorLength++] = blockLine.FrontSector;
                        if (blockLine.BackSector != null && blockLine.BackSector != blockLine.FrontSector)
                            tryMove.IntersectSectors.Data[intersectSectorLength++] = blockLine.BackSector!;
                    }                    
                }
            }
        }


    doneIsPositionValid:
        tryMove.IntersectSectors.Length = intersectSectorLength;

        if (blockLineIndex != -1)
        {
            ref var blockLine = ref m_blockmap.BlockLines[blockLineIndex];
            if (Line.BlocksEntity(entity, entity.Position.X, entity.Position.Y, blockLine.Segment,
                blockLine.OneSided, blockLine.BlockFlags, WorldStatic.Mbf21))
            {
                tryMove.Subsector = null;
                tryMove.Success = false;
                return false;
            }
        }

        if (tryMove.LowestCeilingZ - tryMove.HighestFloorZ < entity.Height || entity.BlockingEntity != null)
        {
            tryMove.Subsector = null;
            tryMove.Success = false;
            return false;
        }

        tryMove.CanFloat = true;

        if (tryMove.LowestCeilingZ - entity.Position.Z < entity.Height)
        {
            tryMove.Subsector = null;
            tryMove.Success = false;
            return false;
        }

        return tryMove.Success;
    }

    private static bool BlocksEntityZ(Entity entity, Entity other, TryMoveData tryMove, bool overlapsZ, LineOpening lineOpening)
    {
        if (WorldStatic.InfinitelyTallThings && !entity.Flags.Missile() && !other.Flags.Missile() && other.MidTexLine == null && other.Sector3D == null)
            return true;

        SetEntityLineOpening(entity, other, tryMove, lineOpening);

        var isPlayer = entity.IsPlayer;
        // If blocking and not a player, do not check step passing below. Non-players can't step onto other things. (Exclude MidTex lines)
        if (overlapsZ && !isPlayer && other.MidTexLine == null && other.Sector3D == null)
            return true;

        return !lineOpening.CanPassOrStepThrough(entity);
    }

    private static void SetEntityLineOpening(Entity entity, Entity other, TryMoveData tryMove, LineOpening opening, bool setDropOff = true)
    {
        if (entity.Position.Z + entity.Height > other.Position.Z)
        {
            // This entity is higher than the other entity and requires step up checking
            var otherHeight = WorldStatic.MissileClip ? other.GetMissileClipHeight(WorldStatic.MissileClip) : other.Height;
            opening.SetTop(tryMove, other.Position.Z + otherHeight);
        }
        else
        {
            // This entity is within the other entity's Z or below
            opening.SetBottom(tryMove, other.Position.Z);
        }

        tryMove.SetIntersectionData3D(opening, entity, setDropOff);
    }

    public void MoveTo(Entity entity, double x, double y, TryMoveData tryMove, Action<Entity, TryMoveData>? onMoveTo = null)
    {
        entity.UnlinkFromWorld(unlinkBlockmapBlocks: false);

        double prevX = entity.Position.X;
        double prevY = entity.Position.Y;
        entity.Position.X = x;
        entity.Position.Y = y;
        onMoveTo?.Invoke(entity, tryMove);            

        LinkToWorld(entity, tryMove, checkLastBlock: true);

        if (entity.Flags.Teleport() || entity.Flags.NoClip())
            return;

        for (int i = tryMove.IntersectSpecialLines.Length - 1; i >= 0 && i < tryMove.IntersectSpecialLines.Length; i--)
        {
            if (entity.Flags.Teleported())
                break;

            var lineId = tryMove.IntersectSpecialLines[i];
            ref var lineSeg = ref m_world.StructLines.Data[lineId].Segment;
            bool fromFront = lineSeg.PerpDot(prevX, prevY) <= 0;
            if (fromFront != (lineSeg.PerpDot(entity.Position.X, entity.Position.Y) <= 0))
                m_world.ActivateSpecialLine(entity, m_world.Lines[lineId], ActivationContext.CrossLine, prevX, prevY);
        }
    }

    private void HandleSlide(Entity entity, ref Vec2D stepDelta, ref int movesLeft, TryMoveData tryMove)
    {
        if (FindClosestBlockingLine(entity, stepDelta, tryMove, out MoveInfo moveInfo) &&
            MoveCloseToBlockingLine(entity, stepDelta, moveInfo, out Vec2D residualStep, tryMove) &&
            !entity.Flags.Teleported())
        {
            ReorientToSlideAlong(entity, m_world.Blockmap.BlockLines[moveInfo.BlockLineIndex].Segment, residualStep, ref stepDelta, ref movesLeft);
            return;
        }

        if (AttemptAxisMove(entity, stepDelta, Axis2D.Y, tryMove))
            return;
        if (AttemptAxisMove(entity, stepDelta, Axis2D.X, tryMove))
            return;

        // If we cannot find the line or thing that is blocking us, then we
        // are fully done moving horizontally.
        if (ShouldClearSlide(entity, tryMove))
        {
            entity.Velocity.X = 0;
            entity.Velocity.Y = 0;
        }

        stepDelta.X = 0;
        stepDelta.Y = 0;
        movesLeft = 0;
    }

    private void CheckCornerTracerIntersection(in Seg2D cornerTracer, Entity entity, TryMoveData tryMove, ref MoveInfo moveInfo)
    {
        bool hit = false;
        double hitTime = double.MaxValue;
        int blockLineIndex = -1;
        
        var it = new BlockmapSegIterator(m_blockmap, cornerTracer);
        while (true)
        {
            var index = it.NextIndex();
            if (index == -1)
                break;

            ref var block = ref m_blockmap.Lines[index];
            int count = block.BlockLineIndex + block.BlockLineCount;
            for (int i = block.BlockLineIndex; i < count; i++)
            {
                ref var line = ref m_blockmap.BlockLines[i];             
                if (cornerTracer.IntersectionExclusive(line.Segment, out double time) && time > 0 && time < 1 &&
                    LineBlocksEntity(entity, entity.Position.X, entity.Position.Y, ref line, tryMove, false, out _) != LineBlock.NoBlock &&
                    time < hitTime)
                {
                    hit = true;
                    hitTime = time;
                    blockLineIndex = i;
                }
            }
        }

        if (hit && hitTime < moveInfo.LineIntersectionTime)
            moveInfo = MoveInfo.From(blockLineIndex, hitTime);
    }

    private bool FindClosestBlockingLine(Entity entity, Vec2D stepDelta, TryMoveData tryMove, out MoveInfo moveInfo)
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

        var first = new Seg2D(corners[0], new Vec2D(corners[0].X + stepDelta.X, corners[0].Y + stepDelta.Y));
        var second = new Seg2D(corners[1], new Vec2D(corners[1].X + stepDelta.X, corners[1].Y + stepDelta.Y));
        var third = new Seg2D(corners[2], new Vec2D(corners[2].X + stepDelta.X, corners[2].Y + stepDelta.Y));

        CheckCornerTracerIntersection(first, entity, tryMove, ref moveInfo);
        CheckCornerTracerIntersection(second, entity, tryMove, ref moveInfo);
        CheckCornerTracerIntersection(third, entity, tryMove, ref moveInfo);

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

    private static void ReorientToSlideAlong(Entity entity, in Seg2D blockingLineSeg, Vec2D residualStep, ref Vec2D stepDelta,
        ref int movesLeft)
    {
        // Our slide direction depends on if we're going along with the
        // line or against the line. If the dot product is negative, it
        // means we are facing away from the line and should slide in
        // the opposite direction from the way the line is pointing.
        Vec2D unitDirection = blockingLineSeg.Delta.Unit();
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
                if (ShouldClearSlide(entity, tryMove))
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
                if (ShouldClearSlide(entity, tryMove))
                    entity.Velocity.X = 0;
                stepDelta.X = 0;
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldClearSlide(Entity entity, TryMoveData tryMove)
    {
        if (!tryMove.BlockedLineClearsVelocity)
            return false;

        if (!WorldStatic.VanillaMovementPhysics)
            return true;

        return tryMove.BlockingEntity == null;
    }

    private void MoveXY(Entity entity)
    {
        if (entity.IsDisposed)
            return;

        // Doom checked skull fly here. This is required to match dehacked functionality if the velocity is cleared but didn't actually hit anything.
        if (entity.Velocity.X == 0 && entity.Velocity.Y == 0)
        {
            if (entity.Flags.Skullfly())
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

        bool shouldClear = false;
        if (entity.Velocity.X > -MinMovement && entity.Velocity.X < MinMovement &&
            entity.Velocity.Y > -MinMovement && entity.Velocity.Y < MinMovement)
        {
            if (entity.PlayerObj == null)
            {
                shouldClear = true;
            }
            else
            {
                var player = entity.PlayerObj;
                if (entity.PlayerObj.IsVooDooDoll)
                    player = m_world.EntityManager.GetRealPlayer(entity.PlayerObj.PlayerNumber);

                shouldClear = player != null && player.TickCommand.SideMoveSpeed == 0 && player.TickCommand.ForwardMoveSpeed == 0;
            }
        }

        if (entity.Flags.MbfBouncer() && ShouldIgnoreMbfBouncerFriction(entity, TryMoveData))
            return;

        if (shouldClear)
        {
            entity.Velocity.X = 0;
            entity.Velocity.Y = 0;
        }
        else if (entity.ShouldApplyFriction())
        {
            double sectorFriction = GetFrictionFromSectors(entity);
            entity.Velocity.X *= sectorFriction;
            entity.Velocity.Y *= sectorFriction;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldIgnoreMbfBouncerFriction(Entity entity, TryMoveData tryMove)
    {
        const double MinVelocity = 0.25;
        return entity.Position.Z > tryMove.DropOffZ &&
            entity.HighestFloorZ != entity.Sector.Floor.Z &&
            (Math.Abs(entity.Velocity.X) > MinVelocity || Math.Abs(entity.Velocity.Y) > MinVelocity);
    }

    private static double GetFrictionFromSectors(Entity entity)
    {
        if (entity.Flags.NoClip() || !WorldStatic.SectorFriction)
            return Constants.DefaultFriction;

        double lowestFriction = double.MaxValue;
        for (int i = entity.IntersectSectors.Length - 1; i >= 0; i--)
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

    private static bool IgnoreOnEntityMoveZ(Entity entity)
    {
        var onEntity = entity.OnEntity();
        if (!WorldStatic.Sector3D)
            return onEntity == null;

        return onEntity == null || onEntity.Sector3D == null || !onEntity.Sector3D.ControlSector.IsMoving;
    }

    public void MoveZ(Entity entity)
    {
        if (entity.IsDisposed || m_world.WorldState == WorldState.Exit)
            return;

        var noVelocity = entity.Velocity.Z == 0;
        var shouldApplyGravity = entity.ShouldApplyGravity();
        if (noVelocity && !shouldApplyGravity && (entity.Flags.Flags1 & EntityFlags.FloatFlag) == 0 && IgnoreOnEntityMoveZ(entity))
            return;

        var floatZ = entity.GetEnemyFloatMove();
        var previousVelocity = entity.Velocity;
        entity.Position.Z = entity.Position.Z + entity.Velocity.Z + floatZ;

        // Passing MoveLinked emulates some vanilla functionality where things are not checked against linked sectors when they haven't moved
        ClampBetweenFloorAndCeiling(entity, null, smoothZ: true, entity.MoveLinked);

        if (entity.IsBlocked())
            m_world.HandleEntityHit(entity, previousVelocity, null);

        if ((entity.Flags.NoGravity() && entity.ShouldApplyFriction()) || (!entity.Flags.Missile() && entity.WaterSubmersionLevel != SubmersionLevel.None))
            entity.Velocity.Z *= Constants.DefaultFriction;

        if (!shouldApplyGravity)
            return;

        if (entity.Flags.MbfBouncer() && entity.Velocity.Z != 0)
        {
            if (!entity.Flags.NoGravity())
                entity.Velocity.Z -= entity.GetMbfBouncerGravity(1);
            return;
        }

        if (entity.WaterSubmersionLevel == SubmersionLevel.None || !entity.HasMovementXY)
        {
            double applyGravity;
            if (entity.Gravity < 0)
                applyGravity = entity.Gravity * -1;
            else
                applyGravity = m_world.Gravity * entity.Properties.Gravity * entity.Sector.Gravity * entity.Gravity;

            // Doom applied the gravity amount twice if the entity originally had no velocity.
            if (noVelocity)
                entity.Velocity.Z -= applyGravity;
            entity.Velocity.Z -= applyGravity;
        }

        if (WorldStatic.Sector3D)
        {
            if (entity.WaterSubmersionLevel > SubmersionLevel.LessThanHalf)
            {
                previousVelocity.Z *= Constants.DefaultFriction;
                if (entity.Velocity.Z < -Entity.WaterSinkSpeed)
                    entity.Velocity.Z = previousVelocity.Z < -Entity.WaterSinkSpeed ? previousVelocity.Z : -Entity.WaterSinkSpeed;
                else
                    entity.Velocity.Z = previousVelocity.Z + ((entity.Velocity.Z - previousVelocity.Z) * 0.125);
            }

            entity.SetWaterSubmersionLevel();
        }
    }
}