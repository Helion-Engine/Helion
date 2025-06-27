using Helion.Maps.Specials.ZDoom;
using Helion.Maps.Specials;
using Helion.Maps.Udmf;
using Helion.Resources;
using Helion.World.Bsp;
using Helion.World.Geometry.Sectors;
using System;
using Helion.Maps.Udmf.Components;
using Helion.Geometry.Segments;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sides;
using Helion.World.Special;
using Helion.World.Geometry.Walls;
using Helion.Util;
using Helion.Maps.Specials.Compatibility;

namespace Helion.World.Geometry.Builder;

public class UdmfGeometryBuilder
{
    public static MapGeometry? Create(UdmfMap map, GeometryBuilder builder, TextureManager textureManager, 
        Func<(CompactBspTree, BspTreeNew)?> createBspTree)
    {
        PopulateSectorData(map, builder, textureManager);
        PopulateLineData(map, builder, textureManager);

        var bspTree = createBspTree();
        if (!bspTree.HasValue)
            return null;

        return new(builder, bspTree.Value.Item1, bspTree.Value.Item2);
    }

    private static void PopulateSectorData(UdmfMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        SectorData sectorData = new();
        foreach (var mapSector in map.Sectors)
        {
            var floorPlane = CreateSectorPlane(mapSector, SectorPlaneFace.Floor, textureManager);
            var ceilingPlane = CreateSectorPlane(mapSector, SectorPlaneFace.Ceiling, textureManager);
            var sector = new Sector(builder.Sectors.Count, mapSector.Tag, mapSector.LightLevel,
                floorPlane, ceilingPlane, mapSector.Special, sectorData)
            {
                Silent = mapSector.Silent,
                NoAttack = mapSector.NoAttack,
                Gravity = mapSector.Gravity,
                DamageAmount = mapSector.DamageAmount,
                DamageInterval = mapSector.DamageInterval,
                DamageLeakiness = mapSector.Leakiness,
                SkyFloor = mapSector.SkyFloor,
                SkyCeiling = mapSector.SkyCeiling,
            };
            
            floorPlane.LightLevel = mapSector.LightFloor;
            floorPlane.LightLevelAbsolute = mapSector.LightFloorAbsolute;
            floorPlane.RenderOffsets.Offset.X = mapSector.PanningFloorX;
            floorPlane.RenderOffsets.LastOffset.X = mapSector.PanningFloorX;
            floorPlane.RenderOffsets.Offset.Y = mapSector.PanningFloorY;
            floorPlane.RenderOffsets.LastOffset.Y = mapSector.PanningFloorY;
            floorPlane.RenderOffsets.Rotate = MathHelper.ToRadians(mapSector.RotationFloor);
            floorPlane.RenderOffsets.Scale.X = mapSector.ScaleFloorX;
            floorPlane.RenderOffsets.Scale.Y = mapSector.ScaleFloorY;

            ceilingPlane.LightLevel = mapSector.LightCeiling;
            ceilingPlane.LightLevelAbsolute = mapSector.LightCeilingAbsolute;
            ceilingPlane.RenderOffsets.Offset.X = mapSector.PanningCeilingX;
            ceilingPlane.RenderOffsets.LastOffset.X = mapSector.PanningCeilingX;
            ceilingPlane.RenderOffsets.Offset.Y = mapSector.PanningCeilingY;
            ceilingPlane.RenderOffsets.LastOffset.Y = mapSector.PanningCeilingY;
            ceilingPlane.RenderOffsets.Rotate = MathHelper.ToRadians(mapSector.RotationCeiling);
            ceilingPlane.RenderOffsets.Scale.X = mapSector.ScaleCeilingX;
            ceilingPlane.RenderOffsets.Scale.Y = mapSector.ScaleCeilingY;

            builder.Sectors.Add(sector);
            sectorData.Clear();
        }
    }

    private static SectorPlane CreateSectorPlane(UdmfSector sector, SectorPlaneFace face,
        TextureManager textureManager)
    {
        double z = (face == SectorPlaneFace.Floor ? sector.FloorZ : sector.CeilingZ);
        string texture = (face == SectorPlaneFace.Floor ? sector.FloorTexture : sector.CeilingTexture);
        int handle = textureManager.GetTexture(texture, ResourceNamespace.Global, ResourceNamespace.Flats).Index;
        return new SectorPlane(face, z, handle, sector.LightLevel);
    }

    private static void PopulateLineData(UdmfMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        int nextSideId = 0;

        foreach (var mapLine in map.Lines)
        {
            (Side front, Side? back) = CreateSides(mapLine, builder, ref nextSideId, textureManager);
            Seg2D seg = new(mapLine.StartPosition, mapLine.EndPosition);
            LineFlags flags = new(mapLine.Flags);

            LineSpecial special;
            if (mapLine.Special == ZDoomLineSpecialType.None)
                special = LineSpecial.Default;
            else
                special = new LineSpecial(mapLine.Special, mapLine.ActivationType, LineSpecialCompatibility.Default);

            LineSpecial.ValidateActivationFlags(special.LineSpecialType, ref flags, map.MapType);
            var line = new Line(mapLine.Id, seg, front, back, flags, special, mapLine.Args)
            {
                Alpha = mapLine.Alpha,
                LockNumber = mapLine.LockNumber,
                MapLineId = mapLine.LineId
            };

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

    private static (Side front, Side? back) CreateSides(UdmfLine line, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
    {
        if (line.Back == null)
            return CreateSingleSide(line, builder, ref nextSideId, textureManager);

        Side front = CreateTwoSided(line, line.Front, builder, ref nextSideId, textureManager);
        Side back = CreateTwoSided(line, line.Back, builder, ref nextSideId, textureManager);
        return (front, back);
    }

    private static (Side front, Side? back) CreateSingleSide(UdmfLine line, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
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

        nextSideId++;

        return (front, null);
    }

    private static Side CreateTwoSided(UdmfLine line, UdmfSide side, GeometryBuilder builder, ref int nextSideId, TextureManager textureManager)
    {
        Sector facingSector = builder.Sectors[side.Sector.Id];

        var middleTexture = GetWallTexture(textureManager, side.MiddleTexture);
        var upperTexture = GetWallTexture(textureManager, side.UpperTexture);
        var lowerTexture = GetWallTexture(textureManager, side.LowerTexture);

        Wall middle = new(middleTexture.Index, WallLocation.Middle, side.LightLevelMiddle, side.LightLevelMiddleAbsolute, side.MiddleOffset, side.MiddleScale);
        Wall upper = new(upperTexture.Index, WallLocation.Upper, side.LightLevelUpper, side.LightLevelUpperAbsolute, side.UpperOffset, side.UpperScale);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower, side.LightLevelLower, side.LightLevelLowerAbsolute, side.BottomOffset, side.BottomScale);

        Side addSide = new(nextSideId, side.Offset, upper, middle, lower, facingSector, side.LightLevel, side.LightLevelAbsolute, side.NoFakeConstrast, side.SmoothLighting, 
            line.WrapMidTex || side.WrapMidTex);
        builder.Sides.Add(addSide);

        nextSideId++;
        return addSide;
    }

    private static Texture GetWallTexture(TextureManager textureManager, string textureName)
    {
        return textureManager.GetTexture(textureName, ResourceNamespace.Global, ResourceNamespace.Textures);
    }
}
