using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Models;
using Helion.Util;
using Helion.Util.Container;
using Helion.World.Entities;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.World.Special.Specials;
using Helion.Maps.Specials;
using Helion.Util.Configs.Components;
using Helion.World.Static;
using Helion.Geometry.Boxes;
using Helion.World.Bsp;
using Helion.World.Geometry.Islands;
using static Helion.World.Entities.EntityManager;
using Helion.Graphics.Palettes;
using Helion.Maps.Doom.Components;

namespace Helion.World.Geometry.Sectors;

public class Sector
{
    public static readonly Sector Default = CreateDefault();

    public const int NoTag = 0;

    public int Id;
    public readonly int Tag;
    public readonly SectorPlane Floor;
    public readonly SectorPlane Ceiling;
    public readonly List<Line> Lines = new();
    public readonly LinkableList<Entity> Entities = new();
    public readonly List<BspSubsector> Subsectors = new();
    public List<LinkableNode<Sector>> BlockmapNodes = new();
    public Island Island = null!;

    public short LightLevel { get; set; }
    public TransferHeights? TransferHeights;
    public SectorMoveSpecial? ActiveFloorMove;
    public SectorMoveSpecial? ActiveCeilingMove;
    public int RenderGametick;
    public int ChangeGametick;
    public int BlockmapCount;
    public SectorPlaneFace LastActivePlaneMove;
    public ZDoomSectorSpecialType SectorSpecialType { get; private set; }
    public bool Secret => (SectorEffect & SectorEffect.Secret) != 0;
    public int DamageAmount { get; private set; }
    public int? SkyTextureHandle { get; private set; }
    public bool FlipSkyTexture { get; set; }
    public bool IsFloorStatic => Floor.Dynamic == SectorDynamic.None;
    public bool IsCeilingStatic => Ceiling.Dynamic == SectorDynamic.None;
    public bool AreFlatsStatic => IsFloorStatic && IsCeilingStatic;

    public bool IsMoving => ActiveFloorMove != null || ActiveCeilingMove != null;
    public SectorDataTypes DataChanges;
    public bool DataChanged => DataChanges > 0;

    public int RenderLightChangeGametick;
    public int LastRenderGametick;
    public int SoundValidationCount;
    public int SoundBlock;
    public int CheckCount;
    public bool MarkAutomap;
    public bool Flood;
    public int ActivatedByLineId = -1;
    public WeakEntity SoundTarget { get; private set; } = WeakEntity.Default;
    public InstantKillEffect KillEffect { get; private set; }
    public SectorEffect SectorEffect { get; private set; }

    public double Friction = Constants.DefaultFriction;

    public Sector TransferFloorLightSector { get; set; }
    public Sector TransferCeilingLightSector { get; set; }

    private Box2D? m_boundingBox;

    public Sector(int id, int tag, short lightLevel, SectorPlane floor, SectorPlane ceiling,
        ZDoomSectorSpecialType sectorSpecial, SectorData sectorData)
    {
        Id = id;
        Tag = tag;
        LightLevel = lightLevel;
        Floor = floor;
        Ceiling = ceiling;
        SectorSpecialType = sectorSpecial;
        DamageAmount = sectorData.DamageAmount;
        KillEffect = sectorData.InstantKillEffect;
        SectorEffect = sectorData.SectorEffect;

        floor.Sector = this;
        ceiling.Sector = this;
        TransferFloorLightSector = this;
        TransferCeilingLightSector = this;
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

        return TransferHeights.GetRenderSector(TransferHeights.GetView(sector, viewZ));
    }

    public Sector GetRenderSector(TransferHeightView view)
    {
        if (TransferHeights == null)
            return this;

        return TransferHeights.GetRenderSector(view);
    }

    public bool LightingChanged() => LightingChanged(LastRenderGametick);

