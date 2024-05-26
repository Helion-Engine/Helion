using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Render.OpenGL.Renderers.Legacy.World;
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
using static Helion.Util.Assertion.Assert;
using Helion.World.Blockmap;
using Helion.World.Geometry.Subsectors;

namespace Helion.World.Entities;

/// <summary>
/// An actor in a world.
/// </summary>
public partial class Entity : IDisposable, ITickable, ISoundSource, IRenderObject
{
    private const double Speed = 47000 / 65536.0;
    private const int ForceGibDamage = ushort.MaxValue;
    private const int KillDamage = ushort.MaxValue - 1;
    private const int DefaultClosetChaseSpeed = 64;
    public const double FloatSpeed = 4.0;

    public Entity? Next;
    public Entity? Previous;

    public Entity? RenderBlockNext;
    public Entity? RenderBlockPrevious;
    public Block? RenderBlock;

    public int BlockmapCount;
    public EntityFlags Flags;
    public Subsector Subsector;
    public FrameState FrameState;
    public double AngleRadians;
    public Vec3D Position;
    public Vec3D Velocity;

    public int Health;
    public int MoveCount;

    public WeakEntity Target = WeakEntity.Default;
    public WeakEntity Tracer = WeakEntity.Default;
    public WeakEntity OnEntity = WeakEntity.Default;
    public WeakEntity OverEntity = WeakEntity.Default;
    public WeakEntity Owner = WeakEntity.Default;

    public EntityDefinition Definition;
    public EntityProperties Properties;

    public Vec3D PrevPosition;

    public Vec3D CenterPoint => new(Position.X, Position.Y, Position.Z + (Height / 2));
    public Vec3D ProjectileAttackPos => new(Position.X, Position.Y, Position.Z + 32);
    public Vec3D HitscanAttackPos => new(Position.X, Position.Y, Position.Z + (Height / 2) + 8);
    public int Armor;
    public EntityProperties? ArmorProperties => ArmorDefinition?.Properties;
    public EntityDefinition? ArmorDefinition;
    public int FrozenTics;
    public Sector Sector;
    public Sector HighestFloorSector;
    public Sector LowestCeilingSector;
    // Can be Sector or Entity
    public object HighestFloorObject;
    public object LowestCeilingObject;
    public double LowestCeilingZ;
    public double HighestFloorZ;
    public DynamicArray<Sector> IntersectSectors = new();
    public DynamicArray<Sector> IntersectMovementSectors = new();
    public Vec3D SpawnPoint;
    public int Id;
    public int ThingId;
    public Line? BlockingLine;
    public Entity? BlockingEntity;
    public SectorPlane? BlockingSectorPlane;
    public Player? PickupPlayer;

    // Values that are modified from EntityProperties
    public int Threshold;
    public int ReactionTime;

    public bool OnGround;
    public bool MoveLinked;
    public bool Respawn;
    public float Alpha;

    public int LastRenderGametick;
    public double RenderDistanceSquared { get; set; } = double.MaxValue;
    public int SlowTickMultiplier = 1;
    public int ChaseFailureSkipCount;
    public double ClosetChaseSpeed = DefaultClosetChaseSpeed;
    public RenderObjectType Type => RenderObjectType.Entity;

    public virtual SoundChannel WeaponSoundChannel => SoundChannel.Default;
    public virtual int ProjectileKickBack => Properties.ProjectileKickBack;

    public bool IsBlocked() => BlockingEntity != null || BlockingLine != null || BlockingSectorPlane != null;
    public readonly DynamicArray<LinkableNode<Entity>> BlockmapNodes = new();
    public readonly DynamicArray<LinkableNode<Entity>> SectorNodes = new();
    public bool IsDisposed;

    public ClosetFlags ClosetFlags;

    public double Height;
    public double Radius;
    public bool IsFrozen => FrozenTics > 0;
    public bool IsDead => Health <= 0;
    public EntityFrame Frame => FrameState.Frame;
    public virtual double ViewZ => 8.0;
    public bool IsDeathStateFinished => IsDead && Frame.Ticks == -1;
    public virtual bool IsInvulnerable => Flags.Invulnerable;
    public virtual Player? PlayerObj => null;
    public virtual bool IsPlayer => false;
    public bool OnSectorFloorZ(Sector sector) => sector.ToFloorZ(Position) == Position.Z;

