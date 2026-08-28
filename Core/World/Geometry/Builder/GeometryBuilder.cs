using System.Collections.Generic;
using System.Diagnostics;
using Helion.Maps;
using Helion.Maps.Bsp;
using Helion.Maps.Bsp.Builder.GLBSP;
using Helion.Maps.Doom;
using Helion.Maps.Hexen;
using Helion.Maps.Udmf;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.World.Bsp;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Sides;
using NLog;

namespace Helion.World.Geometry.Builder;

public readonly record struct CreateBspTreeResult(CompactBspTree BspTree);

public class GeometryBuilder
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public readonly List<Line> Lines;
    public readonly List<Side> Sides;
    public readonly List<Sector> Sectors;

    internal GeometryBuilder(IMap map)
    {
        Lines = new(map.GetLines().Count);
        Sides = new(map.GetSides().Count);
        Sectors = new(map.GetSectors().Count);
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
            case UdmfMap udmfMap:
                return UdmfGeometryBuilder.Create(udmfMap, geometryBuilder, textureManager, CreateBspTree);
            default:
                Log.Error("Do not support map type {0} yet", map.MapType);
                return null;
        }

        CreateBspTreeResult? CreateBspTree()
        {
            CompactBspTree? bspTree;
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

            return new(bspTree);
        }
    }

    private static GLBspBuilder? CreateBspBuilder(IMap map, IConfig config)
    {
        if (map.GL != null)
            return new GLBspBuilder(map);

        Log.Warn("Unable to find GL nodes");
        return null;
    }
}