    public bool LightingChanged(int gametick)
    {
        if (RenderLightChangeGametick >= gametick - 1)
            return true;

        if (TransferFloorLightSector.Id != Id && TransferFloorLightSector.RenderLightChangeGametick >= gametick - 1)
            return true;

        if (TransferCeilingLightSector.Id != Id && TransferCeilingLightSector.RenderLightChangeGametick >= gametick - 1)
            return true;

        if (TransferHeights != null && TransferHeights.ParentSector.Id != TransferHeights.ControlSector.Id && TransferHeights.ControlSector.LightingChanged(gametick))
            return true;

        return false;
    }

    public enum RenderChangeOptions
    {
        None,
        TransferHeightsOverride
    }

    public bool CheckRenderingChanged(int gametick, RenderChangeOptions options = RenderChangeOptions.TransferHeightsOverride)
    {
        if (Floor.LastRenderChangeGametick >= gametick - 1 || Floor.PrevZ != Floor.Z)
            return true;

        if (Ceiling.LastRenderChangeGametick >= gametick - 1 || Ceiling.PrevZ != Ceiling.Z)
            return true;

        if (TransferHeights != null && (options & RenderChangeOptions.TransferHeightsOverride) != 0)
            return (TransferHeights.ControlSector.DataChanges & SectorDataTypes.FloorZ) != 0 || (TransferHeights.ControlSector.DataChanges & SectorDataTypes.CeilingZ) != 0;

        return false;
    }

    public short FloorRenderLightLevel => TransferFloorLightSector.Floor.LightLevel;
    public short CeilingRenderLightLevel => TransferCeilingLightSector.Ceiling.LightLevel;

    public void SetFriction(double friction)
    {
        DataChanges |= SectorDataTypes.Friction;
        Friction = friction;
    }

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
        if (set == Secret)
            return;

