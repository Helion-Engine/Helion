using Helion.Audio;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Models;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Islands;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Helion.Util.Assertion.Assert;
using static Microsoft.FSharp.Core.ByRefKinds;

namespace Helion.World.Entities;

/// <summary>
/// An actor in a world.
/// </summary>
public partial class Entity : IDisposable, ITickable, ISoundSource, IRenderObject
{
    private const double Speed = 47000 / 65536.0;
    private const int ForceGibDamage = ushort.MaxValue;
    private const int KillDamage = ushort.MaxValue - 1;
    public const double FloatSpeed = 4.0;
    public static readonly int MaxSoundChannels = Enum.GetValues(typeof(SoundChannel)).Length;

    public int Id;
    public int ThingId;
    public EntityDefinition Definition;
    public EntityFlags Flags;
    public EntityProperties Properties;
    public EntityManager EntityManager;
    public FrameState FrameState;
    public IWorld World;
    public WorldSoundManager SoundManager;
    public double AngleRadians;
    public Vec3D PrevPosition;
    public Vec3D Position;
    public Vec3D SpawnPoint;
    public Vec3D CenterPoint => new(Position.X, Position.Y, Position.Z + (Height / 2));
    public Vec3D ProjectileAttackPos => new(Position.X, Position.Y, Position.Z + 32);
    public Vec3D HitscanAttackPos => new(Position.X, Position.Y, Position.Z + (Height / 2) + 8);
    public Vec3D Velocity = Vec3D.Zero;
    public int Health;
    public int Armor;
    public EntityProperties? ArmorProperties => ArmorDefinition?.Properties;
    public EntityDefinition? ArmorDefinition;
    public int FrozenTics;
    public int MoveCount;
    public Sector Sector;
    public Sector HighestFloorSector;
    public Sector LowestCeilingSector;
    // Can be Sector or Entity
    public object HighestFloorObject;
    public object LowestCeilingObject;
    public double LowestCeilingZ;
    public double HighestFloorZ;
    public DynamicArray<Sector> IntersectSectors = new();
    public Line? BlockingLine;
    public Entity? BlockingEntity;
    public SectorPlane? BlockingSectorPlane;
    // Possible line with middle texture clipping player's view.
    public bool ViewLineClip;
    public WeakEntity Target { get; private set; } = WeakEntity.Default;
    public WeakEntity Tracer { get; private set; } = WeakEntity.Default;
    public WeakEntity OnEntity { get; private set; } = WeakEntity.Default;
    public WeakEntity OverEntity { get; private set; } = WeakEntity.Default;
    public WeakEntity Owner { get; private set; } = WeakEntity.Default;
    public Player? PickupPlayer;

    // Values that are modified from EntityProperties
    public int Threshold;
    public int ReactionTime;

    public bool OnGround;
    public bool Refire;
    public bool AttackDown;
    // If clipped with another entity. Value set with last SetEntityBoundsZ and my be stale.
    public bool ClippedWithEntity;
    public bool MoveLinked;
    public bool Respawn;
    public int BlockmapCount;
    public int PhysicsCount;

    public double RenderDistance { get; set; }
    public int RenderedCounter; // Used by the renderer only.
    public RenderObjectType Type => RenderObjectType.Entity;

    public virtual SoundChannel WeaponSoundChannel => SoundChannel.Default;
    public virtual int ProjectileKickBack => Properties.ProjectileKickBack;

    public bool IsBlocked() => BlockingEntity != null || BlockingLine != null || BlockingSectorPlane != null;
    public DynamicArray<LinkableNode<Entity>> BlockmapNodes = new();
    public DynamicArray<LinkableNode<Entity>> SectorNodes = new();
    public LinkableNode<Entity>? SubsectorNode;
    public LinkableNode<Entity>? EntityListNode;
    public bool IsDisposed { get; private set; }

    // Temporary storage variable for handling PhysicsManager.SectorMoveZ
    public double SaveZ;
    public double PrevSaveZ;
    public bool WasCrushing;
    public bool InMonsterCloset;
    public Island? Island;

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
    public double TopZ => Position.Z + Height;

    public IAudioSource[] SoundChannels = new IAudioSource[Entity.MaxSoundChannels];

