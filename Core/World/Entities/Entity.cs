using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Audio;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Geometry.Vectors;
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

namespace Helion.World.Entities
{
    /// <summary>
    /// An actor in a world.
    /// </summary>
    public partial class Entity : IDisposable, ITickable, ISoundSource
    {
        private const double Speed = 47000 / 65536.0;
        public const double FloatSpeed = 4.0;
        public static readonly int MaxSoundChannels = Enum.GetValues(typeof(SoundChannelType)).Length;

        public readonly int Id;
        public readonly int ThingId;
        public readonly EntityDefinition Definition;
        public EntityFlags Flags { get; protected set; }
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

        public bool OnGround;
        public bool Refire;
        // If clipped with another entity. Value set with last SetEntityBoundsZ and my be stale.
        public bool ClippedWithEntity;
        public bool MoveLinked;
        public bool Respawn;

        public double RenderDistance;

        public virtual SoundChannelType WeaponSoundChannel => SoundChannelType.Auto;

        public bool IsBlocked() => BlockingEntity != null || BlockingLine != null || BlockingSectorPlane != null;
        protected internal LinkableNode<Entity> EntityListNode = new LinkableNode<Entity>();
        protected internal List<LinkableNode<Entity>> BlockmapNodes = new List<LinkableNode<Entity>>();
        protected internal List<LinkableNode<Entity>> SectorNodes = new List<LinkableNode<Entity>>();
        protected internal LinkableNode<Entity>? SubsectorNode;
        internal bool IsDisposed { get; private set; }

        // Temporary storage variable for handling PhysicsManager.SectorMoveZ
        public double SaveZ;

        public double Height => Box.Height;
        public double Radius => Definition.Properties.Radius;
        public bool IsFrozen => FrozenTics > 0;
        public bool IsDead => Health == 0;
        public EntityFrame Frame => FrameState.Frame;
        public virtual double ViewZ => 8.0;
        public bool IsDeathStateFinished => IsDead && Frame.Ticks == -1;
        public virtual bool IsInvulnerable => Flags.Invulnerable;

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
        /// /// <param name="world">The world this entity belongs to.</param>
        public Entity(int id, int thingId, EntityDefinition definition, in Vec3D position, double angleRadians,
            Sector sector, EntityManager entityManager, WorldSoundManager soundManager, IWorld world)
        {
            Health = definition.Properties.Health;

            Id = id;
            ThingId = thingId;
            Definition = definition;
            Flags = new EntityFlags(definition.Flags);
            // TODO there was a reason for Definition.Properties and Properties being different...
            Properties = new EntityProperties(definition.Properties);
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
            Properties = new EntityProperties(definition.Properties);
            Health = entityModel.Health;
            Armor = entityModel.Armor;

            FrameState = new FrameState(this, definition, entityManager, entityModel.Frame);
            World = world;
            EntityManager = entityManager;
            SoundManager = soundManager;

            AngleRadians = entityModel.AngleRadians;
            Box = new EntityBox(entityModel.Box.Center, entityModel.Box.Radius, entityModel.Box.Height);
            PrevPosition = entityModel.Box.Center;
            Velocity = entityModel.Velocity;
            SpawnPoint = entityModel.SpawnPoint;
            Sector = world.Sectors[entityModel.Sector];

            Refire = entityModel.Refire;
            MoveLinked = entityModel.MoveLinked;
            Respawn = entityModel.Respawn;

            m_direction = (MoveDir)entityModel.MoveDir;
            BlockFloating = entityModel.BlockFloat;
            MoveCount = entityModel.MoveCount;
            FrozenTics = entityModel.FrozenTics;

            if (entityModel.ArmorDefinition != null)
                ArmorDefinition = entityManager.DefinitionComposer.GetByName(entityModel.ArmorDefinition);

            Properties.ApplyEntityPropertiesModel(entityModel.Properties);
        }

