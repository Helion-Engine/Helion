using System.Collections.Generic;
using Helion.Audio;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Sound;
using Helion.World.Special;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.EntityManager;

namespace Helion.World.Geometry.Sectors
{
    public class Sector : ISoundSource
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
        public readonly List<Line> Lines = new List<Line>();
        
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
        public ISectorSpecial? ActiveMoveSpecial;
        
        /// <summary>
        /// The special sector type.
        /// </summary>
        public ZDoomSectorSpecialType SectorSpecialType;
        
        public bool IsMoving => ActiveMoveSpecial != null;
        public bool Has3DFloors => !Floors3D.Empty();
        public SectorDataTypes DataChanges;
        public bool DataChanged => DataChanges > 0;
        public bool LightingChanged;

        public int SoundValidationCount;
        public int SoundBlock;
        public Entity? SoundTarget;

        public Sector(int id, int tag, short lightLevel, SectorPlane floor, SectorPlane ceiling,
            ZDoomSectorSpecialType sectorSpecial)
        {
            Id = id;
            Tag = tag;
            LightLevel = lightLevel;
            Floor = floor;
            Ceiling = ceiling;
            SectorSpecialType = sectorSpecial;

            floor.Sector = this;
            ceiling.Sector = this;
        }

        public void SetSectorSpecialType(ZDoomSectorSpecialType type)
        {
            SectorSpecialType = type;
            DataChanges |= SectorDataTypes.SectorSpecialType;
        }

        public void PlaneTextureChange(SectorPlane sectorPlane)
        {
            if (sectorPlane == Floor)
                DataChanges |= SectorDataTypes.FloorTexture;
            else
                DataChanges |= SectorDataTypes.CeilingTexture;
        }

        public SectorModel ToSectorModel()
        {
            SectorModel sectorModel = new SectorModel()
            {
                Id = Id,
                SoundValidationCount = SoundValidationCount,
                SoundBlock = SoundBlock,
                SoundTarget = SoundTarget?.Id,
                SectorDataChanges = (int)DataChanges
            };

            if (DataChanged)
            {
                if (DataChanges.HasFlag(SectorDataTypes.FloorZ))
                    sectorModel.FloorZ = Floor.Z;
                if (DataChanges.HasFlag(SectorDataTypes.CeilingZ))
                    sectorModel.CeilingZ = Ceiling.Z;
                if (DataChanges.HasFlag(SectorDataTypes.Light))
                    sectorModel.LightLevel = LightLevel;
                if (DataChanges.HasFlag(SectorDataTypes.FloorTexture))
                    sectorModel.FloorTexture = Floor.TextureHandle;
                if (DataChanges.HasFlag(SectorDataTypes.CeilingTexture))
                    sectorModel.CeilingTexture = Ceiling.TextureHandle;
                if (DataChanges.HasFlag(SectorDataTypes.SectorSpecialType))
                    sectorModel.SectorSpecialType = (int)SectorSpecialType;
            }

            return sectorModel;
        }

        public void ApplySectorModel(SectorModel sectorModel, WorldModelPopulateResult result)
        {
            SoundValidationCount = sectorModel.SoundValidationCount;
            SoundBlock = sectorModel.SoundBlock;
            if (sectorModel.SoundTarget.HasValue)
                result.Entities.TryGetValue(sectorModel.SoundTarget.Value, out SoundTarget);

            if (sectorModel.SectorDataChanges > 0)
            {
                DataChanges = (SectorDataTypes)sectorModel.SectorDataChanges;

                if (DataChanges.HasFlag(SectorDataTypes.FloorZ) && sectorModel.FloorZ.HasValue)
                {
                    double amount =  sectorModel.FloorZ.Value - Floor.Z;
                    Floor.Z = sectorModel.FloorZ.Value;
                    Floor.PrevZ = Floor.Z;
                    Floor.Plane.MoveZ(amount);
                }

                if (DataChanges.HasFlag(SectorDataTypes.CeilingZ) && sectorModel.CeilingZ.HasValue)
                {
                    double amount = sectorModel.CeilingZ.Value - Ceiling.Z;
                    Ceiling.Z = sectorModel.CeilingZ.Value;
                    Ceiling.PrevZ = Ceiling.Z;
                    Ceiling.Plane.MoveZ(amount);
                }

                if (DataChanges.HasFlag(SectorDataTypes.Light) && sectorModel.LightLevel.HasValue)
                    SetLightLevel(sectorModel.LightLevel.Value);

                if (DataChanges.HasFlag(SectorDataTypes.FloorTexture) && sectorModel.FloorTexture.HasValue)
                    Floor.SetTexture(sectorModel.FloorTexture.Value);

                if (DataChanges.HasFlag(SectorDataTypes.CeilingTexture) && sectorModel.CeilingTexture.HasValue)
                    Floor.SetTexture(sectorModel.CeilingTexture.Value);

                if (DataChanges.HasFlag(SectorDataTypes.SectorSpecialType) && sectorModel.SectorSpecialType.HasValue)
                    SectorSpecialType = (ZDoomSectorSpecialType)sectorModel.SectorSpecialType;
            }
        }
        
        public LinkableNode<Entity> Link(Entity entity)
        {
            Precondition(!Entities.Contains(entity), "Trying to link an entity to a sector twice");

            LinkableNode<Entity> node = DataCache.Instance.GetLinkableNodeEntity(entity);
            Entities.Add(node);
            return node;
        }

        public void SetLightLevel(short lightLevel)
        {
            DataChanges |= SectorDataTypes.Light;
            LightLevel = lightLevel;
            Floor.LightLevel = lightLevel;
            Ceiling.LightLevel = lightLevel;
        }

