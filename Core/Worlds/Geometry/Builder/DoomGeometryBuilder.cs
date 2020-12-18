using System.Collections.Generic;
using System.Linq;
using Helion.Maps;
using Helion.Maps.Components.Sectors;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Util;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Textures;
using MapSector = Helion.Maps.Components.Sectors.Sector;
using Sector = Helion.Worlds.Geometry.Sectors.Sector;

namespace Helion.Worlds.Geometry.Builder
{
    public static class DoomGeometryBuilder
    {
        public static MapGeometry? Create(Map map)
        {
            GeometryBuilder builder = new();

            PopulateSectors(map, builder);
            PopulateLines(map, builder);

            return new MapGeometry(builder);
        }

        private static void PopulateSectors(Map map, GeometryBuilder builder)
        {
            foreach (DoomSector mapSector in map.Sectors.Cast<DoomSector>())
            {
                int index = builder.Sectors.Count;
                SectorPlane floor = CreateAndAddPlane(mapSector, builder.SectorPlanes, SectorPlaneFace.Floor);
                SectorPlane ceiling = CreateAndAddPlane(mapSector, builder.SectorPlanes, SectorPlaneFace.Ceiling);
                ZDoomSectorSpecialType special = VanillaSectorSpecTranslator.Translate((VanillaSectorSpecialType)mapSector.SectorType);

                Sector sector = new(index, mapSector.Tag, mapSector.LightLevel, floor, ceiling, special);
                builder.Sectors.Add(sector);
            }
        }

        private static SectorPlane CreateAndAddPlane(MapSector mapSector, List<SectorPlane> planes, SectorPlaneFace face)
        {
            int index = planes.Count;
            double z = (face == SectorPlaneFace.Floor ? mapSector.FloorZ : mapSector.CeilingZ);
            CIString textureName = (face == SectorPlaneFace.Floor ? mapSector.FloorTexture : mapSector.CeilingTexture);
            IWorldTexture texture = null!;

            SectorPlane sectorPlane = new(index, face, z, texture, mapSector.LightLevel);
            planes.Add(sectorPlane);

            return sectorPlane;
        }

        private static void PopulateLines(Map map, GeometryBuilder builder)
        {
        }