        public EntityModel ToEntityModel(EntityModel entityModel)
        {
            if (ThingId > 0)
            {
                int lol = 1;
            }

            entityModel.Name = Definition.Name.ToString();
            entityModel.Id = Id;
            entityModel.ThingId = ThingId;
            entityModel.AngleRadians = AngleRadians;
            entityModel.SpawnPoint = SpawnPoint;
            entityModel.Box = Box.ToEntityBoxModel();
            entityModel.Velocity = Velocity;
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
            entityModel.Properties = Properties.ToEntityPropertiesModel();
            return entityModel;
        }

        public virtual void CopyProperties(Entity entity)
        {
            Properties = entity.Properties;
            Flags = entity.Flags;
            Health = entity.Health;
            Armor = entity.Armor;
            ArmorDefinition = entity.ArmorDefinition;
        }

        public double PitchTo(Entity entity) => Position.Pitch(entity.Position, Position.To2D().Distance(entity.Position.To2D()));
        public double PitchTo(in Vec3D start, Entity entity) => start.Pitch(entity.CenterPoint, Position.To2D().Distance(entity.Position.To2D()));

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
            Box.SetXY(position.To2D());
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

            FrameState.Tick();

            if (Flags.Monster && IsDeathStateFinished)
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

        public void ForceGib()
        {
            Health = -Properties.Health - 1;
            Kill(null);
        }

        public void Kill(Entity? source)
        {
            if (Health < -Properties.Health && HasXDeathState())
                SetXDeathState(source);
            else
                SetDeathState(source);

            Health = 0;
            SetHeight(Definition.Properties.Height / 4.0);
        }

        public void SetSpawnState()
        {
            FrameState.SetState(FrameStateLabel.Spawn);
        }

        public void SetSeeState()
        {
            FrameState.SetState(FrameStateLabel.See);
        }

        public void SetMissileState()
        {
            FrameState.SetState(FrameStateLabel.Missile);
        }

        public void SetMeleeState()
        {
            FrameState.SetState(FrameStateLabel.Melee);
        }

        public void SetDeathState(Entity? source)
        {
            if (FrameState.SetState(FrameStateLabel.Death))
                SetDeath(source, false);
        }

        public void SetXDeathState(Entity? source)
        {
            if (FrameState.SetState(FrameStateLabel.XDeath))
                SetDeath(source, true);
        }

        public bool SetCrushState()
        {
            if (FrameState.SetState(FrameStateLabel.Crush))
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
            if (FrameState.SetState(FrameStateLabel.Raise))
            {
                Health = Definition.Properties.Health;
                SetHeight(Definition.Properties.Height);
                Flags = new EntityFlags(Definition.Flags);
            }
        }

        public void SetHealState()
        {
            FrameState.SetState(FrameStateLabel.Heal);
        }

        public void PlaySeeSound()
        {
            if (Definition.Properties.SeeSound.Length > 0)
                SoundManager.CreateSoundOn(this, Definition.Properties.SeeSound, SoundChannelType.Auto, new SoundParams(this));
        }

        public void PlayAttackSound()
        {
            if (Properties.AttackSound.Length > 0)
                SoundManager.CreateSoundOn(this, Definition.Properties.AttackSound, SoundChannelType.Auto, new SoundParams(this));
        }

        public void PlayActiveSound()
        {
            if (Properties.ActiveSound.Length > 0)
                SoundManager.CreateSoundOn(this, Definition.Properties.ActiveSound, SoundChannelType.Auto, new SoundParams(this));
        }

        public CIString GetSpeciesName()
        {
            if (Definition.ParentClassNames.Count < 2)
                return string.Empty;

            return Definition.ParentClassNames[^1];
        }

        public virtual bool CanDamage(Entity source)
        {
            Entity damageSource = source.Owner ?? source;

            if (damageSource is Player || !Flags.IsMonster)
                return true;

            // Not a projectile, always damage
            if (source.Owner == null)
                return true;

            if (GetSpeciesName().Equals(damageSource.GetSpeciesName()) && !Flags.DoHarmSpecies)
                return false;

            return true;
        }

