using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Maps;
using Helion.Maps.Components.Linedefs;
using Helion.Maps.Components.Sectors;
using Helion.Maps.Components.Sidedefs;
using Helion.Maps.Specials;
using Helion.Maps.Specials.Compatibility;
using Helion.Maps.Specials.Vanilla;
using Helion.Maps.Specials.ZDoom;
using Helion.Resource;
using Helion.Util;
using Helion.Util.Geometry.Segments;
using Helion.Worlds.Bsp;
using Helion.Worlds.Geometry.Lines;
using Helion.Worlds.Geometry.Sectors;
using Helion.Worlds.Geometry.Walls;
using Helion.Worlds.Special;
using Helion.Worlds.Textures;
using NLog;
using MapSector = Helion.Maps.Components.Sectors.Sector;
using Sector = Helion.Worlds.Geometry.Sectors.Sector;

namespace Helion.Worlds.Geometry.Builder
{
    /// <summary>
    /// A helper for converting a doom map into the internal map geometry
    /// definitions.
    /// </summary>
    public static class DoomGeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Creates a new map from geometry that is a doom map.
        /// </summary>
        /// <param name="map">The map data.</param>
        /// <param name="textureManager">The world texture manager.</param>
        /// <returns>A new map geometry object from the doom map, or null if
        /// there was map corruption.</returns>
        public static MapGeometry? Create(Map map, WorldTextureManager textureManager)
        {
            GeometryBuilder builder = new();

            PopulateSectors(map, builder, textureManager);
            PopulateLines(map, builder, textureManager);

            BspTree? bspTree = GLBspTreeBuilder.Build(map, builder.Lines);
            if (bspTree == null)
            {
                Log.Error("Unable to build BSP tree for map {0}", map.Name);
                return null;
            }

            builder.BspTree = bspTree;

            return new MapGeometry(builder);
        }

        private static void PopulateSectors(Map map, GeometryBuilder builder, WorldTextureManager textureManager)
        {
            foreach (DoomSector mapSector in map.Sectors.Cast<DoomSector>())
            {
                int index = builder.Sectors.Count;
                SectorPlane floor = CreateAndAddPlane(mapSector, builder.SectorPlanes, SectorPlaneFace.Floor, textureManager);
                SectorPlane ceiling = CreateAndAddPlane(mapSector, builder.SectorPlanes, SectorPlaneFace.Ceiling, textureManager);
                VanillaSectorSpecialType specialType = (VanillaSectorSpecialType)mapSector.SectorType;
                ZDoomSectorSpecialType special = VanillaSectorSpecTranslator.Translate(specialType);

                Sector sector = new(index, mapSector.Tag, mapSector.LightLevel, floor, ceiling, special);
                builder.Sectors.Add(sector);
            }
        }

        private static SectorPlane CreateAndAddPlane(MapSector mapSector, List<SectorPlane> planes, SectorPlaneFace face,
            WorldTextureManager textureManager)
        {
            int index = planes.Count;
            double z = (face == SectorPlaneFace.Floor ? mapSector.FloorZ : mapSector.CeilingZ);
            CIString textureName = (face == SectorPlaneFace.Floor ? mapSector.FloorTexture : mapSector.CeilingTexture);
            IWorldTexture texture = textureManager.Get(textureName, Namespace.Flats);

            SectorPlane sectorPlane = new(index, face, z, texture, mapSector.LightLevel);
            planes.Add(sectorPlane);

            return sectorPlane;
        }

        private static Side CreateSingleSide(DoomLinedef linedef, GeometryBuilder builder,
            WorldTextureManager textureManager, ref int nextSideId)
        {
            Sidedef sidedef = linedef.Front;
            Sector sector = builder.Sectors[sidedef.Sector.Index];
            IWorldTexture texture = textureManager.Get(sidedef.MiddleTexture, Namespace.Textures);

            Wall wall = new(builder.Walls.Count, texture, WallLocation.Middle);
            builder.Walls.Add(wall);

            Side front = new(nextSideId, sidedef.Offset, wall, sector);
            builder.Sides.Add(front);

            wall.Side = front;
            nextSideId++;

            return front;
        }

        private static Side CreateTwoSided(Sidedef sidedef, GeometryBuilder builder,
            WorldTextureManager textureManager, ref int nextSideId)
        {
            Sector facingSector = builder.Sectors[sidedef.Sector.Index];

            IWorldTexture upperTexture = textureManager.Get(sidedef.UpperTexture, Namespace.Textures);
            IWorldTexture middleTexture = textureManager.Get(sidedef.MiddleTexture, Namespace.Textures);
            IWorldTexture lowerTexture = textureManager.Get(sidedef.LowerTexture, Namespace.Textures);

            Wall upper = new(builder.Walls.Count, upperTexture, WallLocation.Upper);
            Wall middle = new(builder.Walls.Count + 1, middleTexture, WallLocation.Middle);
            Wall lower = new(builder.Walls.Count + 2, lowerTexture, WallLocation.Lower);
            builder.Walls.Add(middle);
            builder.Walls.Add(upper);
            builder.Walls.Add(lower);

            Side side = new(nextSideId, sidedef.Offset, upper, middle, lower, facingSector);
            builder.Sides.Add(side);

            nextSideId++;

            return side;
        }

        private static (Side front, Side? back) CreateSides(DoomLinedef linedef, GeometryBuilder builder,
            WorldTextureManager textureManager, ref int nextSideId)
        {
            if (linedef.Back == null)
                return (CreateSingleSide(linedef, builder, textureManager, ref nextSideId), null);

            Side front = CreateTwoSided(linedef.Front, builder, textureManager, ref nextSideId);
            Side back = CreateTwoSided(linedef.Back, builder, textureManager, ref nextSideId);
            return (front, back);
        }

        private static void PopulateLines(Map map, GeometryBuilder builder, WorldTextureManager textureManager)
        {
            int nextSideId = 0;

            foreach (DoomLinedef linedef in map.Linedefs.Cast<DoomLinedef>())
            {
                if (linedef.Start == linedef.End)
                {
                    Log.Warn("Zero length linedef pruned (index: {0})", linedef.Index);
                    continue;
                }

                (Side front, Side? back) = CreateSides(linedef, builder, textureManager, ref nextSideId);

                Seg2D seg = new(linedef.Start, linedef.End);
                LineFlags flags = new(linedef.Flags);
                SpecialArgs specialArgs = new();
                ZDoomLineSpecialType zdoomType = VanillaLineSpecTranslator.Translate(flags, linedef.LineType, (byte)linedef.SectorTag, ref specialArgs, out LineSpecialCompatibility? compatibility);
                LineActivationType activationType = VanillaLineSpecTranslator.GetLineTagActivation(linedef.LineType);
                LineSpecial special = new(zdoomType, activationType, compatibility);

                Line line = new(builder.Lines.Count, linedef.Index, seg, front, back, flags, special, specialArgs);
                builder.Lines.Add(line);
                builder.MapLines[line.MapId] = line;
            }
        }
    }
}