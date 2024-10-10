using System;
using Helion.Geometry.Segments;
using Helion.Graphics.Palettes;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Loggers;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Builder;

public static class DoomGeometryBuilder
{
    private static int SectorIndex;
    private static int PlaneIndex;
    private static int LineIndex;
    private static readonly DynamicArray<Sector> Sectors = new(1024);
    private static readonly DynamicArray<SectorPlane> Planes = new(2048);
    private static readonly DynamicArray<Line> Lines = new(2048);

    public static MapGeometry? Create(DoomMap map, GeometryBuilder builder, TextureManager textureManager,
        Func<(CompactBspTree, BspTreeNew)?> createBspTree)
    {
        InitCache(map);
        PopulateSectorData(map, builder, textureManager);
        PopulateLineData(map, builder, textureManager);

        var bspTree = createBspTree();
        if (!bspTree.HasValue)
            return null;

        return new(map, builder, bspTree.Value.Item1, bspTree.Value.Item2);
    }

    private static void InitCache(DoomMap map)
    {
        SectorIndex = 0;
        PlaneIndex = 0;
        LineIndex = 0;

        if (Sectors.Length < map.Sectors.Count)
        {
            Sectors.FlushReferences();
            Planes.FlushReferences();
            Sectors.Length = 0;
            Planes.Length = 0;

            for (int i = 0; i < map.Sectors.Count; i++)
                Sectors.Add(new Sector());

            for (int i = 0; i < map.Sectors.Count * 2; i++)
                Planes.Add(new SectorPlane());
        }

        if (Lines.Length < map.Lines.Count)
        {
            Lines.FlushReferences();
            Lines.Length = 0;

            for (int i = 0; i < map.Lines.Count; i++)
                Lines.Add(new Line());
        }
    }

    private static SectorPlane CreateSectorPlane(DoomSector doomSector, SectorPlaneFace face,
        TextureManager textureManager)
    {
        double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
        string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
        int handle = textureManager.GetTexture(texture, ResourceNamespace.Global, ResourceNamespace.Flats).Index;
        var plane = Planes[PlaneIndex++];
        plane.Set(face, z, handle, doomSector.LightLevel);
        return plane;
    }

    private static void PopulateSectorData(DoomMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        SectorData sectorData = new();
        foreach (DoomSector doomSector in map.Sectors)
        {
            SectorPlane floorPlane = CreateSectorPlane(doomSector, SectorPlaneFace.Floor, textureManager);
            SectorPlane ceilingPlane = CreateSectorPlane(doomSector, SectorPlaneFace.Ceiling, textureManager);
            ZDoomSectorSpecialType sectorSpecial = VanillaSectorSpecTranslator.Translate(doomSector.SectorType, sectorData);

            var sector = Sectors[SectorIndex++];
            sector.Set(builder.Sectors.Count, doomSector.Tag, doomSector.LightLevel,
                floorPlane, ceilingPlane, sectorSpecial, sectorData);
            builder.Sectors.Add(sector);
            sectorData.Clear();
        }
    }

    private static Texture GetWallTexture(TextureManager textureManager, string textureName)
    {
        return textureManager.GetTexture(textureName, ResourceNamespace.Global, ResourceNamespace.Textures);
    }

    private static (Side front, Side? back) CreateSingleSide(DoomLine doomLine, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
    {
        DoomSide doomSide = doomLine.Front;

        // This is okay because of how we create sectors corresponding
        // to their list index. If this is wrong then someone broke the
        // ordering very badly.
        Invariant(doomSide.Sector.Id < builder.Sectors.Count, "Sector ID mapping broken");
        Sector sector = builder.Sectors[doomSide.Sector.Id];
        var middleTexture = GetWallTexture(textureManager, doomSide.MiddleTexture);
        var upperTexture = GetWallTexture(textureManager, doomSide.UpperTexture);
        var lowerTexture = GetWallTexture(textureManager, doomSide.LowerTexture);

        Wall middle = new(middleTexture.Index, WallLocation.Middle);
        Wall upper = new(upperTexture.Index, WallLocation.Upper);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower);

        Side front = new(nextSideId, doomSide.Offset, upper, middle, lower, sector);
        builder.Sides.Add(front);

        SetColorMaps(doomLine, textureManager, doomSide, middleTexture, upperTexture, lowerTexture, front, true);

        nextSideId++;

        return (front, null);
    }