        public virtual bool Damage(Entity? source, int damage, bool setPainState)
        {
            if (damage <= 0 || Flags.Invulnerable)
                return false;

            if (source != null)
            {
                Entity damageSource = source.Owner ?? source;
                if (!CanDamage(source))
                    return false;

                if (Properties.Threshold <= 0 && !damageSource.IsDead && damageSource != Target)
                {
                    if (!Flags.QuickToRetaliate)
                        Properties.Threshold = Properties.DefThreshold;
                    Target = damageSource;
                    if (HasSeeState() && FrameState.IsState(FrameStateLabel.Spawn))
                        SetSeeState();
                }
            }

            damage = ApplyArmorDamage(damage);

            Health -= damage;
            Properties.ReactionTime = 0;

            if (Health <= 0)
            {
                Kill(source);
            }
            else if (setPainState && !Flags.Skullfly && HasPainState())
            {
                Flags.JustHit = true;
                FrameState.SetState(FrameStateLabel.Pain);
                if (Definition.Properties.PainSound.Length > 0)
                    SoundManager.CreateSoundOn(this, Definition.Properties.PainSound, SoundChannelType.Auto, new SoundParams(this));
            }


            // Skullfly is not turned off here as the original game did not do this
            if (Flags.Skullfly)
                Velocity = Vec3D.Zero;

            return true;
        }

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

        public bool HasMissileState() => Definition.States.Labels.ContainsKey("MISSILE");
        public bool HasMeleeState() => Definition.States.Labels.ContainsKey("MELEE");
        public bool HasXDeathState() => Definition.States.Labels.ContainsKey("XDEATH");
        public bool HasRaiseState() => Definition.States.Labels.ContainsKey("RAISE");
        public bool HasSeeState() => Definition.States.Labels.ContainsKey("SEE");
        public bool HasPainState() => Definition.States.Labels.ContainsKey("PAIN");
        public bool IsCrushing() => LowestCeilingZ - HighestFloorZ < Height;
        public void CheckOnGround() => OnGround = HighestFloorZ >= Position.Z;

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

            return !OnGround;
        }

        public bool ShouldApplyFriction()
        {
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

        public bool ShouldCheckDropOff() => !Flags.Float && !Flags.Dropoff;
        // Allow drop off when monsters have momentum
        public bool CheckDropOff(TryMoveData tryMove)
        {
            if (!ShouldCheckDropOff() || !m_enemyMove)
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
                    EntityManager.World.DamageEntity(BlockingEntity, this, damage, Thrust.Horizontal);
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
        }

        public void Dispose()
        {
            UnlinkFromWorld();
            EntityListNode.Unlink();
            IsDisposed = true;
        }

        protected virtual void SetDeath(Entity? source, bool gibbed)
        {
            if (gibbed)
                SoundManager.CreateSoundOn(this, "misc/gibbed", SoundChannelType.Auto, new SoundParams(this));
            else if (Definition.Properties.DeathSound.Length > 0)
                SoundManager.CreateSoundOn(this, Definition.Properties.DeathSound, SoundChannelType.Auto, new SoundParams(this));

            if (Flags.Missile)
            {
                Flags.Missile = false;
                Velocity = Vec3D.Zero;
            }
            else
            {
                Flags.Corpse = true;
                Flags.Skullfly = false;
                Flags.Solid = false;
                Flags.Shootable = false;
                if (!Flags.DontFall)
                    Flags.NoGravity = false;
            }

            World.HandleEntityDeath(this, source);
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
            if (Flags.Boss && (soundInfo.Name == Definition.Properties.SeeSound || soundInfo.Name == Definition.Properties.DeathSound))
                return false;

            return true;
        }
    }
}