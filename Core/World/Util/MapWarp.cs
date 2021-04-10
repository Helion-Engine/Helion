using Helion.Resources.Definitions.MapInfo;
using System.Text.RegularExpressions;

namespace Helion.World.Util
{
    public static class MapWarp
    {
        public static bool GetMap(string warpString, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
        {
            mapInfoDef = null;
            string[] items = warpString.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (items.Length == 0)
                return false;

            int episode = 1;
            int level;
            if (items.Length > 1)
            {
                if (!int.TryParse(items[0], out episode))
                    return false;
                if (!int.TryParse(items[1], out level))
                    return false;
            }
            else if (!int.TryParse(items[0], out level))
            {      
                return false;
            }

            return GetMap(episode, level, mapInfo, out mapInfoDef);
        }

        public static bool GetMap(int warp, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
        {
            int episode = warp / 10;
            int level = warp - episode * 10;
            return GetMap(episode, level, mapInfo, out mapInfoDef);
        }

        private static bool GetMap(int episode, int level, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
        {
            string mapName = string.Empty;
            int episodeIndex = episode - 1;
            if (episodeIndex >= 0 && episodeIndex < mapInfo.Episodes.Count)
            {
                Regex epRegex = new Regex(@"(?<episode>[^\s\d]+)\d+(?<map>[^\s\d]+)\d+");
                var episodeDef = mapInfo.Episodes[episodeIndex];
                var match = epRegex.Match(episodeDef.StartMap);
                if (match.Success)
                {
                    mapName = match.Groups["episode"].Value + episode.ToString() +
                        match.Groups["map"].Value + level.ToString();
                }
            }

            if (mapName.Length == 0)
            {
                Regex mapRegex = new Regex(@"(?<map>[^\s\d]+)\d+");
                var episodeDef = mapInfo.Episodes[0];
                var match = mapRegex.Match(episodeDef.StartMap);
                if (match.Success)
                    mapName = match.Groups["map"] + episode.ToString() + level.ToString();
            }

            mapInfoDef = mapInfo.GetMap(mapName);
            return mapInfoDef != null;
        }
    }
}
