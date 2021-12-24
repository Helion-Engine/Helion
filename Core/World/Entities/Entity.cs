using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Physics;
using Helion.World.Physics.Blockmap;
using Helion.World.Sound;
using static Helion.Util.Assertion.Assert;
using Helion.Resources.Definitions.MapInfo;
using Helion.Render.Legacy.Renderers.Legacy.World;

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
    public static readonly int MaxSoundChannels = Enum.GetValues(typeof(SoundChannelType)).Length;

    public readonly int Id;
    public readonly int ThingId;
    public readonly EntityDefinition Definition;
    public EntityFlags Flags;
    public EntityProperties Properties;
    public readonly EntityManager EntityManager;
    public readonly FrameState FrameState;
    public readonly IWorld World;
    public readonly WorldSoundManager SoundManager;
    public double AngleRadians;
    public EntityBox Box;
    public Vec3D PrevPosition;
    public Vec3D SpawnPoint;
    public Vec3D Position => Box.Position;
    public Vec3D CenterPoint => new Vec3D(Box.Position.X, Box.Position.Y, Box.Position.Z + (Height / 2));
    public Vec3D ProjectileAttackPos => new Vec3D(Position.X, Position.Y, Position.Z + 32);
    public Vec3D HitscanAttackPos => new Vec3D(Position.X, Position.Y, Position.Z + (Height / 2) + 8);
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
    public List<Sector> IntersectSectors = new List<Sector>();
    // The entity we are standing on.
    public Entity? OnEntity;
    // The entity standing on our head.
    public Entity? OverEntity;
    public Entity? Owner;
    public Line? BlockingLine;
    public Entity? BlockingEntity;
    public SectorPlane? BlockingSectorPlane;
    public Entity? Target;
    public Entity? Tracer;

    // Values that are modified from EntityProperties
    public int Threshold;
    public int ReactionTime;

    public bool OnGround;
    public bool Refire;
    // If clipped with another entity. Value set with last SetEntityBoundsZ and my be stale.
    public bool ClippedWithEntity;
    public bool MoveLinked;
    public bool Respawn;

    public double RenderDistance { get; set; }
    public RenderObjectType Type => RenderObjectType.Entity;

    public virtual SoundChannelType WeaponSoundChannel => SoundChannelType.Auto;
    public virtual int ProjectileKickBack => Properties.ProjectileKickBack;

    public bool IsBlocked() => BlockingEntity != null || BlockingLine != null || BlockingSectorPlane != null;
    protected internal List<LinkableNode<Entity>> BlockmapNodes = new List<LinkableNode<Entity>>();
    protected internal List<LinkableNode<Entity>> SectorNodes = new List<LinkableNode<Entity>>();
    protected internal LinkableNode<Entity>? SubsectorNode;
    protected internal LinkableNode<Entity>? EntityListNode;
    internal bool IsDisposed { get; private set; }

    // Temporary storage variable for handling PhysicsManager.SectorMoveZ
    public double SaveZ;
    public double PrevSaveZ;
    public bool WasCrushing;

    public double Height => Box.Height;
    public double Radius => Definition.Properties.Radius;
    public bool IsFrozen => FrozenTics > 0;
    public bool IsDead => Health <= 0;
    public EntityFrame Frame => FrameState.Frame;
    public virtual double ViewZ => 8.0;
    public bool IsDeathStateFinished => IsDead && Frame.Ticks == -1;
    public virtual bool IsInvulnerable => Flags.Invulnerable;
    public virtual Player? PlayerObj => null;
    public virtual bool IsPlayer => false;

    private readonly IAudioSource?[] m_soundChannels = new IAudioSource[MaxSoundChannels];

    /// <summary>
    /// Creates an entity with the following information.
    /// </summary>
    /// <param name="id">A unique ID for this entity.</param>
    /// <param name="thingId">The 'tid', which is a lookup ID. This differs
    /// from the ID because multiple entities can share the thing ID, but
    /// the other one must be unique.</param>
    /// <param name="definition">The definitions for the entity.</param>
    /// <param name="position">The location in the world.</param>
    /// <param name="angleRadians">The angle in radians.</param>
    /// <param name="sector">The sector that the center of the entity is on.
    /// </param>
    /// <param name="entityManager">The entity manager that created this
    /// entity (so the entity can destroy itself if needed).</param>
    /// <param name="soundManager">The sound manager to which we can play
    /// any sounds with.</param>
    /// <param name="world">The world this entity belongs to.</param>
    public Entity(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
        Sector sector, EntityManager entityManager, WorldSoundManager soundManager, IWorld world)
    {
        Health = definition.Properties.Health;

        Id = id;
        ThingId = thingId;
        Definition = definition;
        Flags = definition.Flags;
        Properties = definition.Properties;
        Threshold = Properties.Threshold;
        ReactionTime = Properties.ReactionTime;

        FrameState = new FrameState(this, definition, entityManager);
        World = world;
        EntityManager = entityManager;
        SoundManager = soundManager;

        AngleRadians = angleRadians;
        Box = new EntityBox(position, Radius, definition.Properties.Height);
        PrevPosition = Box.Position;
        Sector = sector;
        LowestCeilingZ = sector.Ceiling.Z;
        HighestFloorZ = sector.Floor.Z;
        HighestFloorSector = sector;
        HighestFloorObject = sector;
        LowestCeilingSector = sector;
        LowestCeilingObject = sector;
        CheckOnGround();

        Properties.Threshold = 0;
    }

    public Entity(EntityModel entityModel, EntityDefinition definition, EntityManager entityManager,
        WorldSoundManager soundManager, IWorld world)
    {
        Id = entityModel.Id;
        ThingId = entityModel.ThingId;
        Definition = definition;
        Flags = new EntityFlags(entityModel.Flags);
        Properties = definition.Properties;
        Threshold = entityModel.Threshold;
        ReactionTime = entityModel.ReactionTime;

        Health = entityModel.Health;
        Armor = entityModel.Armor;

        FrameState = new FrameState(this, definition, entityManager, entityModel.Frame);
        World = world;
        EntityManager = entityManager;
        SoundManager = soundManager;

        AngleRadians = entityModel.AngleRadians;
        Box = new EntityBox(entityModel.Box.GetCenter(), entityModel.Box.Radius, entityModel.Box.Height);
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
            ArmorDefinition = entityManager.DefinitionComposer.GetByName(entityModel.ArmorDefinition);
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
        entityModel.Box = Box.ToEntityBoxModel();
        entityModel.VelocityX = Velocity.X;
        entityModel.VelocityY = Velocity.Y;
        entityModel.VelocityZ = Velocity.Z;
        entityModel.Health = Health;
        entityModel.Armor = Armor;
        entityModel.FrozenTics = FrozenTics;
        entityModel.MoveCount = MoveCount;
        entityModel.Owner = Owner?.Id;
        entityModel.Target = Target?.Id;
        entityModel.Tracer = Tracer?.Id;
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
        return entityModel;
    }

    public virtual void CopyProperties(Entity entity)
    {
        Flags = entity.Flags;
        Health = entity.Health;
        Armor = entity.Armor;
        ArmorDefinition = entity.ArmorDefinition;
    }

    public double PitchTo(Entity entity) => Position.Pitch(entity.Position, Position.XY.Distance(entity.Position.XY));
    public double PitchTo(in Vec3D start, Entity entity) => start.Pitch(entity.CenterPoint, Position.XY.Distance(entity.Position.XY));

    public string GetBloodType()
    {
        if (!string.IsNullOrEmpty(Definition.Properties.BloodType))
            return Definition.Properties.BloodType;
        // TODO doom special cases...
        return "BLOOD";
    }

    /// <summary>
    /// Sets the bottom of the entity's center to be at the Z coordinate
    /// provided.
    /// </summary>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="smooth">If the entity should smooth the player's view
    /// height. This smooths the camera when stepping up to a higher sector.
    /// </param>
    public virtual void SetZ(double z, bool smooth)
    {
        Box.SetZ(z);
    }

    /// <summary>
    /// Sets the height of the entity's box.
    /// </summary>
    /// <param name="height">The height to set.</param>
    public void SetHeight(double height)
    {
        Box.SetHeight(height);
    }

    /// <summary>
    /// Sets the entity to the new X/Y coordinates.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetXY(Vec2D position)
    {
        Box.SetXY(position);
    }

    /// <summary>
    /// Sets the entity to the new coordinate.
    /// </summary>
    /// <param name="position">The new position.</param>
    public void SetPosition(Vec3D position)
    {
        Box.SetXY(position.XY);
        Box.SetZ(position.Z);
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
        for (int i = 0; i < SectorNodes.Count; i++)
        {
            SectorNodes[i].Unlink();
            DataCache.Instance.FreeLinkableNodeEntity(SectorNodes[i]);
        }

        if (SubsectorNode != null)
        {
            SubsectorNode.Unlink();
            DataCache.Instance.FreeLinkableNodeEntity(SubsectorNode);
        }
        SectorNodes.Clear();
        SubsectorNode = null;

        for (int i = 0; i < BlockmapNodes.Count; i++)
        {
            BlockmapNodes[i].Unlink();
            DataCache.Instance.FreeLinkableNodeEntity(BlockmapNodes[i]);
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

        if (FrozenTics > 0)
            FrozenTics--;

        if (Flags.BossSpawnShot && ReactionTime > 0)
            ReactionTime--;

        FrameState.Tick();

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
        Damage(null, ForceGibDamage, false, false);

    public void Kill(Entity? source) =>
        Damage(source, Health, false, false);

    private void KillInternal(Entity? source)
    {
        if (Health > 0)
            Health = 0;

        bool gib = Health < -Properties.Health;
        SetHeight(Definition.Properties.Height / 4.0);

        if (gib && HasXDeathState())
            SetXDeathState(source);
        else
            SetDeathState(source);
    }

    public void SetSpawnState() =>
        FrameState.SetState(Constants.FrameStates.Spawn);

    public void SetSeeState() =>
        FrameState.SetState(Constants.FrameStates.See);

    public void SetMissileState() =>
        FrameState.SetState(Constants.FrameStates.Missile);

    public void SetMeleeState() =>
        FrameState.SetState(Constants.FrameStates.Melee);

    public void SetDeathState(Entity? source)
    {
        if (Definition.States.Labels.ContainsKey(Constants.FrameStates.Death))
        {
            SetDeath(source, false);
            FrameState.SetState(Constants.FrameStates.Death);

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
            FrameState.SetState(Constants.FrameStates.XDeath);
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
        if (FrameState.SetState(Constants.FrameStates.Raise))
        {
            Health = Definition.Properties.Health;
            SetHeight(Definition.Properties.Height);
            Flags = Definition.Flags;
        }
    }

    public void SetHealState() =>
        FrameState.SetState(Constants.FrameStates.Heal);

    public void PlaySeeSound()
    {
        if (Definition.Properties.SeeSound.Length > 0)
            SoundManager.CreateSoundOn(this, Definition.Properties.SeeSound, SoundChannelType.Auto, DataCache.Instance.GetSoundParams(this));
    }

    public void PlayAttackSound()
    {
        if (Properties.AttackSound.Length > 0)
            SoundManager.CreateSoundOn(this, Definition.Properties.AttackSound, SoundChannelType.Auto, DataCache.Instance.GetSoundParams(this));
    }

    public void PlayActiveSound()
    {
        if (Properties.ActiveSound.Length > 0)
            SoundManager.CreateSoundOn(this, Definition.Properties.ActiveSound, SoundChannelType.Auto, DataCache.Instance.GetSoundParams(this));
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

    public virtual bool CanDamage(Entity source, bool isHitscan)
    {
        Entity damageSource = source.Owner ?? source;
        if (damageSource.IsPlayer)
            return true;

        if (World.MapInfo.HasOption(MapOptions.TotalInfighting))
            return true;
        if (World.MapInfo.HasOption(MapOptions.NoInfighting))
            return false;

        if (isHitscan)
            return true;

        if (GetSpeciesName().Equals(damageSource.GetSpeciesName()) && !Flags.DoHarmSpecies)
            return false;

        return true;
    }

    public virtual bool Damage(Entity? source, int damage, bool setPainState, bool isHitscan)
    {
        if (damage <= 0 || Flags.Invulnerable)
            return false;

        if (source != null)
        {
            Entity damageSource = source.Owner ?? source;
            if (!CanDamage(source, isHitscan))
                return false;

            if (Threshold <= 0 && !damageSource.IsDead && damageSource != Target)
            {
                if (!Flags.QuickToRetaliate)
                    Threshold = Properties.DefThreshold;
                if (!damageSource.Flags.NoTarget && !IsFriend(damageSource))
                    Target = damageSource;
                if (HasSeeState() && FrameState.IsState(Constants.FrameStates.Spawn))
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
        else if (setPainState && !Flags.Skullfly && HasPainState())
        {
            Flags.JustHit = true;
            FrameState.SetState(Constants.FrameStates.Pain);
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
        return damage;
    }

    protected static bool IsWeapon(EntityDefinition definition) => definition.IsType(Inventory.WeaponClassName);
    protected static bool IsAmmo(EntityDefinition definition) => definition.IsType(Inventory.AmmoClassName);

    public bool HasMissileState() => Definition.States.Labels.ContainsKey(Constants.FrameStates.Missile);
    public bool HasMeleeState() => Definition.States.Labels.ContainsKey(Constants.FrameStates.Melee);
    public bool HasXDeathState() => Definition.States.Labels.ContainsKey(Constants.FrameStates.XDeath);
    public bool HasRaiseState() => Definition.States.Labels.ContainsKey(Constants.FrameStates.Raise);
    public bool HasSeeState() => Definition.States.Labels.ContainsKey(Constants.FrameStates.See);
    public bool HasPainState() => Definition.States.Labels.ContainsKey(Constants.FrameStates.Pain);
    public bool IsCrushing() => LowestCeilingZ - HighestFloorZ < Height;
    public void CheckOnGround() => OnGround = HighestFloorZ >= Position.Z;
    public bool IsFriend(Entity entity) => Flags.Friendly && entity.Flags.Friendly;

    /// <summary>
    /// Returns a list of all entities that are able to block this entity (using CanBlockEntity) in a 2D space from IntersectSectors.
    /// </summary>
    public List<Entity> GetIntersectingEntities2D(BlockmapTraverseEntityFlags entityTraverseFlags)
    {
        List<Entity> entities = new List<Entity>();
        List<BlockmapIntersect> intersections = World.BlockmapTraverser.GetBlockmapIntersections(Box.To2D(), BlockmapTraverseFlags.Entities, entityTraverseFlags);

        for (int i = 0; i < intersections.Count; i++)
        {
            Entity? entity = intersections[i].Entity;
            if (entity == null)
                continue;

            if (CanBlockEntity(entity) && entity.Box.Overlaps2D(Box))
                entities.Add(entity);
        }

        DataCache.Instance.FreeBlockmapIntersectList(intersections);

        return entities;
    }

    /// <summary>
    /// Returns a list of all entities that are able to block this entity (using CanBlockEntity) in a 3D space traversing the block map.
    /// </summary>
    /// <param name="position">The position to check this entity against.</param>
    /// <param name="entityTraverseFlags">Flags to check against for traversal of the block map.</param>
    public List<Entity> GetIntersectingEntities3D(in Vec3D position, BlockmapTraverseEntityFlags entityTraverseFlags)
    {
        List<Entity> entities = new List<Entity>();
        EntityBox box = new EntityBox(position, Radius, Height);
        List<BlockmapIntersect> intersections = World.BlockmapTraverser.GetBlockmapIntersections(box.To2D(), BlockmapTraverseFlags.Entities, entityTraverseFlags);

        for (int i = 0; i < intersections.Count; i++)
        {
            Entity? entity = intersections[i].Entity;
            if (entity == null)
                continue;

            if (CanBlockEntity(entity) && box.Overlaps(entity.Box))
                entities.Add(entity);
        }

        DataCache.Instance.FreeBlockmapIntersectList(intersections);

        return entities;
    }

    public bool CanBlockEntity(Entity other)
    {
        if (ReferenceEquals(this, other) || Owner == other || !other.Flags.Solid || other.Flags.NoClip)
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

        // If still clipped with another entity then do not apply gravity
        if (ClippedWithEntity)
        {
            if (IsClippedWithEntity())
                return false;

            ClippedWithEntity = false;
        }

        // Applying gravity if we are on another entity.
        // This prevents issues with this entity floating
        // when the entity beneath is no longer blocking.
        return OnEntity != null || !OnGround;
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

        List<Entity> entities = GetIntersectingEntities2D(BlockmapTraverseEntityFlags.Solid);

        for (int i = 0; i < entities.Count; i++)
        {
            if (entities[i].Box.OverlapsZ(Box))
                return true;
        }

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
        for (int i = 0; i < tryMove.IntersectEntities2D.Count; i++)
        {
            Entity entity = tryMove.IntersectEntities2D[i];
            if (!CanBlockEntity(entity) || !entity.Flags.ActLikeBridge)
                continue;
            if (entity.Box.Top > tryMove.DropOffZ)
                tryMove.DropOffZ = entity.Box.Top;
        }

        if (tryMove.IntersectEntities2D.Count == 0 && tryMove.DropOffEntity != null)
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
                EntityManager.World.DamageEntity(BlockingEntity, this, damage, false, Thrust.Horizontal);
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

            if (bounceWall && BlockingLine  != null)
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
        UnlinkFromWorld();
        EntityListNode?.Unlink();
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    protected virtual void SetDeath(Entity? source, bool gibbed)
    {
        if (Flags.Missile)
        {
            if (Definition.Properties.DeathSound.Length > 0)
                SoundManager.CreateSoundOn(this, Definition.Properties.DeathSound, SoundChannelType.Auto, DataCache.Instance.GetSoundParams(this));

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
        return $"[{Definition}] [{Position}]";
    }

    public void SoundCreated(IAudioSource audioSource, SoundChannelType channel)
    {
        m_soundChannels[(int)channel] = audioSource;
    }

    public double GetDistanceFrom(Entity listenerEntity)
    {
        return Position.Distance(listenerEntity.Position);
    }

    public IAudioSource? TryClearSound(string sound, SoundChannelType channel)
    {
        IAudioSource? audioSource = m_soundChannels[(int)channel];
        if (audioSource != null)
        {
            m_soundChannels[(int)channel] = null;
            return audioSource;
        }

        return null;
    }

    public void ClearSound(IAudioSource audioSource, SoundChannelType channel)
    {
        m_soundChannels[(int)channel] = null;
    }

    public Vec3D? GetSoundPosition(Entity listenerEntity)
    {
        return Position;
    }

    public Vec3D? GetSoundVelocity()
    {
        return Velocity;
    }

    public bool CanAttenuate(SoundInfo soundInfo)
    {
        if (Flags.Boss && (soundInfo.Name.Equals(Definition.Properties.SeeSound, StringComparison.OrdinalIgnoreCase) || soundInfo.Name.Equals(Definition.Properties.DeathSound, StringComparison.OrdinalIgnoreCase)))
            return false;

        return true;
    }

    public virtual bool CanMakeSound() => true;
}
