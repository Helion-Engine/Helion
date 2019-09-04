using System.Collections.Generic;
using Helion.Maps.Doom;
using Helion.Maps.Doom.Components;
using Helion.Util.Geometry;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Geometry.Builder
{
    public class DoomGeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static MapGeometry? Create(DoomMap map)
        {
            GeometryBuilder builder = new GeometryBuilder();
            
            PopulateSectorData(map, builder);
            PopulateLineData(map, builder);
            
            BspTree? bspTree = BspTree.Create(map, builder);
            if (bspTree == null)
                return null;
            
            // TODO: Connect subsector to sectors, and subsector segments to sides (and/or lines)?

            return new MapGeometry(builder, bspTree);
        }

        private static SectorPlane CreateAndAddPlane(DoomSector doomSector, List<SectorPlane> sectorPlanes, SectorPlaneFace face)
        {
            int id = sectorPlanes.Count;
            double z = (face == SectorPlaneFace.Floor ? doomSector.FloorZ : doomSector.CeilingZ);
            string texture = (face == SectorPlaneFace.Floor ? doomSector.FloorTexture : doomSector.CeilingTexture);
            
            SectorPlane sectorPlane = new SectorPlane(id, face, z, texture, doomSector.LightLevel);
            sectorPlanes.Add(sectorPlane);
            
            return sectorPlane;
        }
        
        private static void PopulateSectorData(DoomMap map, GeometryBuilder builder)
        {
            foreach (DoomSector doomSector in map.Sectors)
            {
                SectorPlane floorPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Floor);
                SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, builder.SectorPlanes, SectorPlaneFace.Ceiling);

                Sector sector = new Sector(builder.Sectors.Count, doomSector.Id, doomSector.Tag, 
                    doomSector.LightLevel, floorPlane, ceilingPlane);
                builder.Sectors.Add(sector);
            }
        }

        private static (Side front, Side back) CreateSingleSide(DoomLine doomLine, GeometryBuilder builder, 
            ref int nextSideId)
        {
            DoomSide doomSide = doomLine.Front;
            
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(doomSide.Sector.Id < builder.Sectors.Count, "Sector ID mapping broken");
            Sector sector = builder.Sectors[doomSide.Sector.Id];

            // When we get to 3D floors we're going to have to fix this...
            Wall wall = new Wall(builder.Walls.Count, doomSide.MiddleTexture, WallLocation.Middle);
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
            
            Wall middle = new Wall(builder.Walls.Count, facingSide.MiddleTexture, WallLocation.Middle);
            Wall upper = new Wall(builder.Walls.Count + 1, facingSide.UpperTexture, WallLocation.Upper);
            Wall lower = new Wall(builder.Walls.Count + 2, facingSide.LowerTexture, WallLocation.Lower);
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
                
                Line line = new Line(builder.Lines.Count, doomLine.Id, seg, front, back, flags);
                builder.Lines.Add(line);
            }
        }
    }
}