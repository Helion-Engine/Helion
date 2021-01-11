using System;

namespace Helion.Resources.Definitions.MapInfo
{
    public class MapInfoDef
    {
        public string Map { get; set; } = string.Empty;
        public string MapName { get; set; } = string.Empty;
        public string TitlePatch { get; set; } = string.Empty;
        public string Next { get; set; } = string.Empty;
        public string SecretNext { get; set; } = string.Empty;
        public SkyDef Sky1 { get; set; } = new();
        public SkyDef Sky2 { get; set; } = new();
        public string Music { get; set; } = string.Empty;
        public string LookupName { get; set; } = string.Empty;
        public string NiceName { get; set; } = string.Empty;
        public int LevelNumber { get; set; }
        public int Cluster { get; set; }
        public int ParTime { get; set; }
        public int SuckTime { get; set; }
        public MapSpecial MapSpecial { get; set; }
        public MapSpecialAction MapSpecialAction { get; set; }
        public MapOptions MapOptions { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj is MapInfoDef map && map.MapName.Equals(MapName, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return MapName.ToUpper().GetHashCode();
        }
    }
}