    public Entity()
    {
        Definition = null!;
        HighestFloorObject = null!;
        HighestFloorSector = null!;
        LowestCeilingObject = null!;
        LowestCeilingSector = null!;
        Sector = Sector.Default;
        Subsector = Subsector.Default;
        Properties = null!;
    }

    public void Set(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector)
    {
        IsDisposed = false;
        Health = definition.Properties.Health;

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
        CheckOnGround();

        Properties.Threshold = 0;

        Alpha = (float)Properties.Alpha;

        FrameState = new(this, definition);
    }

    public void Set(EntityModel entityModel, EntityDefinition definition, IWorld world)
    {
        IsDisposed = false;
        Id = entityModel.Id;
        ThingId = entityModel.ThingId;
        Definition = definition;
        Flags = new EntityFlags(entityModel.Flags);
        Properties = definition.Properties;
        Threshold = entityModel.Threshold;
        ReactionTime = entityModel.ReactionTime;

        Health = entityModel.Health;
        Armor = entityModel.Armor;

        AngleRadians = entityModel.AngleRadians;

        Position = entityModel.Box.GetCenter();
        Height = entityModel.Box.Height;
        Radius = entityModel.Box.Radius;

        PrevPosition = entityModel.Box.GetCenter();
        Velocity = entityModel.GetVelocity();
        SpawnPoint = entityModel.GetSpawnPoint();
        Sector = world.Sectors[entityModel.Sector];
                
        MoveLinked = entityModel.MoveLinked;
        Respawn = entityModel.Respawn;

        m_direction = (MoveDir)entityModel.MoveDir;
        BlockFloating = entityModel.BlockFloat;
        MoveCount = entityModel.MoveCount;
        FrozenTics = entityModel.FrozenTics;

        HighestFloorSector = Sector;
        LowestCeilingSector = Sector;
        HighestFloorObject = Sector;
        LowestCeilingObject = Sector;

        if (entityModel.ArmorDefinition != null)
            ArmorDefinition = WorldStatic.EntityManager.DefinitionComposer.GetByName(entityModel.ArmorDefinition);

        Alpha = (float)Properties.Alpha;

        FrameState = new(this, definition, entityModel.Frame);

        if (entityModel.OnGround.HasValue)
            OnGround = entityModel.OnGround.Value;
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
        entityModel.Box = ToEntityBoxModel();
        entityModel.VelocityX = Velocity.X;
        entityModel.VelocityY = Velocity.Y;
        entityModel.VelocityZ = Velocity.Z;
        entityModel.Health = Health;
        entityModel.Armor = Armor;
        entityModel.FrozenTics = FrozenTics;
        entityModel.MoveCount = MoveCount;
        entityModel.Owner = Owner.Entity?.Id;
        entityModel.Target = Target.Entity?.Id;
        entityModel.Tracer = Tracer.Entity?.Id;
        entityModel.MoveLinked = MoveLinked;
        entityModel.Respawn = Respawn;
        entityModel.Sector = Sector.Id;
        entityModel.MoveDir = (int)m_direction;
        entityModel.BlockFloat = BlockFloating;
        entityModel.ArmorDefinition = ArmorDefinition?.Name;
        entityModel.Frame = FrameState.ToFrameStateModel();
        entityModel.Flags = Flags.ToEntityFlagsModel();
        entityModel.Threshold = Threshold;
        entityModel.ReactionTime = ReactionTime;
        entityModel.HighSec = HighestFloorSector.Id;
        entityModel.LowSec = LowestCeilingSector.Id;
        entityModel.HighEntity = GetBoundingEntityForModel(HighestFloorObject);
        entityModel.LowEntity = GetBoundingEntityForModel(LowestCeilingObject);
        entityModel.OnGround = OnGround;
        return entityModel;
    }

    private static int? GetBoundingEntityForModel(object obj)
    {
        if (obj is not Entity entity)
            return null;

        return entity.Id;
    }

    public virtual void CopyProperties(Entity entity)
    {
        Flags = entity.Flags;
        Health = entity.Health;
        Armor = entity.Armor;
        ArmorDefinition = entity.ArmorDefinition;
    }

    public void SetTarget(Entity? entity) =>
        Target = WeakEntity.GetReference(entity);

    public void SetTracer(Entity? entity) =>
        Tracer = WeakEntity.GetReference(entity);

