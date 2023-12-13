using System;
using System.Collections.Generic;
using Helion.Maps;
using Helion.Maps.Bsp;
using Helion.Maps.Bsp.Builder.GLBSP;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using Helion.World.Geometry.Walls;
using NLog;

namespace Helion.World.Geometry.Builder;

/// <summary>
/// A helper class for making the geometry in a map.
/// </summary>
public class GeometryBuilder
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly List<Line> Lines;
    public readonly List<Side> Sides;
    public readonly List<Wall> Walls;
    public readonly List<Sector> Sectors;
    public readonly List<SectorPlane> SectorPlanes;

    /// <summary>
    /// A cached dictionary that maps the line ID number onto a line from
    /// the map this was parsed from.
    /// </summary>
    /// <remarks>
    /// We need this for the BSP builder to work properly because it does
    /// references by map line ID. It also can't be a dictionary because
    /// the line IDs in a map are not guaranteed to be contiguous due to
    /// map corruption, line removal, etc.
    /// </remarks>
    public readonly Dictionary<int, Line> MapLines;
        
    private static CompactBspTree? m_lastBspTree;
    private static string m_lastMap = string.Empty;

    internal GeometryBuilder(IMap map)
    {
        Lines = new(map.GetLines().Count);
        Sides = new(map.GetSides().Count);
        Walls = new(map.GetSides().Count * 3);
        Sectors = new(map.GetSectors().Count);
        SectorPlanes = new(map.GetSectors().Count * 2);
        MapLines = new(map.GetLines().Count);
    }

    /// <summary>
    /// Creates world geometry from a map.
    /// </summary>
    /// <param name="map">The map to turn into world geometry.</param>
    /// <param name="config">The player config data.</param>
    /// <param name="textureManager">TextureManager.</param>
    /// <returns>A map geometry object if it was parsed and created right,
    /// otherwise null if it failed.</returns>
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

        CompactBspTree? CreateBspTree()
        {
            if (map.Name.Length > 0 && m_lastMap == map.Name && m_lastBspTree != null)
            {
                RemapBspTree(m_lastBspTree, geometryBuilder);
                return m_lastBspTree;
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
            m_lastBspTree = bspTree;
            return bspTree;
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
