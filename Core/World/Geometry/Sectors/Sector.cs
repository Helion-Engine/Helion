using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.World.Special.Specials;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.EntityManager;
using Helion.Maps.Specials;
using Helion.Util.Configs.Components;

namespace Helion.World.Geometry.Sectors;

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
    public short LightLevel { get; set; }

    /// <summary>
    /// The transfer heights applied to this sector, or null if none are.
    /// </summary>
    public TransferHeights? TransferHeights;

    public SectorMoveSpecial? ActiveFloorMove;
    public SectorMoveSpecial? ActiveCeilingMove;
    public int RenderGametick;
    public int ChangeGametick;

    /// <summary>
    /// The special sector type.
    /// </summary>
    public ZDoomSectorSpecialType SectorSpecialType { get; private set; }
    public bool Secret { get; private set; }
    public int DamageAmount { get; private set; }
    public int? SkyTextureHandle { get; private set; }
    public bool FlipSkyTexture { get; set; }

    public bool IsMoving => ActiveFloorMove != null || ActiveCeilingMove != null;
    public bool Has3DFloors => !Floors3D.Empty();
    public SectorDataTypes DataChanges;
    public bool DataChanged => DataChanges > 0;

    public int RenderLightChangeGametick;
    public int LastRenderGametick;
    public int SoundValidationCount;
    public int SoundBlock;
    public WeakEntity SoundTarget { get; private set; } = WeakEntity.Default;
    public readonly InstantKillEffect InstantKillEffect;
    public readonly SectorEffect SectorEffect;

    public double Friction = Constants.DefaultFriction;

    private Sector m_transferFloorLightSector;
    private Sector m_transferCeilingLightSector;

    public Sector(int id, int tag, short lightLevel, SectorPlane floor, SectorPlane ceiling,
        ZDoomSectorSpecialType sectorSpecial, SectorData sectorData)
    {
        Id = id;
        Tag = tag;
        LightLevel = lightLevel;
        Floor = floor;
        Ceiling = ceiling;
        SectorSpecialType = sectorSpecial;
        Secret = sectorData.Secret;
        DamageAmount = sectorData.DamageAmount;
        InstantKillEffect = sectorData.InstantKillEffect;
        SectorEffect = sectorData.SectorEffect;

        floor.Sector = this;
        ceiling.Sector = this;
        m_transferFloorLightSector = this;
        m_transferCeilingLightSector = this;
    }

    public static Sector CreateDefault() =>
        new (0, 0, 0,
            new SectorPlane(0, SectorPlaneFace.Floor, 0, 0, 0),
            new SectorPlane(0, SectorPlaneFace.Ceiling, 0, 0, 0),
            ZDoomSectorSpecialType.None, SectorData.Default);

    public Sector GetRenderSector(Sector sector, double viewZ)
    {
        if (TransferHeights == null)
            return this;

        return TransferHeights.GetRenderSector(sector, viewZ);
    }

    public void SetTransferFloorLight(Sector sector, int gametick)
    {
        m_transferFloorLightSector = sector;
        SetFloorLightLevel(Floor.LightLevel, gametick);
    }

    public void SetTransferCeilingLight(Sector sector, int gametick)
    {
        m_transferCeilingLightSector = sector;
        SetCeilingLightLevel(Floor.LightLevel, gametick);
    }

    public bool LightingChanged() => LightingChanged(LastRenderGametick);

    public bool LightingChanged(int gametick)
    {
        if (RenderLightChangeGametick >= gametick - 1)
            return true;

        if (m_transferFloorLightSector.Id != Id && m_transferFloorLightSector.RenderLightChangeGametick >= gametick - 1)
            return true;

        if (m_transferCeilingLightSector.Id != Id && m_transferCeilingLightSector.RenderLightChangeGametick >= gametick - 1)
            return true;

        if (TransferHeights != null && TransferHeights.ParentSector.Id != TransferHeights.ControlSector.Id && TransferHeights.ControlSector.LightingChanged(gametick))
            return true;

        return false;
    }

    public bool CheckRenderingChanged() => CheckRenderingChanged(LastRenderGametick);

    public bool CheckRenderingChanged(int gametick)
    {
        if (Floor.LastRenderChangeGametick >= gametick - 1 || Floor.PrevZ != Floor.Z)
            return true;

        if (Ceiling.LastRenderChangeGametick >= gametick - 1 || Ceiling.PrevZ != Ceiling.Z)
            return true;

        if (TransferHeights != null)
            return TransferHeights.ControlSector.DataChanges.HasFlag(SectorDataTypes.FloorZ) || TransferHeights.ControlSector.DataChanges.HasFlag(SectorDataTypes.CeilingZ);

        return false;
    }

    public short FloorRenderLightLevel => m_transferFloorLightSector.Floor.LightLevel;
    public short CeilingRenderLightLevel => m_transferCeilingLightSector.Ceiling.LightLevel;

    public void SetLightLevel(short lightLevel, int gametick)
    {
        DataChanges |= SectorDataTypes.Light;
        LightLevel = lightLevel;
        Floor.LightLevel = lightLevel;
        Ceiling.LightLevel = lightLevel;
        RenderLightChangeGametick = gametick;
    }

    public void SetFloorLightLevel(short lightLevel, int gametick)
    {
        DataChanges |= SectorDataTypes.Light;
        Floor.LightLevel = lightLevel;
    }

    public void SetCeilingLightLevel(short lightLevel, int gametick)
    {
        DataChanges |= SectorDataTypes.Light;
        Ceiling.LightLevel = lightLevel;
        RenderLightChangeGametick = gametick;
    }

    public void SetSectorSpecialType(ZDoomSectorSpecialType type)
    {
        SectorSpecialType = type;
        DataChanges |= SectorDataTypes.SectorSpecialType;
    }

    public void SetSecret(bool set)
    {
        Secret = set;
        DataChanges |= SectorDataTypes.Secret;
    }

    public void PlaneTextureChange(SectorPlane sectorPlane)
    {
        if (sectorPlane.Facing == SectorPlaneFace.Floor)
            DataChanges |= SectorDataTypes.FloorTexture;
        else
            DataChanges |= SectorDataTypes.CeilingTexture;
    }

    public void SetSkyTexture(int texture, bool flipped, int gametick)
    {
        SkyTextureHandle = texture;
        FlipSkyTexture = flipped;
        DataChanges |= SectorDataTypes.SkyTexture;
        ChangeGametick = gametick;
    }

    public void SetTransferHeights(Sector controlSector)
    {
        TransferHeights = new TransferHeights(this, controlSector);
        DataChanges |= SectorDataTypes.TransferHeights;
    }

    public SectorModel ToSectorModel()
    {
        SectorModel sectorModel = new()
        {
            Id = Id,
            SoundValidationCount = SoundValidationCount,
            SoundBlock = SoundBlock,
            SoundTarget = SoundTarget.Entity?.Id,
            Secret = Secret,
            SectorSpecialType = (int)SectorSpecialType,
            SectorDataChanges = (int)DataChanges,
            SkyTexture = SkyTextureHandle,
            TransferFloorLight = m_transferFloorLightSector?.Id,
            TransferCeilingLight = m_transferCeilingLightSector?.Id,
            TransferHeights = TransferHeights?.ControlSector.Id
        };

        if (DataChanged)
        {
            if (DataChanges.HasFlag(SectorDataTypes.FloorZ))
                sectorModel.FloorZ = Floor.Z;
            if (DataChanges.HasFlag(SectorDataTypes.CeilingZ))
                sectorModel.CeilingZ = Ceiling.Z;
            if (DataChanges.HasFlag(SectorDataTypes.FloorTexture))
                sectorModel.FloorTexture = Floor.TextureHandle;
            if (DataChanges.HasFlag(SectorDataTypes.CeilingTexture))
                sectorModel.CeilingTexture = Ceiling.TextureHandle;
            if (DataChanges.HasFlag(SectorDataTypes.SectorSpecialType))
                sectorModel.SectorSpecialType = (int)SectorSpecialType;
            if (DataChanges.HasFlag(SectorDataTypes.Light))
            {
                sectorModel.LightLevel = LightLevel;
                if (Floor.LightLevel != LightLevel)
                    sectorModel.FloorLightLevel = Floor.LightLevel;
                if (Ceiling.LightLevel != LightLevel)
                    sectorModel.CeilingLightLevel = Ceiling.LightLevel;
            }

            sectorModel.Secret = Secret;
            sectorModel.DamageAmount = DamageAmount;
        }

        return sectorModel;
    }

    public void ApplySectorModel(SectorModel sectorModel, WorldModelPopulateResult result, IList<Sector> sectors)
    {
        SoundValidationCount = sectorModel.SoundValidationCount;
        SoundBlock = sectorModel.SoundBlock;
        if (sectorModel.SoundTarget.HasValue && result.Entities.TryGetValue(sectorModel.SoundTarget.Value, out var soundTarget))
            SetSoundTarget(soundTarget.Entity);

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

            if (DataChanges.HasFlag(SectorDataTypes.Light))
            {
                if (sectorModel.LightLevel.HasValue)
                    SetLightLevel(sectorModel.LightLevel.Value, 0);
                if (sectorModel.FloorLightLevel.HasValue)
                    SetFloorLightLevel(sectorModel.FloorLightLevel.Value, 0);
                if (sectorModel.CeilingLightLevel.HasValue)
                    SetCeilingLightLevel(sectorModel.CeilingLightLevel.Value, 0);
            }

            if (DataChanges.HasFlag(SectorDataTypes.FloorTexture) && sectorModel.FloorTexture.HasValue)
                Floor.SetTexture(sectorModel.FloorTexture.Value, 0);

            if (DataChanges.HasFlag(SectorDataTypes.CeilingTexture) && sectorModel.CeilingTexture.HasValue)
                Ceiling.SetTexture(sectorModel.CeilingTexture.Value, 0);

            if (DataChanges.HasFlag(SectorDataTypes.SectorSpecialType) && sectorModel.SectorSpecialType.HasValue)
                SectorSpecialType = (ZDoomSectorSpecialType)sectorModel.SectorSpecialType;

            if (DataChanges.HasFlag(SectorDataTypes.SkyTexture) && sectorModel.SkyTexture.HasValue)
                SkyTextureHandle = sectorModel.SkyTexture;

            Secret = sectorModel.Secret;
            DamageAmount = sectorModel.DamageAmount;
        }

        if (sectorModel.TransferFloorLight.HasValue && IsSectorIdValid(sectors, sectorModel.TransferFloorLight.Value))
            m_transferFloorLightSector = sectors[sectorModel.TransferFloorLight.Value];

        if (sectorModel.TransferCeilingLight.HasValue && IsSectorIdValid(sectors, sectorModel.TransferCeilingLight.Value))
            m_transferCeilingLightSector = sectors[sectorModel.TransferCeilingLight.Value];

        if (sectorModel.TransferHeights.HasValue && IsSectorIdValid(sectors, sectorModel.TransferHeights.Value))
            TransferHeights = new TransferHeights(this, sectors[sectorModel.TransferHeights.Value]);
    }

    private static bool IsSectorIdValid(IList<Sector> sectors, int id) => id >= 0 && id < sectors.Count;

    public LinkableNode<Entity> Link(Entity entity)
    {
        Precondition(!Entities.ContainsReference(entity), "Trying to link an entity to a sector twice");

        LinkableNode<Entity> node = entity.World.DataCache.GetLinkableNodeEntity(entity);
        Entities.Add(node);
        return node;
    }

    public void SetSoundTarget(Entity? entity) =>
        SoundTarget = WeakEntity.GetReference(entity);

    public double ToFloorZ(in Vec2D position) => Floor.Plane.ToZ(position);
    public double ToFloorZ(in Vec3D position) => Floor.Plane.ToZ(position);
    public double ToCeilingZ(in Vec2D position) => Ceiling.Plane.ToZ(position);
    public double ToCeilingZ(in Vec3D position) => Ceiling.Plane.ToZ(position);

    // TODO implement when slopes exist
    public double LowestPoint(SectorPlane plane, Line line) => plane.Z;
    public double HighestPoint(SectorPlane plane, Line line) => plane.Z;
    public int GetTexture(SectorPlaneFace planeType) => planeType == SectorPlaneFace.Floor ? Floor.TextureHandle : Ceiling.TextureHandle;
    public double GetZ(SectorPlaneFace planeType) => planeType == SectorPlaneFace.Floor ? Floor.Z : Ceiling.Z;
    public SectorPlane GetSectorPlane(SectorPlaneFace planeType) => planeType == SectorPlaneFace.Floor ? Floor : Ceiling;

    /// <summary>
    /// The currently active move special, or null if there's no active
    /// movement happening on this sector for the given SectorPlane.
    /// </summary>
    public ISectorSpecial? GetActiveMoveSpecial(SectorPlane sectorPlane)
    {
        if (!sectorPlane.Sector.Equals(this))
            return null;

        if (sectorPlane.Facing == SectorPlaneFace.Floor)
            return ActiveFloorMove;

        return ActiveCeilingMove;
    }

    public void ClearActiveMoveSpecial()
    {
        ActiveFloorMove = null;
        ActiveCeilingMove = null;
    }

    public void ClearActiveMoveSpecial(SectorPlaneFace planeType)
    {
        if (planeType == SectorPlaneFace.Floor)
            ActiveFloorMove = null;
        else
            ActiveCeilingMove = null;
    }

    public void SetActiveMoveSpecial(SectorPlaneFace planeType, SectorMoveSpecial? special)
    {
        if (planeType == SectorPlaneFace.Floor)
            ActiveFloorMove = special;
        else
            ActiveCeilingMove = special;
    }

    public void SetTexture(SectorPlaneFace planeType, int texture, int gametick)
    {
        if (planeType == SectorPlaneFace.Floor)
            Floor.SetTexture(texture, gametick);
        else
            Ceiling.SetTexture(texture, gametick);
    }

    public SectorDamageSpecial? SectorDamageSpecial { get; set; }

    public Sector? GetLowestAdjacentFloor()
    {
        double lowestZ = Floor.Z;
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
        double thisZ = Floor.Z;
        Sector? currentSector = null;

        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (line.Front.Sector != this && line.Front.Sector.Floor.Z < Floor.Z && line.Front.Sector.Floor.Z > currentZ &&
                line.Front.Sector.Floor.Z < thisZ)
            {
                currentSector = line.Front.Sector;
                currentZ = currentSector.Floor.Z;
            }

            if (line.Back != null && line.Back.Sector != this &&
                line.Back.Sector.Floor.Z < Floor.Z && line.Back.Sector.Floor.Z > currentZ && line.Back.Sector.Floor.Z < thisZ)
            {
                currentSector = line.Back.Sector;
                currentZ = currentSector.Floor.Z;
            }
        }

        return currentSector;
    }

    public Sector? GetNextLowestCeiling()
    {
        double currentZ = int.MinValue;
        double thisZ = Ceiling.Z;
        Sector? currentSector = null;

        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (line.Front.Sector != this && line.Front.Sector.Ceiling.Z < Ceiling.Z && line.Front.Sector.Ceiling.Z > currentZ &&
                line.Front.Sector.Ceiling.Z < thisZ)
            {
                currentSector = line.Front.Sector;
                currentZ = currentSector.Ceiling.Z;
            }

            if (line.Back != null && line.Back.Sector != this &&
                line.Back.Sector.Ceiling.Z < Ceiling.Z && line.Back.Sector.Ceiling.Z > currentZ && line.Back.Sector.Ceiling.Z < thisZ)
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
        double thisZ = Floor.Z;
        Sector? currentSector = null;

        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (line.Front.Sector != this && line.Front.Sector.Floor.Z > Floor.Z && line.Front.Sector.Floor.Z < currentZ &&
                line.Front.Sector.Floor.Z > thisZ)
            {
                currentSector = line.Front.Sector;
                currentZ = currentSector.Floor.Z;
            }

            if (line.Back != null && line.Back.Sector != this &&
                line.Back.Sector.Floor.Z > Floor.Z && line.Back.Sector.Floor.Z < currentZ && line.Back.Sector.Floor.Z > thisZ)
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
        double thisZ = Ceiling.Z;
        Sector? currentSector = null;

        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (line.Front.Sector != this && line.Front.Sector.Ceiling.Z > Ceiling.Z && line.Front.Sector.Ceiling.Z < currentZ &&
                line.Front.Sector.Ceiling.Z > thisZ)
            {
                currentSector = line.Front.Sector;
                currentZ = currentSector.Ceiling.Z;
            }

            if (line.Back != null && line.Back.Sector != this &&
                line.Back.Sector.Ceiling.Z > Ceiling.Z && line.Back.Sector.Ceiling.Z < currentZ && line.Back.Sector.Ceiling.Z > thisZ)
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

    public double GetShortestTexture(TextureManager textureManager, bool byLowerTx, ConfigCompat config)
    {
        double min = double.MaxValue;
        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (line.TwoSided)
            {
                Side side = line.Front;
                min = GetShortestTextureHeight(textureManager, side, byLowerTx, config.VanillaShortestTexture, min);

                if (line.Back != null)
                {
                    side = line.Back;
                    min = GetShortestTextureHeight(textureManager, side, byLowerTx, config.VanillaShortestTexture, min);
                }
            }
        }

        if (min == double.MaxValue)
        {
            var image = textureManager.GetNullCompatibilityTexture(Constants.NoTextureIndex).Image;
            return image == null ? 0 : image.Height;
        }

        return min;
    }

    private static double GetShortestTextureHeight(TextureManager textureManager, Side side, bool byLowerTx,
        bool compat, double currentHeight)
    {
        var wall = byLowerTx ? side.Lower : side.Upper;

        if (wall.TextureHandle == Constants.NoTextureIndex && (!byLowerTx || !compat))
            return currentHeight;

        // Doom didn't check if there was actually a lower texture set
        // It would use index zero which was AASHITTY with a 64 height causing it to max out at 64
        // GetNullCompatibilityTexture emulates this functionality
        var texture = compat ? textureManager.GetNullCompatibilityTexture(wall.TextureHandle) :
            textureManager.GetTexture(wall.TextureHandle);
        if (texture.Image != null && texture.Image.Height < currentHeight)
            return texture.Image.Height;

        return currentHeight;
    }

    public override bool Equals(object? obj) => obj is Sector sector && Id == sector.Id;

    public override int GetHashCode() => Id.GetHashCode();
}
