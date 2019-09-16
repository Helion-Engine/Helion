using System.Collections.Generic;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources;
using Helion.Util.Assertion;
using Helion.Util.Geometry.Segments;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using Helion.World.Special;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Builder
{
    public static class DoomGeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static MapGeometry? Create(DoomMap map)
        {
            GeometryBuilder builder = new GeometryBuilder();
            
            PopulateSectorData(map, builder);
            PopulateLineData(map, builder);

            BspTree? bspTree;
            try
            {
                bspTree = BspTree.Create(map, builder);
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
            
            // TODO: Connect subsector to sectors, and subsector segments to sides (and/or lines)?

            return new MapGeometry(builder, bspTree);
        }

        private static SectorPlane CreateAndAddPlane(DoomSector doomSector, List<SectorPlane> sectorPlanes, SectorPlaneFace face)
        {
            int id = sectorPlanes.Count;
            double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
            string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
            
            SectorPlane sectorPlane = new SectorPlane(id, face, z, TextureManager.Instance.GetTexture(texture, ResourceNamespace.Flats).Index, doomSector.LightLevel);
            sectorPlanes.Add(sectorPlane);
            
            return sectorPlane;
        }
        
        private static void PopulateSectorData(DoomMap map, GeometryBuilder builder)
        {
            foreach (DoomSector doomSector in map.Sectors)
            {
                SectorPlane floorPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Floor);
                SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Ceiling);
                ZDoomSectorSpecialType sectorSpecial = VanillaSectorSpecTranslator.Translate((VanillaSectorSpecialType)doomSector.SectorType);

                Sector sector = new Sector(builder.Sectors.Count, doomSector.Id, doomSector.Tag, 
                    doomSector.LightLevel, floorPlane, ceilingPlane, sectorSpecial);
                builder.Sectors.Add(sector);
            }
        }

        private static (Side front, Side? back) CreateSingleSide(DoomLine doomLine, GeometryBuilder builder,
            ref int nextSideId)
        {
            DoomSide doomSide = doomLine.Front;
            
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(doomSide.Sector.Id < builder.Sectors.Count, "Sector ID mapping broken");
            Sector sector = builder.Sectors[doomSide.Sector.Id];

            // When we get to 3D floors we're going to have to fix this...
            Wall wall = new Wall(builder.Walls.Count, TextureManager.Instance.GetTexture(doomSide.MiddleTexture, ResourceNamespace.Textures).Index, WallLocation.Middle);
            builder.Walls.Add(wall);
            
            Side front = new Side(nextSideId, doomSide.Id, doomSide.Offset, wall, sector);
            builder.Sides.Add(front);

            wall.Side = front;

            nextSideId++;
            
            return (front, null);
        }

        private static TwoSided CreateTwoSided(DoomSide facingSide, GeometryBuilder builder, ref int nextSideId)
        {
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(facingSide.Sector.Id < builder.Sectors.Count, "Sector (facing) ID mapping broken");
            Sector facingSector = builder.Sectors[facingSide.Sector.Id];

            var middleTexture = TextureManager.Instance.GetTexture(facingSide.MiddleTexture, ResourceNamespace.Textures);
            var upperTexture = TextureManager.Instance.GetTexture(facingSide.UpperTexture, ResourceNamespace.Textures);
            var lowerTexture = TextureManager.Instance.GetTexture(facingSide.LowerTexture, ResourceNamespace.Textures);
            
            Wall middle = new Wall(builder.Walls.Count, middleTexture.Index, WallLocation.Middle);
            Wall upper = new Wall(builder.Walls.Count + 1, upperTexture.Index, WallLocation.Upper);
            Wall lower = new Wall(builder.Walls.Count + 2, lowerTexture.Index, WallLocation.Lower);
            builder.Walls.Add(middle);
            builder.Walls.Add(upper);
            builder.Walls.Add(lower);
            
            TwoSided side = new TwoSided(nextSideId, facingSide.Id, facingSide.Offset, upper, middle, lower, facingSector);
            builder.Sides.Add(side);

            nextSideId++;
            
            return side;
        }

        private static (Side front, Side? back) CreateSides(DoomLine doomLine, GeometryBuilder builder,
            ref int nextSideId)
        {
            if (doomLine.Back == null)
                return CreateSingleSide(doomLine, builder, ref nextSideId);

            TwoSided front = CreateTwoSided(doomLine.Front, builder, ref nextSideId);
            TwoSided back = CreateTwoSided(doomLine.Back, builder, ref nextSideId);
            return (front, back);
        }

        private static void PopulateLineData(DoomMap map, GeometryBuilder builder)
        {
            int nextSideId = 0;
            
            foreach (DoomLine doomLine in map.Lines)
            {
                if (doomLine.Start.Position == doomLine.End.Position)
                {
                    Log.Warn("Zero length linedef pruned (id = {0})", doomLine.Id);
                    continue;
                }
                
                (Side front, Side? back) = CreateSides(doomLine, builder, ref nextSideId);

                Seg2D seg = new Seg2D(doomLine.Start.Position, doomLine.End.Position);
                LineFlags flags = new LineFlags(doomLine.Flags);
                SpecialArgs specialArgs = new SpecialArgs();
                ZDoomLineSpecialType zdoomType = VanillaLineSpecTranslator.Translate(flags, doomLine.LineType, (byte)doomLine.SectorTag, specialArgs);
                LineSpecial special = new LineSpecial(zdoomType);
                
                Line line = new Line(builder.Lines.Count, doomLine.Id, seg, front, back, flags, special, specialArgs);
                builder.Lines.Add(line);
            }
        }
    }
}