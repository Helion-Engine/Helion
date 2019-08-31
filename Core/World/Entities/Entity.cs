using System;
using System.Collections.Generic;
using Helion.Maps.Geometry;
using Helion.Maps.Geometry.Lines;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using NLog;

namespace Helion.World.Entities
{
    /// <summary>
    /// An actor in a world.
    /// </summary>
    public class Entity : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly int Id;
        public readonly EntityDefinition Definition;
        public double Angle;
        public EntityBox Box;
        public Vec3D PrevPosition;
        public Vec3D Position => Box.Position;
        public Vec3D Velocity = Vec3D.Zero;
        public Player? Player;
        public int FrozenTics;
        public bool IsFlying;
        public bool NoClip;
        public bool OnGround;
        public Sector Sector;
        public Sector LowestCeilingSector;
        public Sector HighestFloorSector;
        public List<Line> IntersectSpecialLines = new List<Line>();
        public List<Entity> IntersectEntities = new List<Entity>();
        protected int FrameIndex;
        protected int TicksInFrame;
        internal LinkableNode<Entity> EntityListNode = new LinkableNode<Entity>();
        internal List<LinkableNode<Entity>> BlockmapNodes = new List<LinkableNode<Entity>>();
        internal List<LinkableNode<Entity>> SectorNodes = new List<LinkableNode<Entity>>();

        public double Height => Definition.Properties.Height;
        public double Radius => Definition.Properties.Radius;
        public bool IsFrozen => FrozenTics > 0;
        public EntityFlags Flags => Definition.Flags;
        public EntityProperties Properties => Definition.Properties;

        /// <summary>
        /// Creates an entity with the following information.
        /// </summary>
        /// <param name="id">A unique ID for this entity.</param>
        /// <param name="definition">The definitions for the entity.</param>
        /// <param name="position">The location in the world.</param>
        /// <param name="angleRadians">The angle in radians.</param>
        /// <param name="sector">The sector that the center of the entity is on.
        /// </param>
        public Entity(int id, EntityDefinition definition, Vec3D position, double angleRadians, Sector sector)
        {
            Id = id;
            Definition = definition;
            Angle = angleRadians;
            Box = new EntityBox(position, Radius, Height);
            PrevPosition = Box.Position;
            Sector = sector;
            LowestCeilingSector = sector;
            HighestFloorSector = sector;
            OnGround = CheckIfOnGround();

            // TODO: Link to sector?
        }

        /// <summary>
        /// Sets the bottom of the entity's center to be at the Z coordinate
        /// provided.
        /// </summary>
        /// <param name="z">The Z coordinate.</param>
        /// <param name="smooth">If the entity should smooth the player's view height. This smooths the camera when stepping up to a higher sector.</param>
        public void SetZ(double z, bool smooth)
        {
            if (Player != null && smooth && Box.Bottom < z)
                Player.SetSmoothZ(z);

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
        /// Resets any interpolation tracking variables.
        /// </summary>
        /// <remarks>
        /// Intended to be used when we have some kind of movement which we do
        /// not want any interpolation with the previous spot being done in the
        /// renderer. An example of this would be going through a teleporter.
        /// </remarks>
        public void ResetInterpolation()
        {
            PrevPosition = Position;
            Player?.ResetInterpolation();
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
            
            for (int i = 0; i < BlockmapNodes.Count; i++)
                BlockmapNodes[i].Unlink();
            
            BlockmapNodes.Clear();
            IntersectSpecialLines.Clear();
            IntersectEntities.Clear();
        }

        /// <summary>
        /// Runs any tickable logic on the entity.
        /// </summary>
        public void Tick()
        {
            PrevPosition = Box.Position;

            if (FrozenTics > 0)
                FrozenTics--;

            TickFrame();
        }

        public void SetStateToLabel(string label)
        {
            if (Definition.States.Labels.TryGetValue(label, out int index))
                FrameIndex = index;
            else
                Log.Warn("Unable to find state label '{0}' for actor {1}", label, Definition.Name);
        }
        
        public bool IsCrushing() => LowestCeilingSector.Ceiling.Z - HighestFloorSector.Floor.Z < Height;

        public void Dispose()
        {
            UnlinkFromWorld();
            EntityListNode.Unlink();
        }

        private bool CheckIfOnGround() => HighestFloorSector.Floor.Plane.ToZ(Position) >= Position.Z;

        private void TickFrame()
        {
            EntityFrame frame = Definition.States.Frames[FrameIndex];
            
            if (TicksInFrame == 0)
                frame.ActionFunction?.Invoke(this);

            // TODO: If frame.Ticks == 0, we need to loop and keep consuming frames.
            
            TicksInFrame++;
            if (TicksInFrame > frame.Ticks)
            {
                // TODO: If flow control is `Stop` and frame.Ticks is -1, remove actor.
                
                // TODO: If running off the edge, remove the actor? What does ZDoom do...?
                // But we should make it so the very last one doesn't run off when making
                // the actor though!
                
                // TODO: Handle `Wait`.

                FrameIndex = frame.NextFrameIndex;
                TicksInFrame = 0;
            }
        }
    }
}