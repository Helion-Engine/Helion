using Helion.Geometry.Segments;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Udmf;
using Helion.Maps.Udmf.Components;
using Helion.Resources;
using Helion.Util;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using Helion.World.Special.Specials;
using System;

namespace Helion.World.Geometry.Builder;

public class UdmfGeometryBuilder
{
    public static MapGeometry? Create(UdmfMap map, GeometryBuilder builder, TextureManager textureManager,
        CreateBspTreeFunc createBspTree)
    {
        PopulateSectorData(map, builder, textureManager);
        PopulateLineData(map, builder, textureManager);

        var bspTree = createBspTree();
        if (!bspTree.HasValue)
            return null;

        return new(builder, bspTree.Value.BspTree);
    }

    private static void PopulateSectorData(UdmfMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        var needsTranslation = map.UdmfNamespace == UdmfNamespace.Doom;
        foreach (var mapSector in map.Sectors)
        {
            RenderOffsets offsets = default;
            offsets.Offset.X = mapSector.PanningFloorX;
            offsets.LastOffset.X = mapSector.PanningFloorX;
            offsets.Offset.Y = mapSector.PanningFloorY;
            offsets.LastOffset.Y = mapSector.PanningFloorY;
            offsets.Rotate = MathHelper.ToRadians(mapSector.RotationFloor);
            offsets.Scale.X = mapSector.ScaleFloorX;
            offsets.Scale.Y = mapSector.ScaleFloorY;

            var floorPlane = CreateSectorPlane(mapSector, SectorPlaneFace.Floor, textureManager, offsets);
            floorPlane.LightLevelAdd = mapSector.LightFloor;
            floorPlane.LightLevelAbsolute = mapSector.LightFloorAbsolute;

            offsets = default;
            offsets.Offset.X = mapSector.PanningCeilingX;
            offsets.LastOffset.X = mapSector.PanningCeilingX;
            offsets.Offset.Y = mapSector.PanningCeilingY;
            offsets.LastOffset.Y = mapSector.PanningCeilingY;
            offsets.Rotate = MathHelper.ToRadians(mapSector.RotationCeiling);
            offsets.Scale.X = mapSector.ScaleCeilingX;
            offsets.Scale.Y = mapSector.ScaleCeilingY;

            var ceilingPlane = CreateSectorPlane(mapSector, SectorPlaneFace.Ceiling, textureManager, offsets);
            ceilingPlane.LightLevelAdd = mapSector.LightCeiling;
            ceilingPlane.LightLevelAbsolute = mapSector.LightCeilingAbsolute;

            GetSectorSpecial(mapSector, needsTranslation, out var sectorSpecial, out var sectorData);
            sectorData.LightColor = new(mapSector.LightColor);
            sectorData.FogColor = new(mapSector.FadeColor);
            sectorData.FogDensity = mapSector.FogDensity / 510f;

            var sector = new Sector(builder.Sectors.Count, mapSector.Tag, mapSector.LightLevel,
                floorPlane, ceilingPlane, sectorSpecial, sectorData)
            {
                Silent = mapSector.Silent,
                NoAttack = mapSector.NoAttack,
                Gravity = mapSector.Gravity,
                SkyFloor = mapSector.SkyFloor,
                SkyCeiling = mapSector.SkyCeiling,
                DamageInterval = mapSector.DamageInterval == 0 ? SectorDamageSpecial.DefaultDamageInterval : mapSector.DamageInterval,
                MoreTags = mapSector.MoreTags,
                UserProperties = mapSector.UserProperties
            };

            if (mapSector.DamageAmount != 0)
            {
                sector.DamageAmount = mapSector.DamageAmount;
                sector.DamageLeakiness = mapSector.Leakiness;
            }

            builder.Sectors.Add(sector);
        }
    }

    private static void GetSectorSpecial(UdmfSector mapSector, bool needsTranslation, out ZDoomSectorSpecialType sectorSpecial, out SectorData sectorData)
    {
        if (needsTranslation)
        {
            sectorSpecial = VanillaSectorSpecTranslator.Translate(mapSector.Special, out sectorData);
            return;
        }

        sectorSpecial = (ZDoomSectorSpecialType)SectorSpecialData.GetType(mapSector.Special, SectorDataType.ZDoom);
        sectorData = SectorSpecialData.GetSectorData(mapSector.Special, SectorDataType.ZDoom);
    }

    private static SectorPlane CreateSectorPlane(UdmfSector sector, SectorPlaneFace face,
        TextureManager textureManager, in RenderOffsets offsets)
    {
        double z = (face == SectorPlaneFace.Floor ? sector.FloorZ : sector.CeilingZ);
        string texture = (face == SectorPlaneFace.Floor ? sector.FloorTexture : sector.CeilingTexture);
        int handle = textureManager.GetTexture(texture, ResourceNamespace.Global, ResourceNamespace.Flats).Index;
        return new SectorPlane(face, z, handle, sector.LightLevel, offsets);
    }