    public void SetOnEntity(Entity? entity) =>
        OnEntity = WeakEntity.GetReference(entity);

    public void SetOverEntity(Entity? entity) =>
        OverEntity = WeakEntity.GetReference(entity);

    public void SetOwner(Entity? entity) =>
        Owner = WeakEntity.GetReference(entity);

    public double PitchTo(Entity entity) => Position.Pitch(entity.Position, Position.XY.Distance(entity.Position.XY));
    public double PitchTo(Vec3D start, Entity entity) => start.Pitch(entity.Position, Position.XY.Distance(entity.Position.XY));

    public EntityDefinition GetBloodDefinition()
    {
        if (Definition.BloodDefinition != null)
            return Definition.BloodDefinition;

        if (!string.IsNullOrEmpty(Definition.Properties.BloodType))
        {
            Definition.BloodDefinition = WorldStatic.EntityManager.DefinitionComposer.GetByName(Definition.Properties.BloodType);
            return Definition.BloodDefinition;
        }

        Definition.BloodDefinition = WorldStatic.EntityManager.DefinitionComposer.GetByName("BLOOD");
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
    public void UnlinkFromWorld()
    {
        // We need max speed for stuff like this because of how frequently
        // it is called. As of 2017, there is a 61% performance loss by
        // doing foreach instead of for. We can do foreach one day when the
        // compiler does this for us. This may need revisiting since the
        // JIT probably is pretty smart now though.
        //
        // Also maybe doing Clear() is overkill, but then again not doing
        // clearing may cause garbage collection to not be run for any
        // lingering elements in the list.
        for (int i = 0; i < SectorNodes.Length; i++)
        {
            LinkableNode<Entity> node = SectorNodes[i];
            node.Unlink();
            WorldStatic.DataCache.FreeLinkableNodeEntity(node);
            SectorNodes.Data[i] = null!;
        }
        SectorNodes.Clear();

        for (int i = 0; i < BlockmapNodes.Length; i++)
        {
            LinkableNode<Entity> node = BlockmapNodes[i];
            node.Unlink();
            WorldStatic.DataCache.FreeLinkableNodeEntity(node);
            BlockmapNodes.Data[i] = null!;
        }
        BlockmapNodes.Clear();

        if (RenderBlock != null)
        {
            RenderBlock.RemoveLink(this);
            RenderBlock = null;
        }

        IntersectSectors.Clear();
        IntersectMovementSectors.Clear();
        BlockingLine = null;
        BlockingEntity = null;
        BlockingSectorPlane = null;
    }

    public virtual void Tick()
    {
        PrevPosition = Position;

        Flags.Teleported = false;

        if (FrozenTics > 0)
            FrozenTics--;

        if (Flags.BossSpawnShot && ReactionTime > 0)
            ReactionTime--;

        FrameState.Tick();

        if (IsDisposed)
            return;

        if (Flags.CountKill && IsDeathStateFinished)
        {
            if (WorldStatic.RespawnTimeSeconds == 0)
                return;

            MoveCount++;

            if (MoveCount < WorldStatic.RespawnTimeSeconds * (int)Constants.TicksPerSecond)
                return;

            if ((WorldStatic.World.LevelTime & 31) != 0)
                return;

            if (WorldStatic.Random.NextByte() > 4)
                return;

            Respawn = true;
        }

        RunDebugSanityChecks();
    }

    public void ForceGib() =>
        Damage(null, ForceGibDamage, false, DamageType.AlwaysApply);

    public void Kill(Entity? source) =>
        Damage(source, Health, false, DamageType.AlwaysApply);

    private void KillInternal(Entity? source)
    {
        if (Health > 0)
            Health = 0;

        bool gib = Health < -Properties.Health;
        Height = Definition.Properties.Height / 4.0;
        ClosetFlags = ClosetFlags.None;

        if (gib && Definition.XDeathState != null)
            SetXDeathState(source);
        else
            SetDeathState(source);
    }

    public void SetSpawnState()
    {
        if (Definition.SpawnState != null)
            FrameState.SetFrameIndex(Definition.SpawnState.Value);
    }

    public void SetSeeState()
    {
        if (Definition.SeeState != null)
            FrameState.SetFrameIndex(Definition.SeeState.Value);
    }

    public void SetMissileState()
    {
        if (Definition.MissileState != null)
            FrameState.SetFrameIndex(Definition.MissileState.Value);
    }

    public void SetMeleeState()
    {
        if (Definition.MeleeState != null)
            FrameState.SetFrameIndex(Definition.MeleeState.Value);
    }

    public void SetDeathState(Entity? source)
    {
        if (Definition.DeathState.HasValue)
        {
            if (Definition.DeathState != null)
                FrameState.SetFrameIndex(Definition.DeathState.Value);
            
            if (!IsDisposed)
            {
                SetDeathRandomizeTicks();
                SetDeath(source, false);  
            }
        }
    }

    public void SetXDeathState(Entity? source)
    {
        if (Definition.XDeathState.HasValue)
        {
            if (Definition.XDeathState != null)
                FrameState.SetFrameIndex(Definition.XDeathState.Value);
            
            if (!IsDisposed)
            {
                SetDeathRandomizeTicks();
                SetDeath(source, true);                
            }
        }
    }

    private void SetDeathRandomizeTicks()
    {
        if (Flags.Missile)
        {
            // Doom will always apply randomization, force this functionality if a dehacked patch is applied
            if (Flags.Randomize || WorldStatic.Dehacked)
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
        if (FrameState.SetState(Constants.FrameStates.Crush, warn: false) ||
            FrameState.SetState(Constants.FrameStates.GenericCrush, warn: false))
        {
            Flags.DontGib = true;
            Flags.Solid = false;
            Height = 0.0;
            return true;
        }

        return false;
    }

    public virtual void SetRaiseState(bool restoreFlags = true)
    {        
        if (Definition.RaiseState != null)
        {
            FrameState.SetFrameIndex(Definition.RaiseState.Value);
            Health = Definition.Properties.Health;
            Height = Definition.Properties.Height;
            if (restoreFlags)
                Flags = Definition.Flags;
        }
    }

    public void SetHealState() =>
        FrameState.SetState(Constants.FrameStates.Heal);

    public void PlaySeeSound()
    {
        if (Definition.Properties.SeeSound.Length == 0)
            return;

        Attenuation attenuation = (Flags.FullVolSee || Flags.Boss) ? Attenuation.None : Attenuation.Default;
        WorldStatic.SoundManager.CreateSoundOn(this, Definition.Properties.SeeSound,
            new SoundParams(this, attenuation: attenuation, type: SoundType.See));
    }

    public void PlayDeathSound()
    {
        if (Definition.Properties.DeathSound.Length == 0)
            return;

        Attenuation attenuation = (Flags.FullVolDeath || Flags.Boss) ? Attenuation.None : Attenuation.Default;
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
            if (def == null || !def.Flags.IsMonster)
                continue;

            speciesDef = def;
            break;
        }

        Definition.MonsterSpeciesDefinition = speciesDef;
        return speciesDef.Name;
    }

    public virtual bool CanDamage(Entity source, DamageType damageType)
    {
        Entity damageSource = source.Owner.Entity ?? source;
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

        if (GetSpeciesName().Equals(damageSource.GetSpeciesName()) && !Flags.DoHarmSpecies)
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
        if (damage <= 0 || Flags.Invulnerable)
            return false;

        Entity? damageSource = source;
        bool willRetaliate = false;
        if (source != null)
        {
            damageSource = source.Owner.Entity ?? source;
            if (!CanDamage(source, damageType))
                return false;

            willRetaliate = WillRetaliateFrom(damageSource) && Threshold <= 0 && !damageSource.IsDead && damageSource != Target.Entity && damageSource != this;
            if (willRetaliate && !damageSource.Flags.NoTarget && !IsFriend(damageSource))
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
            damage = ApplyArmorDamage(damage);
            Health -= damage;
        }

        ReactionTime = 0;

        if (Health <= 0)
        {
            KillInternal(source);
            return true;
        }
        else if (setPainState && !Flags.Skullfly && Definition.PainState != null)
        {
            Flags.JustHit = true;
            FrameState.SetFrameIndex(Definition.PainState.Value);
        }

        // Skullfly is not turned off here as the original game did not do this
        if (Flags.Skullfly)
            Velocity = Vec3D.Zero;

        if (damageSource != null && willRetaliate)
        {
            if (!Flags.QuickToRetaliate)
                Threshold = Properties.DefThreshold;
            if (Definition.SeeState != null && Definition.SpawnState != null && FrameState.FrameIndex == Definition.SpawnState.Value)
                SetSeeState();
        }

        return true;
    }

    public void SetRandomizeTicks(int opAnd = 3) =>
        FrameState.SetTics(FrameState.CurrentTick - (WorldStatic.Random.NextByte() & opAnd));

    private int ApplyArmorDamage(int damage)
    {
        if (ArmorProperties == null || Armor == 0)
            return damage;
        if (ArmorProperties.Armor.SavePercent == 0)
            return damage;

        int armorDamage = (int)(damage * (ArmorProperties.Armor.SavePercent / 100.0));
        if (Armor < armorDamage)
            armorDamage = Armor;

        Armor -= armorDamage;
        damage = MathHelper.Clamp(damage - armorDamage, 0, damage);

        if (Armor <= 0)
            ArmorDefinition = null;

        return damage;
    }

    protected static bool IsWeapon(EntityDefinition definition) => definition.IsType(Inventory.WeaponClassName);
    protected static bool IsAmmo(EntityDefinition definition) => definition.IsType(Inventory.AmmoClassName);

    public bool IsCrushing() => LowestCeilingZ - HighestFloorZ < Height;
    public void CheckOnGround() => OnGround = HighestFloorZ >= Position.Z;
    public bool IsFriend(Entity entity) => Flags.Friendly && entity.Flags.Friendly;

    public bool CanBlockEntity(Entity other)
    {
        if (this == other || Owner.Entity == other || other.Flags.NoClip)
            return false;

        if (Flags.Ripper)
            return false;

        // Ignore solid checks for missiles
        if (Flags.Missile)
            return true;

        return other.Flags.Solid;
    }

    public double GetMaxStepHeight()
    {
        if (Flags.Missile)
            return Flags.StepMissile ? Properties.MaxStepHeight : 0.0;

        return Properties.MaxStepHeight;
    }

    public bool ShouldApplyGravity()
    {
        if (Flags.NoGravity)
            return false;

        return !OnGround;
    }

    public bool ShouldApplyFriction()
    {
        if (Flags.NoFriction || Flags.Missile || Flags.Skullfly)
            return false;

        // Need to apply friction for player fly
        return OnGround || Flags.Fly;
    }

    /// <summary>
    /// Validates ClippedWithEntity. Iterates through the intersecting entities in the sector.
    /// </summary>
    public bool IsClippedWithEntity()
    {
        if (!Flags.Solid)
            return false;

        DynamicArray<Entity> entities = WorldStatic.DataCache.GetEntityList();
        WorldStatic.World.BlockmapTraverser.GetSolidEntityIntersections2D(this, entities);
        for (int i = 0; i < entities.Length; i++)
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

    public bool ShouldCheckDropOff()
    {
        if (Flags.Float || Flags.Dropoff)
            return false;

        if (!WorldStatic.AllowItemDropoff)
            return true;

        if (IsBoomSentient && Flags.MonsterMove)
            return true;

        return !Flags.IgnoreDropOff;
    }

    public bool IsBoomSentient => Definition.Properties.Health > 0 && Definition.SeeState.HasValue;

    public bool CheckDropOff(TryMoveData tryMove)
    {
        if (!ShouldCheckDropOff())
            return true;

        if (tryMove.DropOffEntity != null && !tryMove.DropOffEntity.Flags.ActLikeBridge)
            return false;

        Entity? highestWalk = null;
        // Walking on things test
        for (int i = 0; i < tryMove.IntersectEntities2D.Length; i++)
        {
            Entity entity = tryMove.IntersectEntities2D[i];
            double topZ = entity.Position.Z + entity.Height;

            if (!CanBlockEntity(entity))
                continue;
            if (topZ >= tryMove.DropOffZ)
            {
                // ActLikeBridge takes precedence when z is equal
                if (topZ == tryMove.DropOffZ && (highestWalk == null || !highestWalk.Flags.ActLikeBridge))
                    highestWalk = entity;
                else
                    highestWalk = entity;

                if (entity.Flags.ActLikeBridge)
                    tryMove.DropOffZ = topZ;
            }
        }

        if (highestWalk != null && !highestWalk.Flags.ActLikeBridge &&
            highestWalk.Position.Z + highestWalk.Height > tryMove.DropOffZ &&
            highestWalk.Position.Z + highestWalk.Height <= Position.Z)
            return false;

        if (tryMove.IntersectEntities2D.Length == 0 && tryMove.DropOffEntity != null)
            return false;

        return tryMove.HighestFloorZ - tryMove.DropOffZ <= GetMaxStepHeight();
    }

    public virtual void Hit(in Vec3D velocity)
    {
        if (Flags.Skullfly)
        {
            if (BlockingEntity != null)
            {
                int damage = Properties.Damage.Get(WorldStatic.Random);
                WorldStatic.World.DamageEntity(BlockingEntity, this, damage, DamageType.AlwaysApply, Thrust.Horizontal);
            }

            // Bounce off plane if it's the only thing blocking
            if (BlockingSectorPlane != null && BlockingLine == null && BlockingEntity == null)
            {
                Velocity = velocity;
                Velocity.Z = -velocity.Z;
            }
            else
            {
                Flags.Skullfly = false;
                Velocity = Vec3D.Zero;
                SetSpawnState();
            }
        }
        else if (Flags.MbfBouncer)
        {
            //MbfBouncer + Missile - bounce off plane only
            //MbfBouncer + NoGravity - bounce of all surfaces
            bool bouncePlane = BlockingSectorPlane != null;
            bool bounceWall = Flags.NoGravity;
            double zFactor = Flags.NoGravity ? 1.0 : 0.5;

            if (bouncePlane || bounceWall)
                Velocity = velocity;

            if (bouncePlane)
                Velocity.Z = -velocity.Z * zFactor;

            if (bounceWall && BlockingLine != null)
            {
                var bounceVelocity = MathHelper.BounceVelocity(velocity.XY, BlockingLine);
                Velocity.X = bounceVelocity.X;
                Velocity.Y = bounceVelocity.Y;
            }
        }
    }

    public bool ShouldDieOnCollision()
    {
        if (Flags.MbfBouncer && Flags.Missile)
            return BlockingEntity != null || BlockingLine != null;

        return Flags.Missile;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        UnlinkFromWorld();
        Unlink();

        FrameState.SetFrameIndex(Constants.NullFrameIndex);

        BlockmapNodes.Clear();
        SectorNodes.Clear();
        IntersectSectors.Clear();
        IntersectMovementSectors.Clear();

        Target = WeakEntity.Default;
        Tracer = WeakEntity.Default;
        OnEntity = WeakEntity.Default;
        OverEntity = WeakEntity.Default;
        Owner = WeakEntity.Default;
        PickupPlayer = null;

        WeakEntity.DisposeEntity(this);

        if (WorldStatic.DataCache.FreeEntity(this))
            Definition = null!;

        Velocity = Vec3D.Zero;

        OnGround = false;
        MoveCount = 0;
        FrozenTics = 0;
        MoveLinked = false;
        Respawn = false;
        ClosetFlags = ClosetFlags.None;
        BlockingLine = null;
        BlockingEntity = null;
        BlockingSectorPlane = null;
        Sector = Sector.Default;
        Subsector = Subsector.Default;
        HighestFloorObject = Sector.Default;
        LowestCeilingObject = Sector.Default;
        HighestFloorSector = Sector.Default;
        LowestCeilingSector = Sector.Default;
        SlowTickMultiplier = 1;
        ChaseFailureSkipCount = 0;
        ClosetChaseSpeed = DefaultClosetChaseSpeed;
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

    protected virtual void SetDeath(Entity? source, bool gibbed)
    {
        if (Flags.Missile)
        {
            PlayDeathSound();
            Flags.Missile = false;
            Velocity = Vec3D.Zero;
        }
        else
        {
            Flags.Corpse = true;
            Flags.Dropoff = true;
            Flags.Skullfly = false;
            Flags.Shootable = false;
            if (!Flags.DontFall)
                Flags.NoGravity = false;
        }

        WorldStatic.World.HandleEntityDeath(this, source, gibbed);
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

    public double GetDistanceFrom(Entity listenerEntity)
    {
        return Position.Distance(listenerEntity.Position);
    }

    public virtual void SoundCreated(SoundInfo soundInfo, IAudioSource? audioSource, SoundChannel channel)
    {

    }

    public virtual bool TryClearSound(string sound, SoundChannel channel, out IAudioSource? clearedSound)
    {
        clearedSound = null;
        return false;
    }

    public virtual void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        
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
