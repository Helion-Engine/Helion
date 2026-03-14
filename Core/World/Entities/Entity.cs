using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Graphics.Palettes;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Models;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Sound;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities;


/// <summary>
/// An actor in a world.
/// </summary>
public partial class Entity : IDisposable, ITickable, ISoundSource, IFloorCeilingAnchor
{
    public const double WaterSinkSpeed = 0.5;
    public const double WaterSinkFactor = 0.125;
    private const double Speed = 47000 / 65536.0;
    protected const int ForceGibDamage = ushort.MaxValue;
    protected const int KillDamage = ushort.MaxValue - 1;
    private const int DefaultClosetChaseSpeed = 40;
    public const double FloatSpeed = 4.0;

    public int Index;
    public IWorld World;
    public Entity? Next;
    public Entity? Previous;
    public Line? MidTexLine;
    public Sector3D? Sector3D;

    public Entity? RenderBlockNext;
    public Entity? RenderBlockPrevious;
    public int RenderBlock = -1;
    public BlockRange BlockRange;

    public int BlockmapCount;
    public EntityFlags Flags;
    public int SubsectorId;
    public FrameState FrameState;
    public double AngleRadians;
    public Vec3D Position;
    public Vec3D Velocity;
    public IAudioSource? AudioSource;
    public SubmersionLevel WaterSubmersionLevel;
    public Sector? WaterControlSector;

    public int Health;
    public int MoveCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? Target() => m_target != null && m_target.Id == m_targetId ? m_target : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? Tracer() => m_tracer != null && m_tracer.Id == m_tracerId ? m_tracer : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? Owner() => m_owner != null && m_owner.Id == m_ownerId ? m_owner : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? OnEntity() => m_onEntity != null && m_onEntity.Id == m_onEntityId ? m_onEntity : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? OverEntity() => m_overEntity != null && m_overEntity.Id == m_overEntityId ? m_overEntity : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? LowestCeilingEntity() => LowestCeilingObject is Entity entity && entity.Id == m_lowCeilEntityId ? entity : null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity? HighestFloorEntity() => HighestFloorObject is Entity entity && entity.Id == m_highFloorEntityId ? entity : null;

    public EntityDefinition Definition;
    public EntityProperties Properties;

    public Vec3D PrevPosition;
    public Vec3D SpawnPoint;

    public Vec3D CenterPoint => new(Position.X, Position.Y, Position.Z + (Height / 2));
    public Vec3D ProjectileAttackPos => new(Position.X, Position.Y, Position.Z + 32);
    public Vec3D HitscanAttackPos => new(Position.X, Position.Y, Position.Z + (Height / 2) + 8);
    public Vec3D CalcLastVelocity() => Position - PrevPosition;
    public int FrozenTics;
    public Sector Sector;
    public Sector HighestFloorSector;
    public Sector LowestCeilingSector;
    public Sector? LightCeilingSector3D;
    // Can be Sector or Entity
    public IFloorCeilingAnchor HighestFloorObject;
    public IFloorCeilingAnchor LowestCeilingObject;
    public double LowestCeilingZ;
    public double HighestFloorZ;
    public DynamicArray<Sector> IntersectSectors = new();
    public int Id;
    public int ThingId;
    // Index in Blockmap.BlockLines
    public int BlockingBlockLineIndex;
    public Entity? BlockingEntity;
    public SectorPlane? BlockingSectorPlane;

    // Values that are modified from EntityProperties
    public int Threshold;
    public int ReactionTime;

    public bool OnGround;
    public bool MoveLinked;
    public bool Respawn;
    public bool HadOnEntity;
    public bool StealthVisible;
    public bool HasMovementZ;
    public bool HasMovementXY;
    public float Alpha;
    public RenderStyle RenderStyle;

    public ZDoomLineSpecialType Special;
    public SpecialArgs Args;

    public int LastRenderGametick;
    public double RenderDistanceSquared = double.MaxValue;
    public short SlowTickMultiplier = 1;
    public short ChaseFailureSkipCount;
    public double ClosetChaseSpeed = DefaultClosetChaseSpeed;
    public virtual SoundChannel WeaponSoundChannel => SoundChannel.Default;
    public virtual int ProjectileKickBack => Properties.ProjectileKickBack;

    public bool IsBlocked() => BlockingEntity != null || BlockingBlockLineIndex != -1 || BlockingSectorPlane != null;
    public readonly DynamicArray<LinkableNode<Entity>> SectorNodes = new();
    public readonly DynamicArray<int> IntersectMidTexLines = new(); 
    public bool IsDisposed;
    public bool WaitSoundDispose;

    public ClosetFlags ClosetFlags;

    public double Height;
    public double Radius;
    public double Gravity;
    public int MaxTargetRange;
    public int MinMissileChance;
    public int MeleeThreshold;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFrozen() => FrozenTics > 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDead() => Health <= 0;
    public virtual double ViewZ => 8.0;
    public bool IsDeathStateFinished => IsDead() && FrameState.Frame.Ticks == -1;
    public virtual bool IsInvulnerable => Flags.Invulnerable();
    public virtual Player? PlayerObj => null;
    public virtual bool IsPlayer => false;
    public bool OnSectorFloorZ(Sector sector) => sector.ToFloorZ(Position) == Position.Z;

    // This follows the pattern from WeakEntity.cs. Unrolled the properties here to save the padding from the struct.
    private Entity? m_target;
    private Entity? m_tracer;
    private Entity? m_owner;
    private Entity? m_onEntity;
    private Entity? m_overEntity;

