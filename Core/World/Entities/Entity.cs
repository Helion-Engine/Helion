using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Resources.Definitions.Decorate.States;
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
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities
{
    // TODO: When the optimized renderer becomes a thing, we can remove subsector stuff (perf boost!)
    
    /// <summary>
    /// An actor in a world.
    /// </summary>
    public class Entity : IDisposable, ITickable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly int Id;
        public readonly int ThingId;
        public readonly EntityDefinition Definition;
        public readonly EntitySoundChannels SoundChannels;
        public readonly EntityManager EntityManager;
        public double AngleRadians;
        public EntityBox Box;
        public Vec3D PrevPosition;
        public Vec3D Position => Box.Position;
        public Vec3D AttackPosition => new Vec3D(Position.X, Position.Y, Position.Z + (Height / 2) + 8);
        public Vec3D Velocity = Vec3D.Zero;
        public Inventory Inventory = new Inventory();
        public int Health;
        public int FrozenTics;
        public bool IsFlying;
        public bool NoClip;
        public bool OnGround;
        public Sector Sector;
        public Sector HighestFloorSector;
        public Sector LowestCeilingSector;
        public double LowestCeilingZ;
        public double HighestFloorZ;
        public List<Line> IntersectSpecialLines = new List<Line>();
        public List<Entity> IntersectEntities = new List<Entity>();
        public List<Sector> IntersectSectors = new List<Sector>();
        public List<Subsector> IntersectSubsectors = new List<Subsector>();
        public Entity? OnEntity;
        public Entity? Owner;
        public Entity? BlockingEntity;
        public bool Refire;
        protected internal LinkableNode<Entity> EntityListNode = new LinkableNode<Entity>();
        protected internal List<LinkableNode<Entity>> BlockmapNodes = new List<LinkableNode<Entity>>();
        protected internal List<LinkableNode<Entity>> SectorNodes = new List<LinkableNode<Entity>>();
        protected internal List<LinkableNode<Entity>> SubsectorNodes = new List<LinkableNode<Entity>>();
        protected readonly SoundManager SoundManager;
        protected int FrameIndex;
        protected int TicksInFrame;

        // Temporary storage variable for handling PhysicsManager.SectorMoveZ
        public double SaveZ;

        public double Height;
        public double Radius => Definition.Properties.Radius;
        public bool IsFrozen => FrozenTics > 0;
        public EntityFlags Flags => Definition.Flags;
        public EntityProperties Properties => Definition.Properties;
        public EntityFrame Frame => Definition.States.Frames[FrameIndex];
        
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
        public Entity(int id, int thingId, EntityDefinition definition, Vec3D position, double angleRadians, 
            Sector sector, EntityManager entityManager, SoundManager soundManager)
        {
            Health = definition.Properties.Health;
            Height = definition.Properties.Height;

            Id = id;
            ThingId = thingId;
            Definition = definition;
            AngleRadians = angleRadians;
            Box = new EntityBox(position, Radius, Height);
            PrevPosition = Box.Position;
            Sector = sector;
            LowestCeilingZ = sector.Ceiling.Z;
            HighestFloorZ = sector.Floor.Z;
            HighestFloorSector = sector;
            LowestCeilingSector = sector;
            OnGround = CheckOnGround();
            EntityManager = entityManager;
            SoundManager = soundManager;
            SoundChannels = new EntitySoundChannels(this);

            FindInitialFrameIndex();
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

            // Need to remove this from other intersect entities as they will not remove us unless they move
            for (int i = 0; i < IntersectEntities.Count; i++)
                IntersectEntities[i].IntersectEntities.Remove(this);
            
            IntersectSpecialLines.Clear();
            IntersectEntities.Clear();
            IntersectSectors.Clear();
            IntersectSubsectors.Clear();

            BlockingEntity = null;
        }

        /// <summary>
        /// Runs any tickable logic on the entity.
        /// </summary>
        public virtual void Tick()
        {
            if (FrozenTics > 0)
                FrozenTics--;

            TickStateFrame();
            SoundChannels.Tick();

            RunDebugSanityChecks();
        }

        private void FindInitialFrameIndex()
        {
            // Every actor must have at least one frame, so if we can't find
            // the spawn frame somehow, we'll assume we start at index zero.
            if (!SetStateToLabel("SPAWN"))
                FrameIndex = 0;
        }

        public void SetDeathState()
        {
            if (SetStateToLabel("DEATH"))
                SetDeath();
        }

        public void SetXDeathState()
        {
            if (SetStateToLabel("XDEATH"))
                SetDeath();
        }

        public void SetPainState()
        {
            SetStateToLabel("PAIN");
        }

        public bool HasXDeathState() => Definition.States.Labels.ContainsKey("XDEATH");
        
        public bool SetStateToLabel(string label)
        {
            if (Definition.States.Labels.TryGetValue(label, out int index))
            {
                TicksInFrame = 0;
                FrameIndex = index;
                return true;
            }
                
            Log.Warn("Unable to find state label '{0}' for actor {1}", label, Definition.Name);
            return false;
        }
        
        public bool IsCrushing() => LowestCeilingZ - HighestFloorZ < Height;

        public bool CheckOnGround() => HighestFloorZ >= Position.Z;

        public void Dispose()
        {
            UnlinkFromWorld();
            EntityListNode.Unlink();
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
            }
        }

        private void TickStateFrame()
        {
            Precondition(FrameIndex >= 0 && FrameIndex < Definition.States.Frames.Count, "Out of range frame index for entity");
            
            EntityFrame frame = Definition.States.Frames[FrameIndex];
            
            // TODO: If frame.Ticks == 0, we need to loop and keep consuming frames.
            if (TicksInFrame == 0)
                frame.ActionFunction?.Invoke(this);
            
            TicksInFrame++;
            if (TicksInFrame > frame.Ticks)
            {
                if (frame.BranchType == ActorStateBranch.Stop && frame.Ticks >= 0)
                {
                    EntityManager.Destroy(this);
                    return;
                }

                FrameIndex = frame.NextFrameIndex;
                TicksInFrame = 0;
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