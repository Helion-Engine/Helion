using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Util;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using Helion.World.Physics;
using Helion.World.Sound;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities
{
    /// <summary>
    /// An actor in a world.
    /// </summary>
    public class Entity : IDisposable, ITickable
    {
        public readonly int Id;
        public readonly int ThingId;
        public readonly EntityDefinition Definition;
        public readonly EntityFlags Flags;
        public readonly EntityProperties Properties;
        public readonly EntitySoundChannels SoundChannels;
        public readonly EntityManager EntityManager;
        public readonly FrameState FrameState;
        public readonly IWorld World;
        public double AngleRadians;
        public EntityBox Box;
        public Vec3D PrevPosition;
        public Vec3D Position => Box.Position;
        public Vec3D AttackPosition => new Vec3D(Position.X, Position.Y, Position.Z + (Height / 2) + 8);
        public Vec3D Velocity = Vec3D.Zero;
        public Inventory Inventory = new Inventory();
        public int Health;
        public int FrozenTics;
        public bool NoClip;
        public bool OnGround;
        public Sector Sector;
        public Sector HighestFloorSector;
        public Sector LowestCeilingSector;
        // Can be Sector or Entity
        public object HighestFloorObject;
        public object LowestCeilingObject;
        public double LowestCeilingZ;
        public double HighestFloorZ;
        public List<Line> IntersectSpecialLines = new List<Line>();
        public List<Sector> IntersectSectors = new List<Sector>();
        public List<Subsector> IntersectSubsectors = new List<Subsector>();
        // The entity we are standing on
        public Entity? OnEntity;
        // The entity standing on our head
        public Entity? OverEntity;
        public Entity? Owner;
        public bool Refire;
        protected internal LinkableNode<Entity> EntityListNode = new LinkableNode<Entity>();
        protected internal List<LinkableNode<Entity>> BlockmapNodes = new List<LinkableNode<Entity>>();
        protected internal List<LinkableNode<Entity>> SectorNodes = new List<LinkableNode<Entity>>();
        protected internal List<LinkableNode<Entity>> SubsectorNodes = new List<LinkableNode<Entity>>();
        protected readonly SoundManager SoundManager;
        protected int FrameIndex;
        protected int TicksInFrame;
        internal bool IsDisposed { get; private set; }

        // Temporary storage variable for handling PhysicsManager.SectorMoveZ
        public double SaveZ;

        public double Height => Box.Height;
        public double Radius => Definition.Properties.Radius;
        public bool IsFrozen => FrozenTics > 0;
        public EntityFrame Frame => FrameState.Frame;
        
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
            Sector sector, EntityManager entityManager, SoundManager soundManager, IWorld world)
        {
            Health = definition.Properties.Health;

            Id = id;
            ThingId = thingId;
            Definition = definition;
            Flags = new EntityFlags(definition.Flags);
            Properties = new EntityProperties(definition.Properties);
            FrameState = new FrameState(this, definition, entityManager);
            World = world;
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
            OnGround = CheckOnGround();
            EntityManager = entityManager;
            SoundManager = soundManager;
            SoundChannels = new EntitySoundChannels(this);

            FrameState.SetState(FrameStateLabel.Spawn);
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
                SectorNodes[i].Unlink();
            SectorNodes.Clear();
            
            for (int i = 0; i < SubsectorNodes.Count; i++)
                SubsectorNodes[i].Unlink();
            SubsectorNodes.Clear();
            
            for (int i = 0; i < BlockmapNodes.Count; i++)
                BlockmapNodes[i].Unlink();
            BlockmapNodes.Clear();

            IntersectSpecialLines.Clear();
            IntersectSectors.Clear();
            IntersectSubsectors.Clear();
        }

        /// <summary>
        /// Runs any tickable logic on the entity.
        /// </summary>
        public virtual void Tick()
        {
            if (FrozenTics > 0)
                FrozenTics--;

            FrameState.Tick();
            SoundChannels.Tick();

            RunDebugSanityChecks();
        }

        public void Kill()
        {
            if (Health < -Properties.Health && HasXDeathState())
                SetXDeathState();
            else
                SetDeathState();

            Health = 0;
            SetHeight(Definition.Properties.Height / 4.0);
            // TODO: Player override this and handle its own m_viewHeight?
        }
        
        public void SetDeathState()
        {
            if (FrameState.SetState(FrameStateLabel.Death))
                SetDeath();
        }

        public void SetXDeathState()
        {
            if (FrameState.SetState(FrameStateLabel.XDeath))
                SetDeath();
        }

        public void Damage(int damage, bool setPainState)
        {
            if (damage <= 0)
                return;
            
            Health -= damage;

            if (Health <= 0)
                Kill();
            else if (setPainState && Definition.States.Labels.ContainsKey("PAIN"))
                FrameState.SetState(FrameStateLabel.Pain);
        }

        public virtual void GivePickedUpItem(Entity item)
        {
            Inventory.Add(item.Definition, item.Properties.Inventory.Amount);
        }

        public bool HasXDeathState() => Definition.States.Labels.ContainsKey("XDEATH");

        public bool IsCrushing() => LowestCeilingZ - HighestFloorZ < Height;

        public bool CheckOnGround() => HighestFloorZ >= Position.Z;

        /// <summary>
        /// Returns a list of all entities that are able to block this entity (using CanBlockEntity) in a 2D space.
        /// </summary>
        public List<Entity> GetIntersectingEntities2D()
        {
            List<Entity> entities = new List<Entity>();

            foreach (var entity in Sector.Entities)
            {
                if (CanBlockEntity(entity) && entity.Box.Overlaps2D(Box))
                    entities.Add(entity);
            }

            return entities;
        }

        public bool CanBlockEntity(Entity other)
        {
            if (ReferenceEquals(this, other) || Owner == other || !other.Flags.Solid)
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
            {
                if (Flags.IsMonster)
                    return Health == 0;

                return false;
            }

            return !OnGround;
        }

        public bool ShouldApplyFriction()
        {
            if (Flags.NoFriction || Flags.Missile)
                return false;

            return Flags.NoGravity || OnGround;
        }

        public void Dispose()
        {
            UnlinkFromWorld();
            EntityListNode.Unlink();
            IsDisposed = true;
        }

        private void SetDeath()
        {
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
                Flags.NoGravity = false;
            }
        }

        [Conditional("DEBUG")]
        private void RunDebugSanityChecks()
        {
            if (Position.Z < PhysicsManager.LowestPossibleZ)
                Fail($"Entity #{Id} ({Definition.Name}) has fallen too far, did you forget +NOGRAVITY with something like +NOSECTOR/+NOBLOCKMAP?");
        }
    }
}