using System.Collections.Generic;
using Helion.Bsp;
using Helion.Geometry.Segments;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Specials;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Util.Assertion;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Builder;

// TODO: This shares a lot with doom, wonder if we can merge?
public static class HexenGeometryBuilder
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static MapGeometry? Create(HexenMap map, IBspBuilder bspBuilder, TextureManager textureManager)
    {
        GeometryBuilder builder = new();

        PopulateSectorData(map, builder, textureManager);
        PopulateLineData(map, builder, textureManager);

        CompactBspTree? bspTree;
        try
        {
            bspTree = CompactBspTree.Create(map, builder, bspBuilder);
            if (bspTree == null)
                return null;
        }
        catch (AssertionException)
        {
            throw;
        }
        catch
        {
            Log.Error("Unable to load map, BSP tree cannot be built due to corrupt geometry");
            return null;
        }

        return new(map, builder, bspTree);
    }

    private static SectorPlane CreateAndAddPlane(DoomSector doomSector, List<SectorPlane> sectorPlanes,
        SectorPlaneFace face, TextureManager textureManager)
    {
        int id = sectorPlanes.Count;
        double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
        string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
        int textureHandle = textureManager.GetTexture(texture, ResourceNamespace.Flats).Index;

        SectorPlane sectorPlane = new(id, face, z, textureHandle, doomSector.LightLevel);
        sectorPlanes.Add(sectorPlane);

        return sectorPlane;
    }

    private static void PopulateSectorData(HexenMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        SectorData sectorData = new();
        foreach (DoomSector doomSector in map.Sectors)
        {
            SectorPlane floorPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Floor, textureManager);
            SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Ceiling, textureManager);
            ZDoomSectorSpecialType sectorSpecial = (ZDoomSectorSpecialType)SectorSpecialData.GetType(doomSector.SectorType, SectorDataType.ZDoom);
            SectorSpecialData.SetSectorData(doomSector.SectorType, sectorData, SectorDataType.ZDoom);

            Sector sector = new Sector(builder.Sectors.Count, doomSector.Tag, doomSector.LightLevel,
                floorPlane, ceilingPlane, sectorSpecial, sectorData);
            builder.Sectors.Add(sector);
            sectorData.Clear();
        }
    }

    private static Side CreateTwoSided(DoomSide facingSide, GeometryBuilder builder, ref int nextSideId, TextureManager textureManager)
    {
        // This is okay because of how we create sectors corresponding
        // to their list index. If this is wrong then someone broke the
        // ordering very badly.
        Invariant(facingSide.Sector.Id < builder.Sectors.Count, "Sector (facing) ID mapping broken");
        Sector facingSector = builder.Sectors[facingSide.Sector.Id];

        string middleTexture = facingSide.MiddleTexture;
        int middleTextureHandle = textureManager.GetTexture(middleTexture, ResourceNamespace.Textures).Index;
        Wall middle = new(builder.Walls.Count,  middleTextureHandle, WallLocation.Middle);

        string upperTexture = facingSide.UpperTexture;
        int upperTextureHandle = textureManager.GetTexture(upperTexture, ResourceNamespace.Textures).Index;
        Wall upper = new(builder.Walls.Count + 1, upperTextureHandle, WallLocation.Upper);

        string lowerTexture = facingSide.LowerTexture;
        int lowerTextureHandle = textureManager.GetTexture(lowerTexture, ResourceNamespace.Textures).Index;
        Wall lower = new(builder.Walls.Count + 2, lowerTextureHandle, WallLocation.Lower);

        builder.Walls.Add(middle);
        builder.Walls.Add(upper);
        builder.Walls.Add(lower);

        Side side = new(nextSideId, facingSide.Offset, upper, middle, lower, facingSector);
        builder.Sides.Add(side);

        nextSideId++;

        return side;
    }

    private static (Side front, Side? back) CreateSingleSide(HexenLine doomLine, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
    {
        DoomSide doomSide = doomLine.Front;

        // This is okay because of how we create sectors corresponding
        // to their list index. If this is wrong then someone broke the
        // ordering very badly.
        Invariant(doomSide.Sector.Id < builder.Sectors.Count, "Sector ID mapping broken");
        Sector sector = builder.Sectors[doomSide.Sector.Id];
        string middleTexture = doomSide.MiddleTexture;
        int middleTextureHandle = textureManager.GetTexture(middleTexture, ResourceNamespace.Textures).Index;
        Wall middle = new(builder.Walls.Count, middleTextureHandle, WallLocation.Middle);

        string upperTexture = doomSide.UpperTexture;
        int upperTextureHandle = textureManager.GetTexture(upperTexture, ResourceNamespace.Textures).Index;
        Wall upper = new(builder.Walls.Count + 1, upperTextureHandle, WallLocation.Upper);

        string lowerTexture = doomSide.LowerTexture;
        int lowerTextureHandle = textureManager.GetTexture(lowerTexture, ResourceNamespace.Textures).Index;
        Wall lower = new(builder.Walls.Count + 2, lowerTextureHandle, WallLocation.Lower);

        builder.Walls.Add(middle);
        builder.Walls.Add(upper);
        builder.Walls.Add(lower);

        Side front = new(nextSideId, doomSide.Offset, upper, middle, lower, sector);
        builder.Sides.Add(front);

        nextSideId++;

        return (front, null);
    }

    private static (Side front, Side? back) CreateSides(HexenLine doomLine, GeometryBuilder builder,
        ref int nextSideId, TextureManager textureManager)
    {
        if (doomLine.Back == null)
            return CreateSingleSide(doomLine, builder, ref nextSideId, textureManager);

        Side front = CreateTwoSided(doomLine.Front, builder, ref nextSideId, textureManager);
        Side back = CreateTwoSided(doomLine.Back, builder, ref nextSideId, textureManager);
        return (front, back);
    }

    private static void PopulateLineData(HexenMap map, GeometryBuilder builder, TextureManager textureManager)
    {
        int nextSideId = 0;

        foreach (HexenLine hexenLine in map.Lines)
        {
            if (hexenLine.Start.Position == hexenLine.End.Position)
            {
                Log.Warn("Zero length linedef pruned (id = {0})", hexenLine.Id);
                continue;
            }

            (Side front, Side? back) = CreateSides(hexenLine, builder, ref nextSideId, textureManager);

            Seg2D seg = new(hexenLine.Start.Position, hexenLine.End.Position);
            LineFlags flags = new(hexenLine.Flags);
            LineSpecial special;
            if (hexenLine.LineType == ZDoomLineSpecialType.None)
                special = LineSpecial.Default;
            else
                special = new LineSpecial(hexenLine.LineType, LineActivationType.Any, LineSpecial.GetCompatibility(hexenLine));

            SpecialArgs specialArgs = new(hexenLine.Args);
            LineSpecial.ValidateActivationFlags(special.LineSpecialType, ref flags);

            Line line = new(builder.Lines.Count, hexenLine.Id, seg, front, back, flags, special, specialArgs);
            builder.Lines.Add(line);
            builder.MapLines[line.MapId] = line;
        }
    }
}
