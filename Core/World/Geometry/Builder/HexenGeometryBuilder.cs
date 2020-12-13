using System;
using System.Collections.Generic;
using Helion.Maps.Doom.Components;
using Helion.Maps.Hexen;
using Helion.Maps.Hexen.Components;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
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
    // TODO: This shares a lot with doom, wonder if we can merge?
    public static class HexenGeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static MapGeometry? Create(HexenMap map)
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
            
            return new MapGeometry(builder, bspTree);
        }
        
        private static SectorPlane CreateAndAddPlane(DoomSector doomSector, List<SectorPlane> sectorPlanes, 
            SectorPlaneFace face)
        {
            int id = sectorPlanes.Count;
            double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
            string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
            int textureHandle = TextureManager.Instance.GetTexture(texture, ResourceNamespace.Flats).Index;
            
            SectorPlane sectorPlane = new SectorPlane(id, face, z, textureHandle, doomSector.LightLevel);
            sectorPlanes.Add(sectorPlane);
            
            return sectorPlane;
        }
        
        private static void PopulateSectorData(HexenMap map, GeometryBuilder builder)
        {
            foreach (DoomSector doomSector in map.Sectors)
            {
                SectorPlane floorPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Floor);
                SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Ceiling);
                // TODO: Is this right?
                ZDoomSectorSpecialType sectorSpecial = (ZDoomSectorSpecialType)doomSector.SectorType;

                Sector sector = new Sector(builder.Sectors.Count, doomSector.Tag, doomSector.LightLevel, 
                    floorPlane, ceilingPlane, sectorSpecial);
                builder.Sectors.Add(sector);
            }
        }

        private static TwoSided CreateTwoSided(DoomSide facingSide, GeometryBuilder builder, ref int nextSideId)
        {
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(facingSide.Sector.Id < builder.Sectors.Count, "Sector (facing) ID mapping broken");
            Sector facingSector = builder.Sectors[facingSide.Sector.Id];

            string middleTexture = facingSide.MiddleTexture;
            int middleTextureHandle = TextureManager.Instance.GetTexture(middleTexture, ResourceNamespace.Textures).Index;
            Wall middle = new Wall(builder.Walls.Count,  middleTextureHandle, WallLocation.Middle);
            
            string upperTexture = facingSide.UpperTexture;
            int upperTextureHandle = TextureManager.Instance.GetTexture(upperTexture, ResourceNamespace.Textures).Index;
            Wall upper = new Wall(builder.Walls.Count + 1, upperTextureHandle, WallLocation.Upper);
            
            string lowerTexture = facingSide.LowerTexture;
            int lowerTextureHandle = TextureManager.Instance.GetTexture(lowerTexture, ResourceNamespace.Textures).Index;
            Wall lower = new Wall(builder.Walls.Count + 2, lowerTextureHandle, WallLocation.Lower);
            
            builder.Walls.Add(middle);
            builder.Walls.Add(upper);
            builder.Walls.Add(lower);
            
            TwoSided side = new TwoSided(nextSideId, facingSide.Offset, upper, middle, lower, facingSector);
            builder.Sides.Add(side);

            nextSideId++;
            
            return side;
        }

        private static (Side front, Side? back) CreateSingleSide(HexenLine doomLine, GeometryBuilder builder,
            ref int nextSideId)
        {
            DoomSide doomSide = doomLine.Front;
            
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(doomSide.Sector.Id < builder.Sectors.Count, "Sector ID mapping broken");
            Sector sector = builder.Sectors[doomSide.Sector.Id];
            int textureHandle = TextureManager.Instance.GetTexture(doomSide.MiddleTexture, ResourceNamespace.Textures).Index;

            // When we get to 3D floors we're going to have to fix this...
            Wall wall = new Wall(builder.Walls.Count, textureHandle, WallLocation.Middle);
            builder.Walls.Add(wall);
            
            Side front = new Side(nextSideId, doomSide.Offset, wall, sector);
            builder.Sides.Add(front);

            wall.Side = front;

            nextSideId++;
            
            return (front, null);
        }

        private static (Side front, Side? back) CreateSides(HexenLine doomLine, GeometryBuilder builder,
            ref int nextSideId)
        {
            if (doomLine.Back == null)
                return CreateSingleSide(doomLine, builder, ref nextSideId);

            TwoSided front = CreateTwoSided(doomLine.Front, builder, ref nextSideId);
            TwoSided back = CreateTwoSided(doomLine.Back, builder, ref nextSideId);
            return (front, back);
        }

        private static void PopulateLineData(HexenMap map, GeometryBuilder builder)
        {
            int nextSideId = 0;
            
            foreach (HexenLine hexenLine in map.Lines)
            {
                if (hexenLine.Start.Position == hexenLine.End.Position)
                {
                    Log.Warn("Zero length linedef pruned (id = {0})", hexenLine.Id);
                    continue;
                }
                
                (Side front, Side? back) = CreateSides(hexenLine, builder, ref nextSideId);

                Seg2D seg = new Seg2D(hexenLine.Start.Position, hexenLine.End.Position);
                LineFlags flags = new LineFlags(hexenLine.Flags);
                LineSpecial special = new LineSpecial(hexenLine.LineType, LineActivationType.Any, LineSpecial.GetCompatibility(hexenLine));
                SpecialArgs specialArgs = new SpecialArgs(hexenLine.Args);

                LineSpecial.ValidateActivationFlags(special.LineSpecialType, flags);
                
                Line line = new Line(builder.Lines.Count, hexenLine.Id, seg, front, back, flags, special, specialArgs);
                builder.Lines.Add(line);
                builder.MapLines[line.MapId] = line;
            }
        }
    }
}