    private int m_targetId;
    private int m_tracerId;
    private int m_ownerId;
    private int m_onEntityId;
    private int m_overEntityId;
    private int m_lowCeilEntityId;
    private int m_highFloorEntityId;

    public Entity()
    {
        World = null!;
        Definition = null!;
        HighestFloorObject = null!;
        HighestFloorSector = null!;
        LowestCeilingObject = null!;
        LowestCeilingSector = null!;
        Sector = Sector.Default;
        SubsectorId = 0;
        Properties = null!;
        BlockRange.StartX = Constants.ClearBlock;
        BlockingBlockLineIndex = -1;
    }

    public void Set(int index, int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, IWorld world, in SpecialArgs args)
    {
        IsDisposed = false;
        Health = definition.Properties.Health;

        World = world;
        Index = index;
        Id = id;
        ThingId = thingId;
        Definition = definition;
        Flags = definition.Flags;
        Properties = definition.Properties;
        ReactionTime = Properties.ReactionTime;

        AngleRadians = angleRadians;
        Height = definition.Properties.Height;
        Radius = definition.Properties.Radius;
        Position = position;
        PrevPosition = Position;
        Sector = sector;
        LowestCeilingZ = sector.Ceiling.Z;
        HighestFloorZ = sector.Floor.Z;
        HighestFloorSector = sector;
        HighestFloorObject = sector;
        LowestCeilingSector = sector;
        LowestCeilingObject = sector;
        LightCeilingSector3D = null;
        CheckOnGround();

        Threshold = 0;
        Gravity = 1;

        Alpha = (float)Properties.Alpha;
        RenderStyle = Properties.RenderStyle;
        MonsterMovementSpeed = Properties.MonsterMovementSpeed;
        MaxTargetRange = Properties.MaxTargetRange;
        MinMissileChance = Properties.MinMissileChance;
        MeleeThreshold = Properties.MeleeThreshold;

        FrameState = new(FrameStateOptions.DestroyOnStop);
        Args = args;
    }

    public void Set(int index, EntityModel entityModel, EntityDefinition definition, IWorld world)
    {
        World = world;
        Index = index;
        IsDisposed = false;
        Id = entityModel.Id;
        ThingId = entityModel.ThingId;
        Definition = definition;
        Flags = new EntityFlags(entityModel.Flags);
        Properties = definition.Properties;
        Threshold = entityModel.Threshold;
        ReactionTime = entityModel.ReactionTime;

        Health = entityModel.Health;

        AngleRadians = entityModel.AngleRadians;

        Position = entityModel.Box.GetCenter();
        Height = entityModel.Box.Height;
        Radius = entityModel.Box.Radius;

        PrevPosition = entityModel.Box.GetCenter();
        Velocity.X = entityModel.VelocityX;
        Velocity.Y = entityModel.VelocityY;
        Velocity.Z = entityModel.VelocityZ;
        Sector = world.Sectors[entityModel.Sector];
                
        MoveLinked = entityModel.MoveLinked;
        Respawn = entityModel.Respawn;

        m_direction = (MoveDir)entityModel.MoveDir;
        Flags.SetInFloat(entityModel.BlockFloat);
        MoveCount = entityModel.MoveCount;
        FrozenTics = entityModel.FrozenTics;
        Gravity = entityModel.Gravity;

        HighestFloorSector = Sector;
        LowestCeilingSector = Sector;
        HighestFloorObject = Sector;
        LowestCeilingObject = Sector;
        LightCeilingSector3D = null;

        MonsterMovementSpeed = Properties.MonsterMovementSpeed;

        FrameState = new(this, entityModel.Frame);

        if (entityModel.OnGround.HasValue)
            OnGround = entityModel.OnGround.Value;

        Alpha = entityModel.Alpha ?? (float)Properties.Alpha;
        RenderStyle = (RenderStyle?)entityModel.RenderStyle ?? Properties.RenderStyle;
        MaxTargetRange = entityModel.MaxTargetRange ?? Properties.MaxTargetRange;
        MinMissileChance = entityModel.MinMissileChance ?? Properties.MinMissileChance;
        MeleeThreshold = entityModel.MeleeThreshold ?? Properties.MeleeThreshold;
        Args = entityModel.Args;

        if (entityModel.IsBlood.HasValue && entityModel.IsBlood.Value)
            Definition.Type = EntityType.Blood;

        if (Flags.Stealth())
            StealthVisible = IsDead();
    }

