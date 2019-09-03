using System.Collections.Generic;
using System.Linq;
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
    public class DoomGeometryBuilder : GeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static MapGeometry? Create(DoomMap map)
        {
            List<Sector> sectors = new List<Sector>();
            List<SectorSpan> sectorSpans = new List<SectorSpan>();
            List<SectorPlane> sectorPlanes = new List<SectorPlane>();
            PopulateSectorData(map, sectors, sectorSpans, sectorPlanes);
            
            List<Line> lines = new List<Line>();
            List<Side> sides = new List<Side>();
            List<Wall> walls = new List<Wall>();
            PopulateLineData(map, lines, sides, walls, sectors);
            
            BspTree? bspTree = BspTree.Create(map);
            if (bspTree == null)
                return null;
            
            // TODO: Connect subsector to sectors, and subsector segments to sides (and/or lines)?

            return new MapGeometry(lines, sides, walls, sectors, sectorSpans, sectorPlanes, bspTree);
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
        
        private static void PopulateSectorData(DoomMap map, List<Sector> sectors, List<SectorSpan> sectorSpans, 
            List<SectorPlane> sectorPlanes)
        {
            foreach (DoomSector doomSector in map.Sectors)
            {
                SectorPlane floorPlane = CreateAndAddPlane(doomSector, sectorPlanes, SectorPlaneFace.Floor);
                SectorPlane ceilingPlane = CreateAndAddPlane(doomSector, sectorPlanes, SectorPlaneFace.Ceiling);

                SectorSpan span = new SectorSpan(sectorSpans.Count, floorPlane, ceilingPlane);
                sectorSpans.Add(span);
                
                Sector sector = new Sector(sectors.Count, doomSector.Id, doomSector.Tag, doomSector.LightLevel, span);
                sectors.Add(sector);
            }
            
            Postcondition(sectors.Any(sector => sector.Id != sector.MapId), "Non-continuous sector allocation detected");
        }

        private static (Side front, Side back) CreateSingleSide(DoomLine doomLine, List<Side> sides, List<Sector> sectors,
            List<Wall> walls, ref int nextSideId)
        {
            DoomSide doomSide = doomLine.Front;
            
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(doomSide.Sector.Id < sectors.Count, "Sector ID mapping broken");
            Sector sector = sectors[doomSide.Sector.Id];

            // When we get to 3D floors we're going to have to fix this...
            Wall wall = new Wall(walls.Count, WallLocation.Middle);
            walls.Add(wall);
            
            Side front = new Side(nextSideId, doomSide.Id, doomSide.Offset, wall, sector);
            sides.Add(front);

            wall.Side = front;

            nextSideId++;
            
            return (front, null);
        }

        private static TwoSided CreateTwoSided(DoomSide facingSide, List<Sector> sectors, List<Side> sides, 
            List<Wall> walls, ref int nextSideId)
        {
            // This is okay because of how we create sectors corresponding
            // to their list index. If this is wrong then someone broke the
            // ordering very badly.
            Invariant(facingSide.Sector.Id < sectors.Count, "Sector (facing) ID mapping broken");
            Sector facingSector = sectors[facingSide.Sector.Id];
            
            Wall middle = new Wall(walls.Count, WallLocation.Middle);
            Wall upper = new Wall(walls.Count + 1, WallLocation.Upper);
            Wall lower = new Wall(walls.Count + 2, WallLocation.Lower);
            walls.Add(middle);
            walls.Add(upper);
            walls.Add(lower);
            
            TwoSided side = new TwoSided(nextSideId, facingSide.Id, facingSide.Offset, upper, middle, lower, facingSector);
            sides.Add(side);

            nextSideId++;
            
            return side;
        }

        private static (Side front, Side? back) CreateSides(DoomLine doomLine, List<Side> sides, List<Sector> sectors, 
            List<Wall> walls, ref int nextSideId)
        {
            if (doomLine.Back == null)
                return CreateSingleSide(doomLine, sides, sectors, walls, ref nextSideId);

            TwoSided front = CreateTwoSided(doomLine.Front, sectors, sides, walls, ref nextSideId);
            TwoSided back = CreateTwoSided(doomLine.Back, sectors, sides, walls, ref nextSideId);
            return (front, back);
        }

        private static void PopulateLineData(DoomMap map, List<Line> lines, List<Side> sides, List<Wall> walls, 
            List<Sector> sectors)
        {
            int nextSideId = 0;
            
            foreach (DoomLine doomLine in map.Lines)
            {
                if (doomLine.Start.Position == doomLine.End.Position)
                {
                    Log.Warn("Zero length linedef pruned (id = {0})", doomLine.Id);
                    continue;
                }
                
                (Side front, Side? back) = CreateSides(doomLine, sides, sectors, walls, ref nextSideId);

                Seg2D seg = new Seg2D(doomLine.Start.Position, doomLine.End.Position);
                LineFlags flags = new LineFlags(doomLine.Flags);
                
                Line line = new Line(lines.Count, doomLine.Id, seg, front, back, flags);
                lines.Add(line);
            }
        }
    }
}