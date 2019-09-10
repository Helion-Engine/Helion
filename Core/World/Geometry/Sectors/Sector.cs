using System.Collections.Generic;
using Helion.Maps.Specials.ZDoom;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Sectors
{
    public class Sector
    {
        /// <summary>
        /// A value that indicates no tag is present.
        /// </summary>
        public const int NoTag = 0;
        
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
        /// All the lines that this sector may influence in some way.
        /// </summary>
        public readonly HashSet<Line> Lines = new HashSet<Line>();
        
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
        
        /// <summary>
        /// The currently active move special, or null if there's no active
        /// movement happening on this sector.
        /// </summary>
        public ISpecial? ActiveMoveSpecial;
        
        /// <summary>
        /// The special sector type.
        /// </summary>
        public ZDoomSectorSpecialType SectorSpecialType;
        
        public bool IsMoving => ActiveMoveSpecial != null;
        public bool Has3DFloors => !Floors3D.Empty();

        public Sector(int id, int mapId, int tag, short lightLevel, SectorPlane floor, SectorPlane ceiling,
            ZDoomSectorSpecialType sectorSpecial)
        {
            Precondition(id == mapId, "Sector mismatch from generated ID to map ID");
            
            Id = id;
            MapId = mapId;
            Tag = tag;
            LightLevel = lightLevel;
            Floor = floor;
            Ceiling = ceiling;
            SectorSpecialType = sectorSpecial;

            floor.Sector = this;
            ceiling.Sector = this;
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

        public double ToFloorZ(Vec2D position) => Floor.Plane?.ToZ(position) ?? Floor.Z;
        public double ToFloorZ(Vec3D position) => Floor.Plane?.ToZ(position) ?? Floor.Z;
        public double ToCeilingZ(Vec2D position) => Ceiling.Plane?.ToZ(position) ?? Ceiling.Z;
        public double ToCeilingZ(Vec3D position) => Ceiling.Plane?.ToZ(position) ?? Ceiling.Z;
        
        // TODO use plane values - can probably make these neater and more concise, they are all basically the same function
        // TODO add function to handle WR_RaiseByShortestLowerTexture
        public Sector? GetLowestAdjacentFloor()
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            foreach (Line line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Floor.Z < lowestZ)
                {
                    lowestSector = line.Front.Sector;
                    lowestZ = lowestSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.Floor.Z < lowestZ)
                {
                    lowestSector = line.Back.Sector;
                    lowestZ = lowestSector.Floor.Z;
                }
            }

            return lowestSector;
        }

        public Sector? GetHighestAdjacentFloor()
        {
            double highestZ = double.MinValue;
            Sector? highestSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Floor.Z > highestZ)
                {
                    highestSector = line.Front.Sector;
                    highestZ = highestSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.Floor.Z > highestZ)
                {
                    highestSector = line.Back.Sector;
                    highestZ = highestSector.Floor.Z;
                }
            }

            return highestSector;
        }

        public Sector? GetLowestAdjacentCeiling()
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = line.Front.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = line.Back.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }
            }

            return lowestSector;
        }

        public Sector? GetHighestAdjacentCeiling()
        {
            double highestZ = double.MinValue;
            Sector? highestSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Ceiling.Z > highestZ)
                {
                    highestSector = line.Front.Sector;
                    highestZ = highestSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.Ceiling.Z > highestZ)
                {
                    highestSector = line.Back.Sector;
                    highestZ = highestSector.Ceiling.Z;
                }
            }

            return highestSector;
        }

        public Sector? GetNextLowestFloor()
        {
            double currentZ = double.MinValue;
            Sector? currentSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Floor.Z < Floor.Z && line.Front.Sector.Floor.Z > currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != this &&
                    line.Back.Sector.Floor.Z < Floor.Z && line.Back.Sector.Floor.Z > currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Floor.Z;
                }
            }

            return currentSector;
        }

        public Sector? GetNextLowestCeiling()
        {
            double currentZ = double.MinValue;
            Sector? currentSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Ceiling.Z < Ceiling.Z && line.Front.Sector.Ceiling.Z > currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != this &&
                    line.Back.Sector.Ceiling.Z < Ceiling.Z && line.Back.Sector.Ceiling.Z > currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }
            }

            return currentSector;
        }

        public Sector? GetNextHighestFloor()
        {
            double currentZ = double.MaxValue;
            Sector? currentSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Floor.Z > Floor.Z && line.Front.Sector.Floor.Z < currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Floor.Z;
                }

                if (line.Back != null && line.Back.Sector != this &&
                    line.Back.Sector.Floor.Z > Floor.Z && line.Back.Sector.Floor.Z < currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Floor.Z;
                }
            }

            return currentSector;
        }

        public Sector? GetNextHighestCeiling()
        {
            double currentZ = double.MaxValue;
            Sector? currentSector = null;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.Ceiling.Z > Ceiling.Z && line.Front.Sector.Ceiling.Z < currentZ)
                {
                    currentSector = line.Front.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }

                if (line.Back != null && line.Back.Sector != this &&
                    line.Back.Sector.Ceiling.Z > Ceiling.Z && line.Back.Sector.Ceiling.Z < currentZ)
                {
                    currentSector = line.Back.Sector;
                    currentZ = currentSector.Ceiling.Z;
                }
            }

            return currentSector;
        }

        public short GetMinLightLevelNeighbor()
        {
            short min = LightLevel;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.LightLevel < min)
                    min = line.Front.Sector.LightLevel;
                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.LightLevel < min)
                    min = line.Back.Sector.LightLevel;
            }

            return min;
        }

        public short GetMaxLightLevelNeighbor()
        {
            short max = LightLevel;

            foreach (var line in Lines)
            {
                if (line.Front.Sector != this && line.Front.Sector.LightLevel > max)
                    max = line.Front.Sector.LightLevel;
                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.LightLevel > max)
                    max = line.Back.Sector.LightLevel;
            }

            return max;
        }

        public override bool Equals(object? obj) => obj is Sector sector && Id == sector.Id;

        public override int GetHashCode() => Id.GetHashCode();
    }
}