        // private static SectorPlane CreateAndAddPlane(DoomSector doomSector, List<SectorPlane> sectorPlanes, SectorPlaneFace face)
        // {
        //     int id = sectorPlanes.Count;
        //     double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
        //     string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
        //     int handle = TextureManager.Instance.GetTexture(texture, Namespace.Flats).Index;
        //
        //     SectorPlane sectorPlane = new SectorPlane(id, face, z, handle, doomSector.LightLevel);
        //     sectorPlanes.Add(sectorPlane);
        //
        //     return sectorPlane;
        // }
        //
        // private static void PopulateSectorData(DoomMap map, GeometryBuilder builder)
        // {
        //     foreach (DoomSector doomSector in map.Sectors)
        //     {
        //         SectorPlane floorPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Floor);
        //         SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Ceiling);
        //         ZDoomSectorSpecialType sectorSpecial = VanillaSectorSpecTranslator.Translate((VanillaSectorSpecialType)doomSector.SectorType);
        //
        //         Sector sector = new Sector(builder.Sectors.Count, doomSector.Tag, doomSector.LightLevel,
        //             floorPlane, ceilingPlane, sectorSpecial);
        //         builder.Sectors.Add(sector);
        //     }
        // }
        //
        // private static (Side front, Side? back) CreateSingleSide(DoomLine doomLine, GeometryBuilder builder,
        //     ref int nextSideId)
        // {
        //     DoomSide doomSide = doomLine.Front;
        //
        //     // This is okay because of how we create sectors corresponding
        //     // to their list index. If this is wrong then someone broke the
        //     // ordering very badly.
        //     Invariant(doomSide.Sector.Id < builder.Sectors.Count, "Sector ID mapping broken");
        //     Sector sector = builder.Sectors[doomSide.Sector.Id];
        //     int handle = TextureManager.Instance.GetTexture(doomSide.MiddleTexture, Namespace.Textures).Index;
        //
        //     // When we get to 3D floors we're going to have to fix this...
        //     Wall wall = new Wall(builder.Walls.Count, handle, WallLocation.Middle);
        //     builder.Walls.Add(wall);
        //
        //     Side front = new Side(nextSideId, doomSide.Offset, wall, sector);
        //     builder.Sides.Add(front);
        //
        //     wall.Side = front;
        //
        //     nextSideId++;
        //
        //     return (front, null);
        // }
        //
        // private static TwoSided CreateTwoSided(DoomSide facingSide, GeometryBuilder builder, ref int nextSideId)
        // {
        //     // This is okay because of how we create sectors corresponding
        //     // to their list index. If this is wrong then someone broke the
        //     // ordering very badly.
        //     Invariant(facingSide.Sector.Id < builder.Sectors.Count, "Sector (facing) ID mapping broken");
        //     Sector facingSector = builder.Sectors[facingSide.Sector.Id];
        //
        //     var middleTexture = TextureManager.Instance.GetTexture(facingSide.MiddleTexture, Namespace.Textures);
        //     var upperTexture = TextureManager.Instance.GetTexture(facingSide.UpperTexture, Namespace.Textures);
        //     var lowerTexture = TextureManager.Instance.GetTexture(facingSide.LowerTexture, Namespace.Textures);
        //
        //     Wall middle = new Wall(builder.Walls.Count, middleTexture.Index, WallLocation.Middle);
        //     Wall upper = new Wall(builder.Walls.Count + 1, upperTexture.Index, WallLocation.Upper);
        //     Wall lower = new Wall(builder.Walls.Count + 2, lowerTexture.Index, WallLocation.Lower);
        //     builder.Walls.Add(middle);
        //     builder.Walls.Add(upper);
        //     builder.Walls.Add(lower);
        //
        //     TwoSided side = new TwoSided(nextSideId, facingSide.Offset, upper, middle, lower, facingSector);
        //     builder.Sides.Add(side);
        //
        //     nextSideId++;
        //
        //     return side;
        // }
        //
        // private static (Side front, Side? back) CreateSides(DoomLine doomLine, GeometryBuilder builder,
        //     ref int nextSideId)
        // {
        //     if (doomLine.Back == null)
        //         return CreateSingleSide(doomLine, builder, ref nextSideId);
        //
        //     TwoSided front = CreateTwoSided(doomLine.Front, builder, ref nextSideId);
        //     TwoSided back = CreateTwoSided(doomLine.Back, builder, ref nextSideId);
        //     return (front, back);
        // }
        //
        // private static void PopulateLineData(DoomMap map, GeometryBuilder builder)
        // {
        //     int nextSideId = 0;
        //
        //     foreach (DoomLine doomLine in map.Lines)
        //     {
        //         if (doomLine.Start.Position == doomLine.End.Position)
        //         {
        //             Log.Warn("Zero length linedef pruned (id = {0})", doomLine.Id);
        //             continue;
        //         }
        //
        //         (Side front, Side? back) = CreateSides(doomLine, builder, ref nextSideId);
        //
        //         Seg2D seg = new Seg2D(doomLine.Start.Position, doomLine.End.Position);
        //         LineFlags flags = new LineFlags(doomLine.Flags);
        //         SpecialArgs specialArgs = default;
        //         ZDoomLineSpecialType zdoomType = VanillaLineSpecTranslator.Translate(flags, doomLine.LineType, (byte)doomLine.SectorTag,
        //             ref specialArgs, out LineSpecialCompatibility? compatibility);
        //         LineActivationType activationType = VanillaLineSpecTranslator.GetLineTagActivation(doomLine.LineType);
        //         LineSpecial special = new LineSpecial(zdoomType, activationType, compatibility);
        //         Line line = new Line(builder.Lines.Count, doomLine.Id, seg, front, back, flags, special, specialArgs);
        //         builder.Lines.Add(line);
        //         builder.MapLines[line.MapId] = line;
        //     }
        // }
    }
}