        if (set)
            SectorEffect |= SectorEffect.Secret;
        else
            SectorEffect &= ~SectorEffect.Secret;
        DataChanges |= SectorDataTypes.SectorEffect;
    }

    public void SetSectorEffect(SectorEffect effect)
    {
        if (SectorEffect == effect)
            return;

        SectorEffect = effect;
        DataChanges |= SectorDataTypes.SectorEffect;
    }

    public void SetKillEffect(InstantKillEffect effect)
    {
        if (KillEffect == effect)
            return;

        KillEffect = effect;
        DataChanges |= SectorDataTypes.KillEffect;
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

    public void SetTransferHeights(Sector controlSector, Colormap? upper, Colormap? middle, Colormap? lower)
    {
        TransferHeights = new TransferHeights(this, controlSector, upper, middle, lower);
        DataChanges |= SectorDataTypes.TransferHeights;
    }

    public SectorModel ToSectorModel(IWorld world)
    {
        SectorModel sectorModel = new()
        {
            Id = Id,
            SoundValidationCount = SoundValidationCount,
            SoundBlock = SoundBlock,
            SoundTarget = SoundTarget.Entity?.Id,
            SectorSpecialType = (int)SectorSpecialType,
            SectorDataChanges = (int)DataChanges,
            SkyTexture = SkyTextureHandle,
            TransferFloorLight = TransferFloorLightSector?.Id,
            TransferCeilingLight = TransferCeilingLightSector?.Id,
            TransferHeights = TransferHeights?.ControlSector.Id,
            TransferHeightsColormapUpper = TransferHeights?.UpperColormap?.Entry?.Path.Name,
            TransferHeightsColormapMiddle = TransferHeights?.MiddleColormap?.Entry?.Path.Name,
            TransferHeightsColormapLower = TransferHeights?.LowerColormap?.Entry?.Path.Name,
            SectorEffect = SectorEffect
        };

        if (DataChanged)
        {
            if ((DataChanges & SectorDataTypes.FloorZ) != 0)
                sectorModel.FloorZ = Floor.Z;
            if ((DataChanges & SectorDataTypes.CeilingZ) != 0)
                sectorModel.CeilingZ = Ceiling.Z;
            if ((DataChanges & SectorDataTypes.FloorTexture) != 0)
                sectorModel.FloorTex = world.TextureManager.GetTexture(Floor.TextureHandle).Name;
            if ((DataChanges & SectorDataTypes.CeilingTexture) != 0)
                sectorModel.CeilingTex = world.TextureManager.GetTexture(Ceiling.TextureHandle).Name;
            if ((DataChanges & SectorDataTypes.SectorSpecialType) != 0)
                sectorModel.SectorSpecialType = (int)SectorSpecialType;
            if ((DataChanges & SectorDataTypes.Friction) != 0)
                sectorModel.Friction = Friction;
            if ((DataChanges & SectorDataTypes.Light) != 0)
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

    public void ApplySectorModel(IWorld world, SectorModel sectorModel, WorldModelPopulateResult result)
    {
        IList<Sector> sectors = world.Sectors;
        SoundValidationCount = sectorModel.SoundValidationCount;
        SoundBlock = sectorModel.SoundBlock;
        if (sectorModel.SoundTarget.HasValue && result.Entities.TryGetValue(sectorModel.SoundTarget.Value, out var soundTarget))
            SetSoundTarget(soundTarget.Entity);

        if (sectorModel.SectorDataChanges > 0)
        {
            DataChanges = (SectorDataTypes)sectorModel.SectorDataChanges;

            if ((DataChanges & SectorDataTypes.FloorZ) != 0 && sectorModel.FloorZ.HasValue)
            {
                double amount =  sectorModel.FloorZ.Value - Floor.Z;
                Floor.Z = sectorModel.FloorZ.Value;
                Floor.PrevZ = Floor.Z;
                Floor.Plane.MoveZ(amount);
            }

            if ((DataChanges & SectorDataTypes.CeilingZ) != 0 && sectorModel.CeilingZ.HasValue)
            {
                double amount = sectorModel.CeilingZ.Value - Ceiling.Z;
                Ceiling.Z = sectorModel.CeilingZ.Value;
                Ceiling.PrevZ = Ceiling.Z;
                Ceiling.Plane.MoveZ(amount);
            }

            if ((DataChanges & SectorDataTypes.Light) != 0)
            {
                if (sectorModel.LightLevel.HasValue)
                    SetLightLevel(sectorModel.LightLevel.Value, 0);
                if (sectorModel.FloorLightLevel.HasValue)
                    SetFloorLightLevel(sectorModel.FloorLightLevel.Value, 0);
                if (sectorModel.CeilingLightLevel.HasValue)
                    SetCeilingLightLevel(sectorModel.CeilingLightLevel.Value, 0);
            }

            if ((DataChanges & SectorDataTypes.FloorTexture) != 0)
            {
                if (sectorModel.FloorTex != null)
                    Floor.SetTexture(world.TextureManager.GetTexture(sectorModel.FloorTex, ResourceNamespace.Global).Index, 0);
                else if (sectorModel.FloorTexture.HasValue)
                    Floor.SetTexture(sectorModel.FloorTexture.Value, 0);
            }

            if ((DataChanges & SectorDataTypes.CeilingTexture) != 0)
            {
                if (sectorModel.CeilingTex != null)
                    Ceiling.SetTexture(world.TextureManager.GetTexture(sectorModel.CeilingTex, ResourceNamespace.Global).Index, 0);
                else if (sectorModel.CeilingTexture.HasValue)
                    Ceiling.SetTexture(sectorModel.CeilingTexture.Value, 0);
            }

            if ((DataChanges & SectorDataTypes.SectorSpecialType) != 0 && sectorModel.SectorSpecialType.HasValue)
                SectorSpecialType = (ZDoomSectorSpecialType)sectorModel.SectorSpecialType;

            if ((DataChanges & SectorDataTypes.SkyTexture) != 0 && sectorModel.SkyTexture.HasValue)
                SkyTextureHandle = sectorModel.SkyTexture;

            if ((DataChanges & SectorDataTypes.Friction) != 0 && sectorModel.Friction.HasValue)
                Friction = sectorModel.Friction.Value;

            if (sectorModel.Secret.HasValue)
                SetSecret(sectorModel.Secret.Value);
            if (sectorModel.SectorEffect.HasValue)
                SetSectorEffect(sectorModel.SectorEffect.Value);

            DamageAmount = sectorModel.DamageAmount;
        }

        if (sectorModel.TransferFloorLight.HasValue && IsSectorIdValid(sectors, sectorModel.TransferFloorLight.Value))
            TransferFloorLightSector = sectors[sectorModel.TransferFloorLight.Value];

        if (sectorModel.TransferCeilingLight.HasValue && IsSectorIdValid(sectors, sectorModel.TransferCeilingLight.Value))
            TransferCeilingLightSector = sectors[sectorModel.TransferCeilingLight.Value];

        if (sectorModel.TransferHeights.HasValue && IsSectorIdValid(sectors, sectorModel.TransferHeights.Value))
        {
            var textureManager = world.ArchiveCollection.TextureManager;
            textureManager.TryGetColormap(sectorModel.TransferHeightsColormapUpper, out var upper);
            textureManager.TryGetColormap(sectorModel.TransferHeightsColormapMiddle, out var middle);
            textureManager.TryGetColormap(sectorModel.TransferHeightsColormapLower, out var lower);
            TransferHeights = new TransferHeights(this, sectors[sectorModel.TransferHeights.Value], upper, middle, lower);
        }
    }

    private static bool IsSectorIdValid(IList<Sector> sectors, int id) => id >= 0 && id < sectors.Count;

    public LinkableNode<Entity> Link(Entity entity)
    {
        //Precondition(!Entities.ContainsReference(entity), "Trying to link an entity to a sector twice");

        LinkableNode<Entity> node = WorldStatic.DataCache.GetLinkableNodeEntity(entity);
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
        LastActivePlaneMove = planeType;
        if (planeType == SectorPlaneFace.Floor)
            ActiveFloorMove = special;
        else
            ActiveCeilingMove = special;
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

    public Box2D GetBoundingBox()
    {
        if (m_boundingBox != null)
            return m_boundingBox.Value;

        Vec2D min = new(double.MaxValue, double.MaxValue);
        Vec2D max = new(double.MinValue, double.MinValue);

        for (int i = 0; i < Lines.Count; i++)
        {
            Line line = Lines[i];
            if (line.Segment.Start.X < min.X)
                min.X = line.Segment.Start.X;
            if (line.Segment.Start.X > max.X)
                max.X = line.Segment.Start.X;

            if (line.Segment.Start.Y < min.Y)
                min.Y = line.Segment.Start.Y;
            if (line.Segment.Start.Y > max.Y)
                max.Y = line.Segment.Start.Y;

            if (line.Segment.End.X < min.X)
                min.X = line.Segment.End.X;
            if (line.Segment.End.X > max.X)
                max.X = line.Segment.End.X;

            if (line.Segment.End.Y < min.Y)
                min.Y = line.Segment.End.Y;
            if (line.Segment.End.Y > max.Y)
                max.Y = line.Segment.End.Y;
        }

        m_boundingBox = new(min, max);
        return m_boundingBox.Value;
    }

    public override bool Equals(object? obj) => obj is Sector sector && Id == sector.Id;

    public override int GetHashCode() => Id.GetHashCode();

    public void UnlinkFromWorld(IWorld world)
    {
        for (int i = 0; i < BlockmapNodes.Count; i++)
        {
            BlockmapNodes[i].Unlink();
            world.DataCache.FreeLinkableNodeSector(BlockmapNodes[i]);
        }

        BlockmapNodes.Clear();
    }

    public void ApplyTriggerChanges(IWorld world, TriggerChanges changes, SectorPlaneFace planeType, bool transferSpecial)
    {
        if (changes.Texture.HasValue)
            world.SetPlaneTexture(GetSectorPlane(planeType), changes.Texture.Value);
        if (transferSpecial)
        {
            SectorDamageSpecial = changes.DamageSpecial?.Copy(this);
            if (changes.SectorEffect.HasValue)
                SetSectorEffect(changes.SectorEffect.Value);
            if (changes.KillEffect.HasValue)
                SetKillEffect(changes.KillEffect.Value);
        }
    }
}