    private static void PopulateLineData(UdmfMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        int nextSideId = 0;
        var needsTranslation = map.UdmfNamespace == UdmfNamespace.Doom;

        foreach (var mapLine in map.Lines)
        {
            (Side front, Side? back) = CreateSides(mapLine, builder, ref nextSideId, textureManager, needsTranslation);
            var seg = new Seg2D(mapLine.StartPosition, mapLine.EndPosition);
            var flags = new LineFlags(mapLine.Flags);

            var special = GetLineSpecial(needsTranslation, mapLine, ref flags);

            LineSpecial.ValidateActivationFlags(special.LineSpecialType, mapLine.Args, ref flags, map.MapType);
            var line = new Line(mapLine.Id, seg, front, back, flags, special, mapLine.Args, mapLine.RenderStyle)
            {
                LockNumber = mapLine.LockNumber,
                MapLineId = mapLine.LineId,
                UserProperties = mapLine.UserProperties
            };

            if (needsTranslation)
            {
                VanillaLineSpecTranslator.FinalizeLine(mapLine, line);
                line.MapLineId = mapLine.LineId;
            }

            line.MoreLineIds = mapLine.MoreLineIds;

            if (mapLine.Alpha != 1)
                line.SetAlpha(mapLine.Alpha, true);

            if (mapLine.Health > 0)
            {
                line.ObjectHealth = new()
                {
                    Health = mapLine.Health,
                    OriginalHealth = mapLine.Health,
                    HealthGroup = mapLine.HealthGroup,
                    DamageSpecial = mapLine.DamageSpecial,
                    DeathSpecial = mapLine.DeathSpecial,
                };
            }

            builder.Lines.Add(line);
        }
    }

    private static LineSpecial GetLineSpecial(bool needsTranslation, UdmfLine mapLine, ref LineFlags flags)
    {
        if (mapLine.LineType <= 0)
            return LineSpecial.Default;

        if (needsTranslation)
        {
            var args = new SpecialArgs();
            var spec = VanillaLineSpecTranslator.Translate(ref flags, (VanillaLineSpecialType)mapLine.Special, mapLine.Args.Arg0, ref args, out var lineActivationType, out var compat);
            mapLine.Args = args;
            return new(spec, lineActivationType, compat);
        }

        return new((ZDoomLineSpecialType)mapLine.LineType, mapLine.ActivationType, LineSpecialCompatibility.Default);
    }

    private static (Side front, Side? back) CreateSides(UdmfLine line, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager, bool isTranslated)
    {
        if (line.Back == null)
            return CreateSingleSide(line, builder, ref nextSideId, textureManager, isTranslated);

        Side front = CreateTwoSided(line, line.Front, builder, ref nextSideId, textureManager, isTranslated);
        Side back = CreateTwoSided(line, line.Back, builder, ref nextSideId, textureManager, isTranslated);
        return (front, back);
    }

    private static (Side front, Side? back) CreateSingleSide(UdmfLine line, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager, bool isTranslated)
    {
        var side = line.Front;
        Sector sector = builder.Sectors[side.Sector.Id];
        var middleTexture = GetWallTexture(textureManager, side.MiddleTexture);
        var upperTexture = GetWallTexture(textureManager, side.UpperTexture);
        var lowerTexture = GetWallTexture(textureManager, side.LowerTexture);

        Wall middle = new(middleTexture.Index, WallLocation.Middle, side.LightLevelMiddle, side.LightLevelMiddleAbsolute, side.MiddleOffset, side.MiddleScale);
        Wall upper = new(upperTexture.Index, WallLocation.Upper, side.LightLevelUpper, side.LightLevelUpperAbsolute, side.UpperOffset, side.UpperScale);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower, side.LightLevelLower, side.LightLevelLowerAbsolute, side.BottomOffset, side.BottomScale);

        Side front = new(nextSideId, side.Offset, upper, middle, lower, sector, side.LightLevel, side.LightLevelAbsolute, side.NoFakeConstrast, side.SmoothLighting, 
            line.WrapMidTex || side.WrapMidTex);
        builder.Sides.Add(front);

        if (isTranslated)
            DoomGeometryBuilder.SetColorMaps(line, textureManager, side, front);

        nextSideId++;

        return (front, null);
    }

    private static Side CreateTwoSided(UdmfLine line, UdmfSide side, GeometryBuilder builder, ref int nextSideId, TextureManager textureManager, bool isTranslated)
    {
        Sector facingSector = builder.Sectors[side.Sector.Id];

        var middleTexture = GetWallTexture(textureManager, side.MiddleTexture);
        var upperTexture = GetWallTexture(textureManager, side.UpperTexture);
        var lowerTexture = GetWallTexture(textureManager, side.LowerTexture);

        Wall middle = new(middleTexture.Index, WallLocation.Middle, side.LightLevelMiddle, side.LightLevelMiddleAbsolute, side.MiddleOffset, side.MiddleScale);
        Wall upper = new(upperTexture.Index, WallLocation.Upper, side.LightLevelUpper, side.LightLevelUpperAbsolute, side.UpperOffset, side.UpperScale);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower, side.LightLevelLower, side.LightLevelLowerAbsolute, side.BottomOffset, side.BottomScale);

        Side addSide = new(nextSideId, side.Offset, upper, middle, lower, facingSector, side.LightLevel, side.LightLevelAbsolute, side.NoFakeConstrast, side.SmoothLighting, 
            line.WrapMidTex || side.WrapMidTex)
        {
            UserProperties = side.UserProperties
        };
        builder.Sides.Add(addSide);


        if (isTranslated)
            DoomGeometryBuilder.SetColorMaps(line, textureManager, side, addSide);

        nextSideId++;
        return addSide;
    }

    private static Texture GetWallTexture(TextureManager textureManager, string textureName)
    {
        return textureManager.GetTexture(textureName, ResourceNamespace.Global, ResourceNamespace.Textures);
    }
}
