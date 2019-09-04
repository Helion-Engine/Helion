using System.Collections.Generic;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Geometry.Sides;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Sectors
{
    public class Sector
    {
        /// <summary>
        /// A unique identifier for this element.
        /// </summary>
        public readonly int Id;
        
        /// <summary>
        /// The index in the map file it was loaded from.
        /// </summary>
        public readonly int MapId;
        
        /// <summary>
        /// The tag that other actions will use to reference this sector by.
        /// </summary>
        public readonly int Tag;
        
        /// <summary>
        /// A list of all the sides that reference this sector.
        /// </summary>
        public readonly List<Side> Sides = new List<Side>();
        
        /// <summary>
        /// The floor plane of this sector.
        /// </summary>
        public readonly SectorPlane Floor;
        
        /// <summary>
        /// The ceiling plane of this sector.
        /// </summary>
        public readonly SectorPlane Ceiling;
        
        /// <summary>
        /// All the 3D floors that exist for this sector.
        /// </summary>
        public readonly List<Sector3DFloor> Floors3D = new List<Sector3DFloor>();
        
        /// <summary>
        /// A list of all the entities that linked themselves into this sector.
        /// </summary>
        public readonly LinkableList<Entity> Entities = new LinkableList<Entity>();
        
        /// <summary>
        /// The light level of the sector. This is usually between 0 - 255, but
        /// can be outside the range.
        /// </summary>
        public short LightLevel { get; private set; }
        
        /// <summary>
        /// The transfer heights applied to this sector, or null if none are.
        /// </summary>
        public TransferHeights? TransferHeights;

        public bool Has3DFloors => !Floors3D.Empty();

        public Sector(int id, int mapId, int tag, short lightLevel, SectorPlane floor, SectorPlane ceiling)
        {
            Precondition(id == mapId, "Sector mismatch from generated ID to map ID");
            
            Id = id;
            MapId = mapId;
            Tag = tag;
            LightLevel = lightLevel;
            Floor = floor;
            Ceiling = ceiling;
        }
        
        public LinkableNode<Entity> Link(Entity entity)
        {
            Precondition(!Entities.Contains(entity), "Trying to link an entity to a sector twice");
            
            return Entities.Add(entity);            
        }

        public void SetLightLevel(short lightLevel)
        {
            LightLevel = lightLevel;
            Floor.LightLevel = lightLevel;
            Ceiling.LightLevel = lightLevel;
        }

        public override bool Equals(object? obj) => obj is Sector sector && Id == sector.Id;

        public override int GetHashCode() => Id.GetHashCode();
    }
}