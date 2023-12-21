using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Bsp;
using Helion.Maps.Bsp.Builder.GLBSP;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using NLog;

namespace Helion.World.Geometry.Builder;

public class GeometryBuilder
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly List<Line> Lines;
    public readonly List<Side> Sides;
    public readonly List<Wall> Walls;
    public readonly List<Sector> Sectors;
    public readonly List<SectorPlane> SectorPlanes;
        
    private static (CompactBspTree, BspTreeNew)? m_lastBspTree;
    private static string m_lastMap = string.Empty;
    private static string m_lastArchive = string.Empty;


    internal GeometryBuilder(IMap map)
    {
        Lines = new(map.GetLines().Count);
        Sides = new(map.GetSides().Count);
        Walls = new(map.GetSides().Count * 3);
        Sectors = new(map.GetSectors().Count);
        SectorPlanes = new(map.GetSectors().Count * 2);
    }

    public static MapGeometry? Create(IMap map, IConfig config, TextureManager textureManager)
    {
        IBspBuilder? bspBuilder = CreateBspBuilder(map, config);
        if (bspBuilder == null)
            return null;

        GeometryBuilder geometryBuilder = new(map);
        switch (map)
        {
            case DoomMap doomMap:
                return DoomGeometryBuilder.Create(doomMap, geometryBuilder, textureManager, CreateBspTree);
            case HexenMap hexenMap:
                return HexenGeometryBuilder.Create(hexenMap, geometryBuilder, textureManager, CreateBspTree);
            default:
                Log.Error("Do not support map type {0} yet", map.MapType);
                return null;
        }

        (CompactBspTree, BspTreeNew)? CreateBspTree()
        {
            if (map.Name.Length > 0 && m_lastMap.EqualsIgnoreCase(map.Name) &&
                m_lastArchive.Equals(map.Archive.MD5) &&
                m_lastBspTree != null)
            {
                RemapBspTree(m_lastBspTree.Value.Item1, geometryBuilder);
                return (m_lastBspTree.Value.Item1, m_lastBspTree.Value.Item2);
            }

            CompactBspTree? bspTree;
            m_lastBspTree = null;
            try
            {
                bspTree = CompactBspTree.Create(map, geometryBuilder, bspBuilder);
                if (bspTree == null)
                    return null;
            }
            catch
            {
                Log.Error("Unable to load map, BSP tree cannot be built due to corrupt geometry");
                return null;
            }

            m_lastMap = map.Name;
            m_lastArchive = map.Archive.MD5;
            m_lastBspTree = (bspTree, new BspTreeNew(map, geometryBuilder.Lines, geometryBuilder.Sectors));
            return m_lastBspTree;
        }
    }

    private static void RemapBspTree(CompactBspTree bspTree, GeometryBuilder geometryBuilder)
    {
        // Sectors are recreated so they need to be remapped from the new builder
        for (int i = 0; i < bspTree.Subsectors.Length; i++)
        {
            var subsector = bspTree.Subsectors[i];
            subsector.Sector = geometryBuilder.Sectors[subsector.Sector.Id];
        }
    }

    private static IBspBuilder? CreateBspBuilder(IMap map, IConfig config)
    {
        if (map.GL != null)
            return new GLBspBuilder(map);

        Log.Warn("Unable to find GL nodes from ZDBSP");
        return null;
    }
}