        public double ToFloorZ(in Vec2D position) => Floor.Plane?.ToZ(position) ?? Floor.Z;
        public double ToFloorZ(in Vec3D position) => Floor.Plane?.ToZ(position) ?? Floor.Z;
        public double ToCeilingZ(in Vec2D position) => Ceiling.Plane?.ToZ(position) ?? Ceiling.Z;
        public double ToCeilingZ(in Vec3D position) => Ceiling.Plane?.ToZ(position) ?? Ceiling.Z;

        // TODO implement when slopes exist
        public double LowestPoint(SectorPlane plane, Line line) => plane.Z;
        public double HighestPoint(SectorPlane plane, Line line) => plane.Z;

        public SectorDamageSpecial? SectorDamageSpecial { get; set; }

        // TODO use plane values - can probably make these neater and more concise, they are all basically the same function
        // TODO add function to handle WR_RaiseByShortestLowerTexture
        public Sector? GetLowestAdjacentFloor()
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

        public Sector? GetLowestAdjacentCeiling(bool includeThis)
        {
            double lowestZ = double.MaxValue;
            Sector? lowestSector = null;

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
                if ((includeThis || line.Front.Sector != this) && line.Front.Sector.Ceiling.Z < lowestZ)
                {
                    lowestSector = line.Front.Sector;
                    lowestZ = lowestSector.Ceiling.Z;
                }

                if (line.Back != null && (includeThis || line.Back.Sector != this) && line.Back.Sector.Ceiling.Z < lowestZ)
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
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

            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
                if (line.Front.Sector != this && line.Front.Sector.LightLevel > max)
                    max = line.Front.Sector.LightLevel;
                if (line.Back != null && line.Back.Sector != this && line.Back.Sector.LightLevel > max)
                    max = line.Back.Sector.LightLevel;
            }

            return max;
        }

        public double GetShortestLower(TextureManager textureManager)
        {
            double min = double.MaxValue;

            // Doom didn't check if there was actually a lower texture set
            // It would use index zero which was AASHITTY with a 64 height causing it to max out at 64
            // TODO compatibility option
            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
                if (line.TwoSided)
                {
                    TwoSided twoSided = (TwoSided)line.Front;
                    var texture = textureManager.GetNullCompatibilityTexture(twoSided.Lower.TextureHandle);
                    if (texture.Image != null && texture.Image.Height < min)
                        min = texture.Image.Height;

                    if (line.Back != null)
                    {
                        twoSided = (TwoSided)line.Back;
                        texture = textureManager.GetNullCompatibilityTexture(twoSided.Lower.TextureHandle);
                        if (texture.Image != null && texture.Image.Height < min)
                            min = texture.Image.Height;
                    }
                }
            }

            return min;
        }

        public Vec3D GetSoundSource(Entity listener, SectorPlaneType type)
        {
            Vec2D pos2D = listener.Position.XY;
            if (listener.Sector.Equals(this))
                return pos2D.To3D(type == SectorPlaneType.Floor ? ToFloorZ(pos2D) : ToCeilingZ(pos2D));

            double z = listener.Position.Z;     
            pos2D = GetClosestPointFrom(pos2D);

            // Check if the player z is in line with the lower/upper of the moving sector
            // This is set to the player z so the sound doesn't attenuate on z axis
            if (type == SectorPlaneType.Floor)
            {
                double floorZ = ToFloorZ(pos2D);
                if (floorZ < z)
                    z = floorZ;
            }
            else
            {
                double ceilingZ = ToCeilingZ(pos2D);
                if (ceilingZ > z)
                    z = ceilingZ;
            }

            return new Vec3D(pos2D.X, pos2D.Y, z);
        }

        public Vec2D GetClosestPointFrom(in Vec2D point)
        {
            double minDist = double.MaxValue;
            Line? minLine = null;

            foreach (var line in Lines)
            {
                double dist = line.Segment.ClosestPoint(point).Distance(point);
                if (dist < minDist)
                {
                    minDist = dist;
                    minLine = line;
                }
            }

            if (minLine != null)
                return minLine.Segment.ClosestPoint(point);

            return Vec2D.Zero;
        }

        public override bool Equals(object? obj) => obj is Sector sector && Id == sector.Id;

        public override int GetHashCode() => Id.GetHashCode();

        private IAudioSource? m_audio;

        public void SoundCreated(IAudioSource audioSource, SoundChannelType channel)
        {
            m_audio = audioSource;
        }

        public IAudioSource? TryClearSound(string sound, SoundChannelType channel)
        {
            if (m_audio != null && m_audio.AudioData.SoundInfo.Name == sound)
            {
                IAudioSource stopSource = m_audio;
                m_audio = null;
                return stopSource;
            }

            return null;
        }

        public void ClearSound(IAudioSource audioSource, SoundChannelType channel)
        {
            m_audio = null;
        }

        public double GetDistanceFrom(Entity listenerEntity)
        {
            if (ActiveMoveSpecial is SectorMoveSpecial moveSpecial)
                return GetSoundSource(listenerEntity, moveSpecial.MoveData.SectorMoveType).Distance(listenerEntity.Position);

            return 0.0;
        }

        public Vec3D? GetSoundPosition(Entity listenerEntity)
        {
            if (ActiveMoveSpecial is SectorMoveSpecial moveSpecial)
                return GetSoundSource(listenerEntity, moveSpecial.MoveData.SectorMoveType);

            return null;
        }

        public Vec3D? GetSoundVelocity()
        {
            return Vec3D.Zero;
        }

        public bool CanAttenuate(SoundInfo soundInfo) => true;
    }
}