    public EntityModel ToEntityModel(EntityModel entityModel)
    {
        entityModel.Name = Definition.Name;
        entityModel.Id = Id;
        entityModel.ThingId = ThingId;
        entityModel.AngleRadians = AngleRadians;
        entityModel.SpawnPointX = SpawnPoint.X;
        entityModel.SpawnPointY = SpawnPoint.Y;
        entityModel.SpawnPointZ = SpawnPoint.Z;

        entityModel.Box.CenterX = Position.X;
        entityModel.Box.CenterY = Position.Y;
        entityModel.Box.CenterZ = Position.Z;
        entityModel.Box.Radius = Radius;
        entityModel.Box.Height = Height;

        entityModel.VelocityX = Velocity.X;
        entityModel.VelocityY = Velocity.Y;
        entityModel.VelocityZ = Velocity.Z;
        entityModel.Health = Health;
        entityModel.FrozenTics = FrozenTics;
        entityModel.MoveCount = MoveCount;
        entityModel.Owner = Owner()?.Id;
        entityModel.Target = Target()?.Id;
        entityModel.Tracer = Tracer()?.Id;
        entityModel.MoveLinked = MoveLinked;
        entityModel.Respawn = Respawn;
        entityModel.Sector = Sector.Id;
        entityModel.MoveDir = (int)m_direction;
        entityModel.BlockFloat = Flags.InFloat();

        entityModel.Frame.FrameIndex = FrameState.FrameIndex;
        entityModel.Frame.Tics = FrameState.CurrentTick;
        entityModel.Frame.Destroy = (FrameState.Options & FrameStateOptions.DestroyOnStop) != 0;
        entityModel.Frame.PlayerSprite = (FrameState.Options & FrameStateOptions.PlayerSprite) != 0;

        entityModel.Flags.Flags1 = Flags.Flags1;
        entityModel.Flags.Flags2 = Flags.Flags2;
        entityModel.Flags.Flags3 = Flags.Flags3;

        entityModel.Threshold = Threshold;
        entityModel.ReactionTime = ReactionTime;

        entityModel.HighSec = HighestFloorSector.Id;
        entityModel.LowSec = LowestCeilingSector.Id;

        entityModel.HighEntity = GetBoundingEntityForModel(HighestFloorObject, m_highFloorEntityId);
        entityModel.LowEntity = GetBoundingEntityForModel(LowestCeilingObject, m_lowCeilEntityId);
        entityModel.OnGround = OnGround;
        entityModel.Gravity = Gravity;
        entityModel.Alpha = Alpha;
        entityModel.RenderStyle = (int)RenderStyle;
        entityModel.MaxTargetRange = MaxTargetRange;
        entityModel.MinMissileChance = MinMissileChance;
        entityModel.MeleeThreshold = MeleeThreshold;
        entityModel.IsBlood = Definition.Type == EntityType.Blood;
        entityModel.Args = Args;
        return entityModel;
    }

    private static int? GetBoundingEntityForModel(IFloorCeilingAnchor obj, int validateEntityId)
    {
        if (obj is not Entity entity || entity.Id != validateEntityId)
            return null;

        if (entity.MidTexLine != null)
            return entity.MidTexLine.Id | EntityModel.MidTexEntityFlag;

        if (entity.Sector3D != null)
            return entity.Sector3D.SectorId | EntityModel.Sector3DEntityFlag;

        if (entity.Id < 0)
            return null;

        return entity.Id;
    }