    public Entity() { }

    public void Set(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, IWorld world)
    {
        IsDisposed = false;
        Health = definition.Properties.Health;

        Id = id;
        ThingId = thingId;
        Definition = definition;
        Flags = definition.Flags;
        Properties = definition.Properties;
        Threshold = Properties.Threshold;
        ReactionTime = Properties.ReactionTime;

        World = world;
        EntityManager = world.EntityManager;
        SoundManager = world.SoundManager;

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

        FrameState = new(this, definition, world.EntityManager);
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

        World = world;
        EntityManager = world.EntityManager;
        SoundManager = world.SoundManager;

        AngleRadians = entityModel.AngleRadians;

        Position = entityModel.Box.GetCenter();
        Height = entityModel.Box.Height;
        Radius = entityModel.Box.Radius;

        PrevPosition = entityModel.Box.GetCenter();
        Velocity = entityModel.GetVelocity();
        SpawnPoint = entityModel.GetSpawnPoint();
        Sector = world.Sectors[entityModel.Sector];

        Refire = entityModel.Refire;
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
            ArmorDefinition = world.EntityManager.DefinitionComposer.GetByName(entityModel.ArmorDefinition);

        FrameState = new(this, definition, world.EntityManager, entityModel.Frame);
    }

    public EntityModel ToEntityModel(EntityModel entityModel)
    {
        entityModel.Name = Definition.Name.ToString();
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
        entityModel.Refire = Refire;
        entityModel.MoveLinked = MoveLinked;
        entityModel.Respawn = Respawn;
        entityModel.Sector = Sector.Id;
        entityModel.MoveDir = (int)m_direction;
        entityModel.BlockFloat = BlockFloating;
        entityModel.ArmorDefinition = ArmorDefinition?.Name.ToString();
        entityModel.Frame = FrameState.ToFrameStateModel();
        entityModel.Flags = Flags.ToEntityFlagsModel();
        entityModel.Threshold = Threshold;
        entityModel.ReactionTime = ReactionTime;
        entityModel.HighSec = HighestFloorSector.Id;
        entityModel.LowSec = LowestCeilingSector.Id;
        entityModel.HighEntity = GetBoundingEntityForModel(HighestFloorObject);
        entityModel.LowEntity = GetBoundingEntityForModel(LowestCeilingObject);
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
    public double PitchTo(in Vec3D start, Entity entity) => start.Pitch(entity.CenterPoint, Position.XY.Distance(entity.Position.XY));

    public string GetBloodType()
    {
        if (!string.IsNullOrEmpty(Definition.Properties.BloodType))
            return Definition.Properties.BloodType;
        return "BLOOD";
    }

    /// <summary>
    /// Sets the height of the entity's box.
    /// </summary>
    /// <param name="height">The height to set.</param>
    public void SetHeight(double height)
    {
        Height = height;
    }

    /// <summary>
    /// Sets the entity to the new X/Y coordinates.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetXY(Vec2D position)
    {
        Position.X = position.X;
        Position.Y = position.Y;
    }

    /// <summary>
    /// Sets the entity to the new coordinate.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(Vec3D position)
    {
        Position = position;
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
            var node = SectorNodes[i];
            node.Unlink();
            World.DataCache.FreeLinkableNodeEntity(node);
            SectorNodes.Data[i] = null;
        }

        if (SubsectorNode != null)
        {
            SubsectorNode.Unlink();
            World.DataCache.FreeLinkableNodeEntity(SubsectorNode);
        }
        SectorNodes.Clear();
        SubsectorNode = null;

        for (int i = 0; i < BlockmapNodes.Length; i++)
        {
            var node = BlockmapNodes[i];
            node.Unlink();
            World.DataCache.FreeLinkableNodeEntity(node);
            BlockmapNodes.Data[i] = null;
        }
        BlockmapNodes.Clear();

        IntersectSectors.Clear();
        BlockingLine = null;
        BlockingEntity = null;
        BlockingSectorPlane = null;
    }

