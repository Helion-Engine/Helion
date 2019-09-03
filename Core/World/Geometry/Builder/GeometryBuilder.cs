using Helion.Maps;
using Helion.Maps.Doom;
using NLog;

namespace Helion.World.Geometry.Builder
{
    public abstract class GeometryBuilder
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static MapGeometry? Create(IMap map)
        {
            switch (map)
            {
            case DoomMap doomMap:
                return DoomGeometryBuilder.Create(doomMap);
            default:
                Log.Error("Do not support map type {0} yet", map.MapType);
                return null;
            }
        }
    }
}