using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Helion.World.Util;

public static class MapWarp
{
    public static bool GetMap(string warpString, ArchiveCollection archiveCollection, [NotNullWhen(true)] out MapInfoDef? mapInfoDef)
    {
        mapInfoDef = null;
        if (warpString.Contains(' '))
        {
            string[] items = warpString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (items.Length >= 2 && int.TryParse(items[0], out int ep) && int.TryParse(items[1], out int level))
                return GetMap(ep, level, archiveCollection, out mapInfoDef);

            return false;
        }

        if (!int.TryParse(warpString, out int warp))
            return false;

        return GetMap(warp, archiveCollection, out mapInfoDef);
    }

    public static bool GetMap(int warp, ArchiveCollection archiveCollection, [NotNullWhen(true)] out MapInfoDef? mapInfoDef)
    {
        int episode = warp / 10;
        int level = warp - episode * 10;
        return GetMap(episode, level, archiveCollection, out mapInfoDef);
    }

    private static bool GetMap(int episode, int level, ArchiveCollection archiveCollection, [NotNullWhen(true)] out MapInfoDef? mapInfoDef)
    {
        var mapInfo = archiveCollection.Definitions.MapInfoDefinition.MapInfo;
        if (GetMapName(episode, level, mapInfo, out mapInfoDef))
            return true;

        if (GetEpisodeMapName(episode, level, mapInfo, out mapInfoDef))
            return true;

        if (GetMapNameString(episode, level, mapInfo, out var mapName) && archiveCollection.FindMap(mapName) != null)
        {
            mapInfoDef = mapInfo.GetMapInfoOrDefault(mapName);
            return true;
        }

        if (GetEpisodeMapNameString(episode, level, mapInfo, out mapName) && archiveCollection.FindMap(mapName) != null)
        {
            mapInfoDef = mapInfo.GetMapInfoOrDefault(mapName);
            return true;
        }

        return false;
    }

    private static bool GetMapName(int episode, int level, MapInfo mapInfo, [NotNullWhen(true)]  out MapInfoDef? mapInfoDef)
    {
        if (GetMapNameString(episode, level, mapInfo, out var mapName))
        {
            mapInfoDef = mapInfo.GetMap(mapName).MapInfo;
            return mapInfoDef != null;
        }

        mapInfoDef = null;
        return false;
    }

    private static bool GetMapNameString(int episode, int level, MapInfo mapInfo, [NotNullWhen(true)] out string? mapName)
    {
        string startMap = "map01";        
        if (mapInfo.Episodes.Count > 0 && !MapInfo.IsWarpTrans(mapInfo.Episodes[0].StartMap))
            startMap = mapInfo.Episodes[0].StartMap;

        Regex mapRegex = new Regex(@"(?<map>[^\s\d]+)\d+");
        var match = mapRegex.Match(startMap);
        if (match.Success)
        {
            mapName = match.Groups["map"] + episode.ToString() + level.ToString();
            return true;
        }
        

        mapName = null;
        return false;
    }

    private static bool GetEpisodeMapName(int episode, int level, MapInfo mapInfo, [NotNullWhen(true)] out MapInfoDef? mapInfoDef)
    {
        if (GetEpisodeMapNameString(episode, level, mapInfo, out var mapName))
        {
            mapInfoDef = mapInfo.GetMap(mapName).MapInfo;
            return mapInfoDef != null;
        }

        mapInfoDef = null;
        return false;
    }

    private static readonly Regex EpisodeRegex = new(@"(?<episode>[^\s\d]+)\d+(?<map>[^\s\d]+)\d+", RegexOptions.Compiled);

    private static bool GetEpisodeMapNameString(int episode, int level, MapInfo mapInfo, [NotNullWhen(true)] out string? mapName)
    {
        episode = Math.Clamp(episode, 1, int.MaxValue);
        int episodeIndex = episode - 1;
        string startMap = "e1m1";

        if (episodeIndex >= 0 && episodeIndex < mapInfo.Episodes.Count)
            startMap = mapInfo.Episodes[episodeIndex].StartMap;
        
        var match = EpisodeRegex.Match(startMap);
        if (match.Success)
        {
            mapName = match.Groups["episode"].Value + episode.ToString() +
                match.Groups["map"].Value + level.ToString();
            return true;
        }    

        mapName = null;
        return false;
    }
}