    public virtual void CopyProperties(Entity entity)
    {
        Flags = entity.Flags;
        Health = entity.Health;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTarget(Entity? entity)
    {
        m_target = entity;
        m_targetId = entity == null ? 0 : entity.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTracer(Entity? entity)
    {
        m_tracer = entity;
        m_tracerId = entity == null ? 0 : entity.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOnEntity(Entity? entity)
    {
        HadOnEntity = HadOnEntity || OnEntity() != null;
        m_onEntity = entity;
        m_onEntityId = entity == null ? 0 : entity.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOverEntity(Entity? entity)
    {
        m_overEntity = entity;
        m_overEntityId = entity == null ? 0 : entity.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetOwner(Entity? entity)
    {
        m_owner = entity;
        m_ownerId = entity == null ? 0 : entity.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLowestCeilingEntity(Entity entity)
    {
        LowestCeilingObject = entity;
        m_lowCeilEntityId = entity.Id;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetHighestFloorEntity(Entity entity)
    {
        HighestFloorObject = entity;
        m_highFloorEntityId = entity.Id;
    }

    public double PitchTo(Entity entity) => Position.Pitch(entity.Position, Position.XY.Distance(entity.Position.XY));
    public double PitchTo(Vec3D start, Entity entity) => start.Pitch(entity.Position, Position.XY.Distance(entity.Position.XY));

    public EntityDefinition GetBloodDefinition()
    {
        if (Definition.DefinitionSet)
            return Definition.BloodDefinition;

        Definition.DefinitionSet = true;

        if (!string.IsNullOrEmpty(Definition.Properties.BloodType))
        {
            Definition.BloodDefinition = WorldStatic.EntityManager.DefinitionComposer.GetByNameOrDefault(Definition.Properties.BloodType);
            return Definition.BloodDefinition;
        }

        Definition.BloodDefinition = WorldStatic.EntityManager.DefinitionComposer.GetByNameOrDefault("BLOOD");
        return Definition.BloodDefinition;
    }

    /// <summary>
    /// Resets any interpolation tracking variables.
    /// </summary>
    /// <remarks>
    /// Intended to be used when we have some kind of movement which we do
    /// not want any interpolation with the previous spot being done in the
    /// renderer. An example of this would be going through a teleporter.
    /// </remarks>
    public virtual void ResetInterpolation()
    {
        PrevPosition = Position;
    }

    /// <summary>
    /// Unlinks this entity from the world it is in, but not the entity
    /// list it belongs to.
    /// </summary>
    /// <remarks>
    /// When moving from position to position, we want to unlink from
    /// everything except the entity list (which should be unlinked from
    /// when the entity is fully removed from the world).
    /// </remarks>
    public void UnlinkFromWorld(bool unlinkBlockmapBlocks = true)
    {
        for (int i = SectorNodes.Length - 1; i >= 0; i--)
        {
            LinkableNode<Entity> node = SectorNodes[i];
            node.Unlink();
            WorldStatic.DataCache.FreeLinkableNodeEntity(node);
            SectorNodes.Data[i] = null!;
        }
        SectorNodes.Clear();

        if (unlinkBlockmapBlocks)
            UnlinkBlockMapBlocks();

        if (RenderBlock != -1)
        {
            World.RenderBlockmap.RemoveRenderLink(this);
            RenderBlock = -1;
        }

        IntersectSectors.Clear();
        IntersectMidTexLines.Clear();
        BlockingBlockLineIndex = -1;
        BlockingEntity = null;
        BlockingSectorPlane = null;
    }

    public void UnlinkBlockMapBlocks()
    {
        if (BlockRange.StartX == Constants.ClearBlock)
            return;

        for (var by = BlockRange.StartY; by <= BlockRange.EndY; by++)
        {
            for (var bx = BlockRange.StartX; bx <= BlockRange.EndX; bx++)
            {
                var blockIndex = by * World.Blockmap.Width + bx;
                ref var block = ref World.Blockmap.Entities[blockIndex];
                var data = block.EntityIndices;
                for (int index = block.EntityIndicesLength - 1; index >= 0; index--)
                {
                    if (data[index] == Index)
                    {
                        block.EntityIndicesLength--;
                        if (index < block.EntityIndicesLength)
                            Array.Copy(data, index + 1, data, index, block.EntityIndicesLength - index);
                        break;
                    }
                }
            }
        }

        BlockRange.StartX = Constants.ClearBlock;
    }

    public virtual void Tick()
    {
        Flags.ClearTeleported();

        if (FrozenTics > 0)
            FrozenTics--;

        if (Flags.BossSpawnShot() && ReactionTime > 0)
            ReactionTime--;

        FrameState.Tick(this);

        if (IsDisposed)
            return;

        if (Flags.Stealth())
        {
            if (StealthVisible || Flags.Attacking())
            {
                Alpha += 2 / (float)Constants.TicksPerSecond;
                if (Alpha >= 1)
                {
                    Alpha = 1;
                    StealthVisible = false;
                }
            }
            else if (!IsDead())
            {
                Alpha -= 1.5f / (float)Constants.TicksPerSecond;
                if (Alpha < 0)
                    Alpha = 0;
            }
        }

        if (Flags.CountKill() && IsDeathStateFinished)
        {
            int checkCount = Properties.RespawnTicks ?? WorldStatic.RespawnTicks;
            if (checkCount == 0 || Flags.NoRespawn())
                return;

            MoveCount++;

            if (MoveCount < checkCount)
                return;

            if ((WorldStatic.World.LevelTime & 31) != 0)
                return;

            if (WorldStatic.Random.NextByte() > Properties.RespawnDice)
                return;

            Respawn = true;
        }

        if (Flags.InScrollSector())
        {
            var accumulator = WorldStatic.World.GetEntityScrollAccumulator(this);

            if (accumulator.Count.X != 0)
                Velocity.X += accumulator.Speed.X / accumulator.Count.X;

            if (accumulator.Count.Y != 0)
                Velocity.Y += accumulator.Speed.Y / accumulator.Count.Y;

            WorldStatic.World.ClearEntityScrollAccumulator(this);
            Flags.ClearInScrollSector();
        }

        RunDebugSanityChecks();
    }

    public void ForceGib() =>
        Damage(null, ForceGibDamage, false, DamageType.AlwaysApply);

    public void Kill(Entity? source) =>
        Damage(source, Health, false, DamageType.AlwaysApply);

    private void KillInternal(Entity? source, DamageType damageType)
    {
        if (Health > 0)
            Health = 0;

        bool gib = Health < -Properties.Health;
        Height = Definition.Properties.Height / 4.0;
        ClosetFlags = ClosetFlags.None;
        Flags.ClearAttacking();
        StealthVisible = true;

        if (WorldStatic.MirrorCorpse && IsDead() && Flags.IsMonster() && !Flags.DontMirrorCorpse() && (World.SecondaryRandom.NextByte() & 1) != 0)
            Flags.FlipMirror();

        if (gib && Definition.XDeathState != null)
            SetXDeathState(source, damageType);
        else
            SetDeathState(source, damageType);
    }

    public void SetSpawnState()
    {
        if (Definition.SpawnState != null)
            FrameState.SetFrameIndex(this, Definition.SpawnState.Value);
    }

    public void SetSeeState()
    {
        if (Definition.SeeState != null)
            FrameState.SetFrameIndex(this, Definition.SeeState.Value);
    }

    public void SetMissileState()
    {
        if (Definition.MissileState != null)
            FrameState.SetFrameIndex(this, Definition.MissileState.Value);
    }

    public void SetMeleeState()
    {
        if (Definition.MeleeState != null)
            FrameState.SetFrameIndex(this, Definition.MeleeState.Value);
    }

    public void SetDeathState(Entity? source, DamageType damageType)
    {
        // Doom didn't check null death states
        var deathState = Definition.DeathState ?? 0;       
        if (!IsDisposed)
            SetDeath(source, damageType, false);
                
        FrameState.SetFrameIndex(this, deathState);

        if (!IsDisposed)
            SetDeathRandomizeTicks();
    }

    public void SetXDeathState(Entity? source, DamageType damageType)
    {
        if (!IsDisposed)
            SetDeath(source, damageType, true);

        if (Definition.XDeathState.HasValue)
            FrameState.SetFrameIndex(this, Definition.XDeathState.Value);

        if (!IsDisposed)
            SetDeathRandomizeTicks();
    }

    private void SetDeathRandomizeTicks()
    {
        if (Flags.Missile())
        {
            // Doom will always apply randomization, force this functionality if a dehacked patch is applied
            if (Flags.Randomize() || WorldStatic.Dehacked)
                SetRandomizeTicks();
            if (FrameState.CurrentTick < 1)
                FrameState.SetTics(1);
            return;
        }

        SetRandomizeTicks();
        if (FrameState.CurrentTick < 1)
            FrameState.SetTics(1);
    }

    public bool SetCrushState()
    {
        // Check if there is a Crush state, otherwise default to GenericCrush
        if (FrameState.SetState(this, Definition, Constants.FrameStates.Crush, warn: false) ||
            FrameState.SetState(this, Definition, Constants.FrameStates.GenericCrush, warn: false))
        {
            Flags.SetDontGib();
            Flags.ClearSolid();
            Height = 0.0;
            return true;
        }

        return false;
    }

    public virtual void SetRaiseState(bool restoreFlags = true)
    {        
        if (Definition.RaiseState != null)
        {
            FrameState.SetFrameIndex(this, Definition.RaiseState.Value);
            Health = Definition.Properties.Health;
            Height = Definition.Properties.Height;
            Flags.ClearCrushGiblets();
            if (restoreFlags)
                Flags = Definition.Flags;
        }
    }

    public void SetHealState() =>
        FrameState.SetState(this, Definition, Constants.FrameStates.Heal);

    public void PlaySeeSound(SoundContext ctx = default)
    {
        if (Definition.Properties.SeeSound.Length == 0)
            return;

        Attenuation attenuation = (Flags.FullVolSee() || Flags.Boss()) ? Attenuation.None : Attenuation.Default;
        WorldStatic.SoundManager.CreateSoundOn(this, Definition.Properties.SeeSound,
            new SoundParams(this, attenuation: attenuation, type: SoundType.See, context: ctx));
    }

    public void PlayDeathSound()
    {
        if (Definition.Properties.DeathSound.Length == 0)
            return;

        Attenuation attenuation = (Flags.FullVolDeath() || Flags.Boss()) ? Attenuation.None : Attenuation.Default;
        WorldStatic.SoundManager.CreateSoundOn(this, Definition.Properties.DeathSound,
            new SoundParams(this, attenuation: attenuation));
    }

    public void PlayAttackSound()
    {
        if (Properties.AttackSound.Length > 0)
            WorldStatic.SoundManager.CreateSoundOn(this, Definition.Properties.AttackSound, new SoundParams(this));
    }

    public void PlayActiveSound()
    {
        if (Properties.ActiveSound.Length > 0)
            WorldStatic.SoundManager.CreateSoundOn(this, Definition.Properties.ActiveSound,
                new SoundParams(this, type: SoundType.Active));
    }

    public string GetSpeciesName()
    {
        if (Definition.MonsterSpeciesDefinition != null)
            return Definition.MonsterSpeciesDefinition.Name;

        // In decorate the lowest class that is a monster is the definition of the species
        EntityDefinition speciesDef = Definition;
        for (int i = 0; i < Definition.ParentClassNames.Count; i++)
        {
            var def = WorldStatic.EntityManager.DefinitionComposer.GetByName(Definition.ParentClassNames[i]);
            if (def == null || !def.Flags.IsMonster())
                continue;

            speciesDef = def;
            break;
        }

        Definition.MonsterSpeciesDefinition = speciesDef;
        return speciesDef.Name;
    }

    public virtual bool CanDamage(Entity source, DamageType damageType)
    {
        Entity damageSource = source.Owner() ?? source;
        if (damageSource.IsPlayer)
            return true;

        if (WorldStatic.World.MapInfo.HasOption(MapOptions.TotalInfighting))
            return true;
        if (WorldStatic.World.MapInfo.HasOption(MapOptions.NoInfighting))
            return false;

        if (damageType == DamageType.AlwaysApply)
            return true;

        if (Properties.ProjectileGroup.HasValue)
            return !ProjectileGroupEquals(Properties.ProjectileGroup, damageSource.Properties.ProjectileGroup);

        if (GetSpeciesName().EqualsIgnoreCase(damageSource.GetSpeciesName()) && !Flags.DoHarmSpecies())
            return false;

        return true;
    }

    public bool CanApplyRadiusExplosionDamage(Entity source) =>
        !Properties.SplashGroup.HasValue || !Properties.SplashGroup.NullableEquals(source.Properties.SplashGroup);

    private static bool ProjectileGroupEquals(int? thisGroup, int? otherGroup)
    {
        if (thisGroup < 0)
            return false;

        return thisGroup.NullableEquals(otherGroup);
    }

    public virtual bool Damage(Entity? source, int damage, bool setPainState, DamageType damageType)
    {
        if (damage <= 0 || Flags.Invulnerable())
            return false;

        Entity? damageSource = source;
        bool canRetaliate = false;
        bool willRetaliate = false;
        if (source != null)
        {
            damageSource = source.Owner() ?? source;
            if (!CanDamage(source, damageType))
                return false;

            canRetaliate = WillRetaliateFrom(damageSource) && Threshold <= 0 && !damageSource.IsDead() && damageSource != this;
            willRetaliate = canRetaliate && damageSource != Target();
            if (willRetaliate && !damageSource.Flags.NoTarget() && !IsFriend(damageSource))
                SetTarget(damageSource);
        }

        if (damage == ForceGibDamage)
        {
            // Smooth Doom 21 has A_JumpIfHealthBelow that relies on instant kill sectors setting health very negative
            Health = -10000;
        }
        else if (damage == KillDamage)
        {
            Health = 0;
        }
        else
        {
            Health -= damage;
        }

        if (Flags.Stealth())
            StealthVisible = true;

        ReactionTime = 0;

        if (Health <= 0)
        {
            KillInternal(source, damageType);
            return true;
        }
        else if (setPainState && !Flags.Skullfly() && Definition.PainState != null)
        {
            Flags.SetJustHit();
            FrameState.SetFrameIndex(this, Definition.PainState.Value);
        }

        // Skullfly is not turned off here as the original game did not do this
        if (Flags.Skullfly())
            Velocity = Vec3D.Zero;

        if (damageSource != null && canRetaliate && !Flags.QuickToRetaliate())
            Threshold = Properties.DefThreshold;
        if (damageSource != null && willRetaliate)
        {
            if (Definition.SeeState != null && Definition.SpawnState != null && FrameState.FrameIndex == Definition.SpawnState.Value)
                SetSeeState();
        }

        return true;
    }

    public void SetRandomizeTicks(int opAnd = 3) =>
        FrameState.SetTics(FrameState.CurrentTick - (WorldStatic.Random.NextByte() & opAnd));

    protected static bool IsWeapon(EntityDefinition definition) => definition.IsType(Inventory.WeaponClassName);
    protected static bool IsAmmo(EntityDefinition definition) => definition.IsType(Inventory.AmmoClassName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsCrushing() => LowestCeilingZ - HighestFloorZ < Height;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CheckOnGround() => OnGround = HighestFloorZ >= Position.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsFriend(Entity entity) => Flags.Friendly() && entity.Flags.Friendly();

    public bool CanBlockEntity(Entity other)
    {
        if (this == other || Owner() == other || other.Flags.NoClip())
            return false;

        if (Flags.Ripper())
            return false;

        if (Flags.Missile())
        {
            if (!other.Flags.Shootable() && !other.Flags.Solid())
                return false;

            return true;
        }

        return other.Flags.Solid();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetMaxStepHeight()
    {
        if (Flags.Missile())
            return Flags.StepMissile() ? Properties.MaxStepHeight : 0.0;

        return Properties.MaxStepHeight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldApplyGravity()
    {
        if (Flags.NoGravity())
            return false;

        if (WaterSubmersionLevel >= SubmersionLevel.MoreThanHalf && HasMovementZ)
            return false;

        return !OnGround;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldApplyFriction()
    {
        if (Flags.NoFriction() || Flags.Missile() || Flags.Skullfly())
            return false;

        // Need to apply friction for player fly
        return OnGround || Flags.Fly() || WaterSubmersionLevel > SubmersionLevel.None;
    }

    /// <summary>
    /// Validates ClippedWithEntity. Iterates through the intersecting entities in the sector.
    /// </summary>
    public bool IsClippedWithEntity()
    {
        if (!Flags.Solid())
            return false;

        DynamicArray<Entity> entities = WorldStatic.DataCache.GetEntityList();
        WorldStatic.World.BlockmapTraverser.GetSolidEntityIntersections2D(this, entities);
        for (int i = entities.Length - 1; i >= 0; i--)
        {
            if (entities[i].OverlapsZ(this))
            {
                WorldStatic.DataCache.FreeEntityList(entities);
                return true;
            }
        }

        WorldStatic.DataCache.FreeEntityList(entities);
        return false;
    }

    const int DropOffFlags = EntityFlags.FloatFlag | EntityFlags.DropOffFlag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ShouldCheckDropOff()
    {
        if ((Flags.Flags1 & DropOffFlags) != 0)
            return false;

        if (!WorldStatic.AllowItemDropoff)
            return true;

        if (IsBoomSentient && Flags.MonsterMove())
            return true;

        return !Flags.IgnoreDropOff();
    }

    public bool IsBoomSentient => Definition.Properties.Health > 0 && Definition.SeeState.HasValue;

    public bool CheckDropOff(TryMoveData tryMove)
    {
        if (!ShouldCheckDropOff())
            return true;

        var maxStepHeight = GetMaxStepHeight();
        var dropOffZ = tryMove.DropOffZ;
        // Walking on things test
        Entity? highestWalk = null;
        for (int i = tryMove.IntersectEntities2D.Length - 1; i >= 0; i--)
            highestWalk = GetHighestWalkEntity(tryMove, highestWalk, tryMove.IntersectEntities2D.Data[i], maxStepHeight, ref dropOffZ);

        for (int i = tryMove.IntersectMidTexLines.Length - 1; i >= 0; i--)
            highestWalk = GetHighestWalkEntity(tryMove, highestWalk, World.Lines[tryMove.IntersectMidTexLines.Data[i]].GetMidTexEntity(World), maxStepHeight, ref dropOffZ);

        if (WorldStatic.Sector3D)
        {
            WorldStatic.CheckCounter++;
            for (int i = tryMove.IntersectSectors.Length - 1; i >= 0; i--)
                highestWalk = GetHighestWalkEntitySector3D(tryMove, maxStepHeight, highestWalk, tryMove.IntersectSectors.Data[i], ref dropOffZ);

            if (tryMove.Subsector != null)
                highestWalk = GetHighestWalkEntitySector3D(tryMove, maxStepHeight, highestWalk, tryMove.Subsector.Sector, ref dropOffZ);

            if (tryMove.HasDropOff3D)
                dropOffZ = tryMove.DropOffZ_3D;
        }

        tryMove.DropOffZ = dropOffZ;
        if (highestWalk != null && !highestWalk.Flags.ActLikeBridge() &&
            highestWalk.Position.Z + highestWalk.Height > tryMove.DropOffZ &&
            highestWalk.Position.Z + highestWalk.Height <= Position.Z)
            return false;

        // Entities can walk over the edge of another entity unlike normal sector lines.
        // Check drop off from the entity they are on.
        var onEntity = highestWalk ?? OnEntity();
        if (onEntity != null && onEntity.Flags.ActLikeBridge())
            return onEntity.Position.Z + onEntity.Height - dropOffZ <= maxStepHeight;

        return tryMove.HighestFloorZ - dropOffZ <= maxStepHeight;
    }

    private Entity? GetHighestWalkEntitySector3D(TryMoveData tryMove, double maxStepHeight, Entity? highestWalk, Sector sector, ref double dropOffZ)
    {
        if (sector.CheckCount == WorldStatic.CheckCounter)
            return highestWalk;

        sector.CheckCount = WorldStatic.CheckCounter;
        for (int i = 0; i < sector.Sectors3D.Length; i++)
            highestWalk = GetHighestWalkEntity(tryMove, highestWalk, sector.Sectors3D[i].GetSectorEntity3D(), maxStepHeight, ref dropOffZ);

        return highestWalk;
    }

    private Entity? GetHighestWalkEntity(TryMoveData tryMove, Entity? highestWalk, Entity entity, double maxStepHeight, ref double dropOffZ)
    {
        var topZ = entity.Position.Z + entity.Height;

        if (CanBlockEntity(entity) && topZ >= dropOffZ)
        {
            // Ignore if can't step up
            if (topZ > Position.Z && topZ - Position.Z > maxStepHeight)
                return highestWalk;

            // ActLikeBridge takes precedence when z is equal
            if (topZ == dropOffZ)
            {
                if (highestWalk == null || !highestWalk.Flags.ActLikeBridge())
                    highestWalk = entity;
            }
            else
            {
                highestWalk = entity;
            }

            if (entity.Flags.ActLikeBridge())
                dropOffZ = topZ;
        }

        return highestWalk;
    }

    public virtual void Hit(in Vec3D velocity)
    {
        if (Flags.Skullfly())
        {
            if (BlockingEntity != null)
            {
                int damage = Properties.Damage.Get(WorldStatic.Random);
                WorldStatic.World.DamageEntity(BlockingEntity, this, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            }

            // Bounce off plane if it's the only thing blocking
            if (BlockingSectorPlane != null && BlockingBlockLineIndex == -1 && BlockingEntity == null)
            {
                Velocity = velocity;
                Velocity.Z = -velocity.Z;
            }
            else
            {
                Flags.ClearSkullfly();
                Velocity = Vec3D.Zero;
                SetSpawnState();
            }
        }
        else if (Flags.MbfBouncer())
        {
            if (BlockingSectorPlane != null)
            {
                Velocity.Z = -velocity.Z * GetBounceDecay();
                if (Math.Abs(Velocity.Z) <= GetMbfBouncerGravity(4))
                    Velocity.Z = 0;
            }

            if (!Flags.Missile() && BlockingBlockLineIndex != -1)
            {
                var bounceVelocity = MathHelper.BounceVelocity(velocity.XY, World.Blockmap.BlockLines[BlockingBlockLineIndex].Segment);
                Velocity.X = bounceVelocity.X;
                Velocity.Y = bounceVelocity.Y;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double GetMbfBouncerGravity(int factor)
    {
        return Properties.Mass * (World.Gravity * Properties.Gravity * Sector.Gravity * Gravity * factor / 256);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private double GetBounceDecay()
    {
        if (Flags.NoGravity())
            return 1.0;

        if (Flags.Float())
        {
            if (Flags.Dropoff())
                return 0.85;
            return 0.7;
        }

        return 0.45;
    }

    public bool ShouldDieOnCollision()
    {
        if (Flags.MbfBouncer() && Flags.Missile())
            return BlockingEntity != null || BlockingBlockLineIndex != -1;

        return Flags.Missile();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetTranslationColorMap()
    {
        // Player colormaps start index 0 (green)
        if (PlayerObj != null)
            return PlayerObj.PlayerNumber % ((int)TranslateColor.Count + 1);

        return (Flags.Flags3 & EntityFlags.TranslationFlag) >> 11;
    }

    public void SetWaterSubmersionLevel()
    {
        if (!Sector.GetWaterSubmersionHeight(this, out var height, out var sector3d))
        {
            WaterControlSector = null;
            WaterSubmersionLevel = SubmersionLevel.None;
            return;
        }

        WaterControlSector = sector3d.ControlSector;
        var depth = height - Position.Z;
        if (depth <= 0)
        {
            WaterSubmersionLevel = SubmersionLevel.None;
            return;
        }

        if (depth > Height / 2)
        {
            if (depth >= Height || (PlayerObj != null && depth >= PlayerObj.ViewHeight))
                WaterSubmersionLevel = SubmersionLevel.Full;
            else
                WaterSubmersionLevel = SubmersionLevel.MoreThanHalf;
        }
        else
        {
            WaterSubmersionLevel = SubmersionLevel.LessThanHalf;
        }
    }

    public bool IsNormalByContext(double heightZ, SolidContext context)
    {
        if (!WorldStatic.Sector3D)
            return true;

        var sector3d = GetSectorByHeight3D(heightZ);
        if (sector3d == null)
            return true;

        if (context == SolidContext.LineOfSight)
            return (sector3d.Flags & SectorFlags3D.SightInvert) == 0;

        return (sector3d.Flags & SectorFlags3D.ShootInvert) == 0;
    }

    public Sector3D? GetSectorByHeight3D(double heightZ)
    {
        for (int i = 0; i < Sector.Sectors3D.Length; i++)
        {
            var sector3d = Sector.Sectors3D[i];
            if (sector3d.ControlTop.Z < heightZ || sector3d.ControlBottom.Z > heightZ)
                continue;

            return sector3d;
        }

        return null;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        // The sound has a reference back to this entity to update base on position it can't be reused until the sound is complete.
        WaitSoundDispose = AudioSource != null;

        Id = int.MinValue;
        IsDisposed = true;
        UnlinkFromWorld();
        Unlink();

        FrameState.SetFrameIndex(this, Constants.NullFrameIndex);

        SectorNodes.Clear();
        IntersectSectors.Clear();
        IntersectMidTexLines.Clear();

        m_target = null;
        m_targetId = 0;

        m_tracer = null;
        m_tracerId = 0;

        m_owner = null;
        m_ownerId = 0;

        m_onEntity = null;
        m_onEntityId = 0;

        m_overEntity = null;
        m_overEntityId = 0;

        m_lowCeilEntityId = 0;
        m_highFloorEntityId = 0;

        if (!WaitSoundDispose)
            FreeToDataCache();

        Velocity = Vec3D.Zero;

        OnGround = false;
        MoveCount = 0;
        FrozenTics = 0;
        MoveLinked = false;
        Respawn = false;
        HadOnEntity = false;
        ClosetFlags = ClosetFlags.None;
        BlockingBlockLineIndex = -1;
        BlockingEntity = null;
        BlockingSectorPlane = null;
        Sector = Sector.Default;
        SubsectorId = 0;
        HighestFloorObject = Sector.Default;
        LowestCeilingObject = Sector.Default;
        HighestFloorSector = Sector.Default;
        LowestCeilingSector = Sector.Default;
        SlowTickMultiplier = 1;
        ChaseFailureSkipCount = 0;
        ClosetChaseSpeed = DefaultClosetChaseSpeed;
        Special = ZDoomLineSpecialType.None;
        WaterSubmersionLevel = SubmersionLevel.None;
        WaterControlSector = null;
    }

    private void FreeToDataCache()
    {
        if (Index > 0 && World.DataCache.FreeEntity(this))
            Definition = null!;
    }

    private void Unlink()
    {
        if (this == WorldStatic.EntityManager.Head)
        {
            WorldStatic.EntityManager.Head = Next;
            if (WorldStatic.EntityManager.Head != null)
                WorldStatic.EntityManager.Head.Previous = null;
            Next = null;
            Previous = null;
            return;
        }

        if (Next != null)
            Next.Previous = Previous;
        if (Previous != null)
            Previous.Next = Next;

        Next = null;
        Previous = null;
    }

    protected virtual void SetDeath(Entity? source, DamageType damageType, bool gibbed)
    {
        if (Flags.Missile())
        {
            PlayDeathSound();
            Flags.ClearMissile();
            Velocity = Vec3D.Zero;
        }
        else
        {
            Flags.SetCorpse();
            Flags.SetDropoff();
            Flags.ClearSkullfly();
            Flags.ClearShootable();
            if (!Flags.DontFall())
                Flags.ClearNoGravity();
        }

        WorldStatic.World.HandleEntityDeath(this, source, damageType, gibbed);
    }

    [Conditional("DEBUG")]
    private void RunDebugSanityChecks()
    {
        if (Position.Z < PhysicsManager.LowestPossibleZ)
            Fail($"Entity #{Id} ({Definition.Name}) has fallen too far, did you forget +NOGRAVITY with something like +NOSECTOR/+NOBLOCKMAP?");
    }

    public override string ToString()
    {
        return $"Id:{Id} [{Definition}] [{Position}]";
    }

    public double GetDistanceSquaredFrom(Entity listenerEntity)
    {
        return Position.DistanceSquared(listenerEntity.Position);
    }

    public virtual void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel)
    {
        AudioSource = audioSource;
    }

    public virtual bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound)
    {
        AudioSource = null;
        clearedSound = null;

        if (IsDisposed && WaitSoundDispose)
        {
            WaitSoundDispose = false;
            FreeToDataCache();
        }

        return false;
    }

    public virtual void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        AudioSource = null;
    }

    public Vec3D? GetSoundPosition(Entity listenerEntity)
    {
        return Position;
    }

    public Vec3D? GetSoundVelocity()
    {
        return Velocity;
    }

    public virtual bool CanMakeSound() => true;

    public float GetSoundRadius() => (float)Radius + 16;

    private bool WillRetaliateFrom(Entity damageSource)
    {
        if (damageSource.IsPlayer)
            return true;

        if (Properties.InfightingGroup.NullableEquals(damageSource.Properties.InfightingGroup))
            return false;

        return true;
    }
}