    private static void SetColorMaps(DoomLine doomLine, TextureManager textureManager, DoomSide doomSide, Texture middleTexture, Texture upperTexture, Texture lowerTexture, Side front, bool oneSided)
    {
        // Boom only allows transfer heights colormaps for one-sided lines?
        if (oneSided && doomLine.LineType == VanillaLineSpecialType.TransferHeights)
        {
            Colormap? upperColormap = null;
            Colormap? middleColormap = null;
            Colormap? lowerColormap = null;
            if (upperTexture.Index == Constants.NoTextureIndex)
                textureManager.TryGetColormap(doomSide.UpperTexture, out upperColormap);
            if (middleTexture.Index == Constants.NoTextureIndex)
                textureManager.TryGetColormap(doomSide.MiddleTexture, out middleColormap);
            if (lowerTexture.Index == Constants.NoTextureIndex)
                textureManager.TryGetColormap(doomSide.LowerTexture, out lowerColormap);

            if (upperColormap != null || middleColormap != null || lowerColormap != null)
                front.Colormaps = new(upperColormap, middleColormap, lowerColormap);
        }

        if (IsSetColorMap(doomLine) && textureManager.TryGetColormap(doomSide.UpperTexture, out var colormap))
            front.Colormaps = new(colormap, null, null);
    }

    private static bool IsSetColorMap(DoomLine line)
    {
        switch (line.LineType)
        {
            case VanillaLineSpecialType.SetSectorColorMap:
            case VanillaLineSpecialType.W1_SetSectorColorMap:
            case VanillaLineSpecialType.WR_SetSectorColorMap:
            case VanillaLineSpecialType.S1_SetSectorColorMap:
            case VanillaLineSpecialType.SR_SetSectorColorMap:
            case VanillaLineSpecialType.G1_SetSectorColorMap:
            case VanillaLineSpecialType.GR_SetSectorColorMap:
                return true;
        }

        return false;
    }

    private static Side CreateTwoSided(DoomLine line, DoomSide facingSide, GeometryBuilder builder, ref int nextSideId, TextureManager textureManager)
    {
        // This is okay because of how we create sectors corresponding
        // to their list index. If this is wrong then someone broke the
        // ordering very badly.
        Invariant(facingSide.Sector.Id < builder.Sectors.Count, "Sector (facing) ID mapping broken");
        Sector facingSector = builder.Sectors[facingSide.Sector.Id];

        var middleTexture = GetWallTexture(textureManager, facingSide.MiddleTexture);
        var upperTexture = GetWallTexture(textureManager, facingSide.UpperTexture);
        var lowerTexture = GetWallTexture(textureManager, facingSide.LowerTexture);
            
        Wall middle = new(middleTexture.Index, WallLocation.Middle);
        Wall upper = new(upperTexture.Index, WallLocation.Upper);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower);

        Side side = new(nextSideId, facingSide.Offset, upper, middle, lower, facingSector);
        builder.Sides.Add(side);

        SetColorMaps(line, textureManager, facingSide, middleTexture, upperTexture, lowerTexture, side, false);

        nextSideId++;
        return side;
    }

    private static (Side front, Side? back) CreateSides(DoomLine doomLine, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
    {
        if (doomLine.Back == null)
            return CreateSingleSide(doomLine, builder, ref nextSideId, textureManager);

        Side front = CreateTwoSided(doomLine, doomLine.Front, builder, ref nextSideId, textureManager);
        Side back = CreateTwoSided(doomLine, doomLine.Back, builder, ref nextSideId, textureManager);
        return (front, back);
    }

    private static void PopulateLineData(DoomMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        int nextSideId = 0;

        foreach (DoomLine doomLine in map.Lines)
        {
            if (doomLine.Start.Position == doomLine.End.Position)
            {
                HelionLog.Warn($"Zero length linedef pruned (id = {doomLine.Id})");
                continue;
            }

            (Side front, Side? back) = CreateSides(doomLine, builder, ref nextSideId, textureManager);

            Seg2D seg = new(doomLine.Start.Position, doomLine.End.Position);
            LineFlags flags = new(doomLine.Flags);
            SpecialArgs specialArgs = default;
            ZDoomLineSpecialType zdoomType = VanillaLineSpecTranslator.Translate(ref flags, doomLine.LineType, doomLine.SectorTag,
                ref specialArgs, out LineActivationType activationType, out LineSpecialCompatibility compatibility);

            LineSpecial special;
            if (zdoomType == ZDoomLineSpecialType.None)
                special = LineSpecial.Default;
            else
                special = new LineSpecial(zdoomType, activationType, compatibility);

            var line = Lines[LineIndex++];
            line.Set(builder.Lines.Count, seg, front, back, flags, special, specialArgs);
            VanillaLineSpecTranslator.FinalizeLine(doomLine, line);
            builder.Lines.Add(line);
        }
    }
}
