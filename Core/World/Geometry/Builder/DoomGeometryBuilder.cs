using System;
using System.Collections.Generic;
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
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Builder;

public static class DoomGeometryBuilder
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static MapGeometry? Create(DoomMap map, GeometryBuilder builder, TextureManager textureManager, 
        Func<(CompactBspTree, BspTreeNew)?> createBspTree)
    {
        PopulateSectorData(map, builder, textureManager);
        PopulateLineData(map, builder, textureManager);

        var bspTree = createBspTree();
        if (!bspTree.HasValue)
            return null;

        return new(map, builder, bspTree.Value.Item1, bspTree.Value.Item2);
    }

    private static SectorPlane CreateAndAddPlane(DoomSector doomSector, List<SectorPlane> sectorPlanes, SectorPlaneFace face,
        TextureManager textureManager)
    {
        int id = sectorPlanes.Count;
        double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
        string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
        int handle = textureManager.GetTexture(texture, ResourceNamespace.Flats).Index;

        SectorPlane sectorPlane = new(id, face, z, handle, doomSector.LightLevel);
        sectorPlanes.Add(sectorPlane);

        return sectorPlane;
    }

    private static void PopulateSectorData(DoomMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        SectorData sectorData = new();
        foreach (DoomSector doomSector in map.Sectors)
        {
            SectorPlane floorPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Floor, textureManager);
            SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Ceiling, textureManager);
            ZDoomSectorSpecialType sectorSpecial = VanillaSectorSpecTranslator.Translate(doomSector.SectorType, sectorData);

            Sector sector = new Sector(builder.Sectors.Count, doomSector.Tag, doomSector.LightLevel,
                floorPlane, ceilingPlane, sectorSpecial, sectorData);
            builder.Sectors.Add(sector);
            sectorData.Clear();
        }
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
        var middleTexture = textureManager.GetTexture(doomSide.MiddleTexture, ResourceNamespace.Textures);
        var upperTexture = textureManager.GetTexture(doomSide.UpperTexture, ResourceNamespace.Textures);
        var lowerTexture = textureManager.GetTexture(doomSide.LowerTexture, ResourceNamespace.Textures);

        Wall middle = new(middleTexture.Index, WallLocation.Middle);
        Wall upper = new(upperTexture.Index, WallLocation.Upper);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower);

        Side front = new Side(nextSideId, doomSide.Offset, upper, middle, lower, sector);
        builder.Sides.Add(front);

        if (doomLine.LineType == VanillaLineSpecialType.TransferHeights)
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

        nextSideId++;

        return (front, null);
    }

    private static Side CreateTwoSided(DoomSide facingSide, GeometryBuilder builder, ref int nextSideId, TextureManager textureManager)
    {
        // This is okay because of how we create sectors corresponding
        // to their list index. If this is wrong then someone broke the
        // ordering very badly.
        Invariant(facingSide.Sector.Id < builder.Sectors.Count, "Sector (facing) ID mapping broken");
        Sector facingSector = builder.Sectors[facingSide.Sector.Id];

        var middleTexture = textureManager.GetTexture(facingSide.MiddleTexture, ResourceNamespace.Textures);
        var upperTexture = textureManager.GetTexture(facingSide.UpperTexture, ResourceNamespace.Textures);
        var lowerTexture = textureManager.GetTexture(facingSide.LowerTexture, ResourceNamespace.Textures);

        Wall middle = new(middleTexture.Index, WallLocation.Middle);
        Wall upper = new(upperTexture.Index, WallLocation.Upper);
        Wall lower = new(lowerTexture.Index, WallLocation.Lower);

        Side side = new(nextSideId, facingSide.Offset, upper, middle, lower, facingSector);
        builder.Sides.Add(side);

        nextSideId++;

        return side;
    }

    private static (Side front, Side? back) CreateSides(DoomLine doomLine, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
    {
        if (doomLine.Back == null)
            return CreateSingleSide(doomLine, builder, ref nextSideId, textureManager);

        Side front = CreateTwoSided(doomLine.Front, builder, ref nextSideId, textureManager);
        Side back = CreateTwoSided(doomLine.Back, builder, ref nextSideId, textureManager);
        return (front, back);
    }

    private static void PopulateLineData(DoomMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        int nextSideId = 0;

        foreach (DoomLine doomLine in map.Lines)
        {
            if (doomLine.Start.Position == doomLine.End.Position)
            {
                Log.Warn("Zero length linedef pruned (id = {0})", doomLine.Id);
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

            Line line = new(builder.Lines.Count, seg, front, back, flags, special, specialArgs);
            VanillaLineSpecTranslator.FinalizeLine(doomLine, line);
            builder.Lines.Add(line);
        }
    }
}
