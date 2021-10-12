using Helion.Resources.Definitions.MapInfo;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Helion.World.Util;

public static class MapWarp
{
    public static bool GetMap(string warpString, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
    {
        mapInfoDef = null;
        if (warpString.Contains(' '))
        {
            string[] items = warpString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length >= 2 && int.TryParse(items[0], out int ep) && int.TryParse(items[1], out int level))
                return GetMap(ep, level, mapInfo, out mapInfoDef);

            return false;
        }

        if (!int.TryParse(warpString, out int warp))
            return false;

        return GetMap(warp, mapInfo, out mapInfoDef);
    }

    public static bool GetMap(int warp, MapInfo mapInfo, [NotNullWhen(true)] out MapInfoDef? mapInfoDef)
    {
        int episode = warp / 10;
        int level = warp - episode * 10;
        return GetMap(episode, level, mapInfo, out mapInfoDef);
    }

    private static bool GetMap(int episode, int level, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
    {
        if (!GetMapName(episode, level, mapInfo, out mapInfoDef))
            GetEpisodeMapName(episode, level, mapInfo, out mapInfoDef);

        return mapInfoDef != null;
    }

    private static bool GetMapName(int episode, int level, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
    {
        mapInfoDef = null;
        if (mapInfo.Episodes.Count > 0)
        {
            Regex mapRegex = new Regex(@"(?<map>[^\s\d]+)\d+");
            var episodeDef = mapInfo.Episodes[0];
            var match = mapRegex.Match(episodeDef.StartMap);
            if (match.Success)
            {
                string mapName = match.Groups["map"] + episode.ToString() + level.ToString();
                mapInfoDef = mapInfo.GetMap(mapName);
                return mapInfoDef != null;
            }
        }

        return false;
    }

    private static bool GetEpisodeMapName(int episode, int level, MapInfo mapInfo, out MapInfoDef? mapInfoDef)
    {
        mapInfoDef = null;
        episode = Math.Clamp(episode, 1, int.MaxValue);
        int episodeIndex = episode - 1;
        if (episodeIndex >= 0 && episodeIndex < mapInfo.Episodes.Count)
        {
            Regex epRegex = new Regex(@"(?<episode>[^\s\d]+)\d+(?<map>[^\s\d]+)\d+");
            var episodeDef = mapInfo.Episodes[episodeIndex];
            var match = epRegex.Match(episodeDef.StartMap);
            if (match.Success)
            {
                string mapName = match.Groups["episode"].Value + episode.ToString() +
                    match.Groups["map"].Value + level.ToString();
                mapInfoDef = mapInfo.GetMap(mapName);
                return mapInfoDef != null;
            }
        }

        return false;
    }
}