    /// <summary>
    /// Runs any tickable logic on the entity.
    /// </summary>
    public virtual void Tick()
    {
        PrevPosition = Position;
        Flags.Teleport = false;

        if (FrozenTics > 0)
            FrozenTics--;

        if (Flags.BossSpawnShot && ReactionTime > 0)
            ReactionTime--;

        FrameState.Tick();

        if (IsDisposed)
            return;

        if (Flags.CountKill && IsDeathStateFinished)
        {
            if (World.SkillDefinition.RespawnTime.Seconds == 0)
                return;

            MoveCount++;

            if (MoveCount < World.SkillDefinition.RespawnTime.Seconds * (int)Constants.TicksPerSecond)
                return;

            if ((World.LevelTime & 31) != 0)
                return;

            if (World.Random.NextByte() > 4)
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
        SetHeight(Definition.Properties.Height / 4.0);

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
        if (Definition.States.Labels.ContainsKey(Constants.FrameStates.Death))
        {
            SetDeath(source, false);
            if (Definition.DeathState != null)
                FrameState.SetFrameIndex(Definition.DeathState.Value);

            // Vanilla would set the ticks to 1 if less than 1 always because it didn't care if it was actually randomized.
            // Not doing this can break dehacked frames...
            if (Flags.Randomize || World.ArchiveCollection.Definitions.DehackedDefinition != null)
                SetRandomizeTicks();
        }
    }

    public void SetXDeathState(Entity? source)
    {
        if (Definition.States.Labels.ContainsKey(Constants.FrameStates.XDeath))
        {
            SetDeath(source, true);
            if (Definition.XDeathState != null)
                FrameState.SetFrameIndex(Definition.XDeathState.Value);
        }
    }

    public bool SetCrushState()
    {
        // Check if there is a Crush state, otherwise default to GenericCrush
        if (FrameState.SetState(Constants.FrameStates.Crush, warn: false) ||
            FrameState.SetState(Constants.FrameStates.GenericCrush, warn: false))
        {
            Flags.DontGib = true;
            Flags.Solid = false;
            SetHeight(0.0);
            return true;
        }

        return false;
    }

    public virtual void SetRaiseState()
    {        
        if (Definition.RaiseState != null)
        {
            FrameState.SetFrameIndex(Definition.RaiseState.Value);
            Health = Definition.Properties.Health;
            SetHeight(Definition.Properties.Height);
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
        SoundManager.CreateSoundOn(this, Definition.Properties.SeeSound,
            new SoundParams(this, attenuation: attenuation, type: SoundType.See));
    }

    public void PlayDeathSound()
    {
        if (Definition.Properties.DeathSound.Length == 0)
            return;

        Attenuation attenuation = (Flags.FullVolDeath || Flags.Boss) ? Attenuation.None : Attenuation.Default;
        SoundManager.CreateSoundOn(this, Definition.Properties.DeathSound,
            new SoundParams(this, attenuation: attenuation));
    }

    public void PlayAttackSound()
    {
        if (Properties.AttackSound.Length > 0)
            SoundManager.CreateSoundOn(this, Definition.Properties.AttackSound, new SoundParams(this));
    }

    public void PlayActiveSound()
    {
        if (Properties.ActiveSound.Length > 0)
            SoundManager.CreateSoundOn(this, Definition.Properties.ActiveSound,
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
            var def = World.EntityManager.DefinitionComposer.GetByName(Definition.ParentClassNames[i]);
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

        if (World.MapInfo.HasOption(MapOptions.TotalInfighting))
            return true;
        if (World.MapInfo.HasOption(MapOptions.NoInfighting))
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

        if (source != null)
        {
            Entity damageSource = source.Owner.Entity ?? source;
            if (!CanDamage(source, damageType))
                return false;

            if (WillRetaliateFrom(damageSource) && Threshold <= 0 && !damageSource.IsDead && damageSource != Target.Entity && damageSource != this)
            {
                if (!Flags.QuickToRetaliate)
                    Threshold = Properties.DefThreshold;
                if (!damageSource.Flags.NoTarget && !IsFriend(damageSource))
                    SetTarget(damageSource);
                if (Definition.SeeState != null && FrameState.IsState(Constants.FrameStates.Spawn))
                    SetSeeState();
            }
        }

        if (damage == ForceGibDamage)
        {
            Health = -Properties.Health - 1;
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
        }
        else if (setPainState && !Flags.Skullfly && Definition.PainState != null)
        {
            Flags.JustHit = true;
            FrameState.SetFrameIndex(Definition.PainState.Value);
        }

        // Skullfly is not turned off here as the original game did not do this
        if (Flags.Skullfly)
            Velocity = Vec3D.Zero;

        return true;
    }

    public void SetRandomizeTicks(int opAnd = 3) =>
        FrameState.SetTics(FrameState.CurrentTick - (World.Random.NextByte() & opAnd));

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

    /// <summary>
    /// Returns a list of all entities that are able to block this entity (using CanBlockEntity) in a 2D space from IntersectSectors.
    /// </summary>
    public DynamicArray<Entity> GetIntersectingEntities2D()
    {
        DynamicArray<Entity> entities = World.DataCache.GetEntityList();
        DynamicArray<BlockmapIntersect> intersections = World.BlockmapTraverser.GetSolidEntityIntersections(GetBox2D());

        for (int i = 0; i < intersections.Length; i++)
        {
            Entity? entity = intersections[i].Entity;
            if (entity == null)
                continue;

            if (CanBlockEntity(entity) && entity.Overlaps2D(this))
                entities.Add(entity);
        }

        World.DataCache.FreeBlockmapIntersectList(intersections);
        return entities;
    }

    /// <summary>
    /// Returns a list of all entities that are able to block this entity (using CanBlockEntity) in a 3D space traversing the block map.
    /// </summary>
    /// <param name="position">The position to check this entity against.</param>
    /// <param name="entityTraverseFlags">Flags to check against for traversal of the block map.</param>
    public List<Entity> GetIntersectingEntities3D(in Vec3D position, BlockmapTraverseEntityFlags entityTraverseFlags)
    {
        List<Entity> entities = new();
        Box3D box = new(position, Radius, Height);
        DynamicArray<BlockmapIntersect> intersections = World.BlockmapTraverser.GetBlockmapIntersections(new Box2D(position.XY, Radius), BlockmapTraverseFlags.Entities, entityTraverseFlags);

        for (int i = 0; i < intersections.Length; i++)
        {
            Entity? entity = intersections[i].Entity;
            if (entity == null)
                continue;

            if (CanBlockEntity(entity) && entity.Overlaps(box))
                entities.Add(entity);
        }

        World.DataCache.FreeBlockmapIntersectList(intersections);
        return entities;
    }

    public bool CanBlockEntity(Entity other)
    {
        if (ReferenceEquals(this, other) || Owner.Entity == other || !other.Flags.Solid || other.Flags.NoClip)
            return false;

        if (Flags.Ripper)
            return false;

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
        if (Flags.MbfBouncer && Flags.NoGravity)
            return false;

        if (Flags.NoFriction || Flags.Missile || Flags.Skullfly)
            return false;

        return Flags.NoGravity || OnGround;
    }

    /// <summary>
    /// Validates ClippedWithEntity. Iterates through the intersecting entities in the sector.
    /// </summary>
    public bool IsClippedWithEntity()
    {
        if (!Flags.Solid)
            return false;

        DynamicArray<Entity> entities = GetIntersectingEntities2D();
        for (int i = 0; i < entities.Length; i++)
        {
            if (entities[i].OverlapsZ(this))
            {
                World.DataCache.FreeEntityList(entities);
                return true;
            }
        }

        World.DataCache.FreeEntityList(entities);
        return false;
    }

    public bool ShouldCheckDropOff()
    {
        if (Flags.Float || Flags.Dropoff)
            return false;

        // Only allow for non-monster things. Currently the physics code allows monsters to easily get stuck.
        // There are boom maps that require item movement like this (e.g. Fractured Worlds MAP03 red key BFG)
        if (World.Config.Compatibility.AllowItemDropoff)
            return Definition.Properties.Health > 0 && Definition.States.Labels.ContainsKey(Constants.FrameStates.See);

        return true;
    }

    public bool CheckDropOff(TryMoveData tryMove)
    {
        if (!ShouldCheckDropOff())
            return true;

        if (tryMove.DropOffEntity != null && !tryMove.DropOffEntity.Flags.ActLikeBridge)
            return false;

        // Walking on things test
        for (int i = 0; i < tryMove.IntersectEntities2D.Length; i++)
        {
            Entity entity = tryMove.IntersectEntities2D[i];
            if (!CanBlockEntity(entity) || !entity.Flags.ActLikeBridge)
                continue;
            if (entity.TopZ > tryMove.DropOffZ)
                tryMove.DropOffZ = entity.TopZ;
        }

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
                int damage = Properties.Damage.Get(World.Random);
                EntityManager.World.DamageEntity(BlockingEntity, this, damage, DamageType.AlwaysApply, Thrust.Horizontal);
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
                double velocityAngle = Math.Atan2(Velocity.X, Velocity.Y);
                double lineAngle = BlockingLine.Segment.Start.Angle(BlockingLine.Segment.End);
                double newAngle = 2 * lineAngle - velocityAngle;
                if (MathHelper.GetPositiveAngle(newAngle) == MathHelper.GetPositiveAngle(velocityAngle))
                    newAngle += MathHelper.Pi;
                Vec2D velocity2D = velocity.XY.Rotate(newAngle - velocityAngle);
                Velocity.X = velocity2D.X;
                Velocity.Y = velocity2D.Y;
            }
        }
    }

    public bool ShouldDieOnCollison()
    {
        if (Flags.MbfBouncer && Flags.Missile)
            return BlockingEntity != null || BlockingLine != null;

        if (Flags.Missile)
            return true;

        return false;
    }

    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        UnlinkFromWorld();
        
        if (EntityListNode != null)
        {
            EntityListNode.Unlink();
            World.DataCache.FreeLinkableNodeEntity(EntityListNode);
            EntityListNode = null;
        }

        FrameState.SetFrameIndex(Constants.NullFrameIndex);

        BlockmapNodes.Clear();
        SectorNodes.Clear();
        IntersectSectors.Clear();

        for (int i = 0; i < SoundChannels.Length; i++)
            SoundChannels[i] = null;

        Target = WeakEntity.Default;
        Tracer = WeakEntity.Default;
        OnEntity = WeakEntity.Default;
        OverEntity = WeakEntity.Default;
        Owner = WeakEntity.Default;
        PickupPlayer = null;

        World.DataCache.FreeEntity(this);

        WeakEntity.DisposeEntity(this);
        World = null!;
        EntityManager = null!;
        SoundManager = null!;
        Definition = null!;

        Velocity = Vec3D.Zero;

        OnGround = false;
        Refire = false;
        AttackDown = false;
        ClippedWithEntity = false;
        SaveZ = 0;
        PrevSaveZ = 0;
        MoveCount = 0;
        FrozenTics = 0;
        MoveLinked = false;
        Respawn = false;
        WasCrushing = false;
        InMonsterCloset = false;
        Island = null;
        BlockingLine = null;
        BlockingEntity = null;
        BlockingSectorPlane = null;
        Sector = Sector.Default;
        HighestFloorObject = Sector.Default;
        LowestCeilingObject = Sector.Default;
        HighestFloorSector = Sector.Default;
        LowestCeilingSector = Sector.Default;
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
            Flags.Skullfly = false;
            Flags.Shootable = false;
            if (!Flags.DontFall)
                Flags.NoGravity = false;
        }

        World.HandleEntityDeath(this, source, gibbed);
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

    public void SoundCreated(IAudioSource audioSource, SoundChannel channel)
    {
        SoundChannels[(int)channel] = audioSource;
    }

    public double GetDistanceFrom(Entity listenerEntity)
    {
        return Position.Distance(listenerEntity.Position);
    }

    public IAudioSource? TryClearSound(string sound, SoundChannel channel)
    {
        IAudioSource? audioSource = SoundChannels[(int)channel];
        if (audioSource != null)
        {
            SoundChannels[(int)channel] = null;
            return audioSource;
        }

        return null;
    }

    public void ClearSound(IAudioSource audioSource, SoundChannel channel)
    {
        SoundChannels[(int)channel] = null;
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

    private bool WillRetaliateFrom(Entity damageSource)
    {
        if (damageSource.IsPlayer)
            return true;

        if (Properties.InfightingGroup.NullableEquals(damageSource.Properties.InfightingGroup))
            return false;

        return true;
    }
}
