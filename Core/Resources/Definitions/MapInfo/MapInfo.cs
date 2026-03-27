using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Helion.Maps.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util.Extensions;
using Helion.World.Util;

namespace Helion.Resources.Definitions.MapInfo;

public class MapInfo
{
    public static readonly HashSet<string> EndGameMaps = new(StringComparer.OrdinalIgnoreCase)
    {
        "EndPic", "EndGame1", "EndGame2", "EndGameW", "EndGame4", "EndGameC", "EndGame3",
        "EndDemon", "EndGameS", "EndChess", "EndTitle", "EndSequence", "EndBunny", "EndGame"
    };

    const string WarpTrans = "&wt@";

    public IReadOnlyList<EpisodeDef> Episodes => m_episodes.AsReadOnly();
    public IReadOnlyList<MapInfoDef> Maps => m_maps.AsReadOnly();
    public IReadOnlyList<ClusterDef> Clusters => m_clusters.AsReadOnly();
    public IReadOnlyList<SkillDef> Skills => m_skills.AsReadOnly();
    public MapInfoDef? DefaultMap { get; private set; }

    private readonly List<EpisodeDef> m_episodes = [];
    private readonly List<MapInfoDef> m_maps = [];
    private readonly List<ClusterDef> m_clusters = [];
    private readonly List<SkillDef> m_skills = [];
    private readonly List<MapInfoDef> m_orderedMaps = [];
    private bool m_builtOrderedMaps;

    public void ClearEpisodes() => m_episodes.Clear();

    public void AddEpisode(EpisodeDef episode) =>
        AddOrReplace(m_episodes, episode);

    public void RemoveEpisodeByMapName(string mapName)
    {
        var episode = m_episodes.FirstOrDefault(x => x.StartMap.EqualsIgnoreCase(mapName));
        if (episode != null)
            m_episodes.Remove(episode);
    }

    public void AddOrReplaceMap(MapInfoDef newMap)
        => AddOrReplace(m_maps, newMap);

    public void AddCluster(ClusterDef newCluster)
        => AddOrReplace(m_clusters, newCluster);

    public void RemoveCluster(int clusterNum)
    {
        if (TryGetCluster(clusterNum, out ClusterDef? clusterDef))
            m_clusters.Remove(clusterDef);
    }

    public void AddSkill(SkillDef skill) => m_skills.Add(skill);

    public void ClearSkills() => m_skills.Clear();

    public SkillDef? GetSkill(SkillLevel skill)
    {
        if (skill == SkillLevel.None)
            return m_skills.FirstOrDefault(x => x.Default);

        int iSkill = (int)skill - 1;
        if (iSkill < 0 || iSkill >= m_skills.Count)
            return null;

        return m_skills[iSkill];
    }

    public SkillLevel GetSkillLevel(SkillDef skillDef)
    {
        for (int i = 0; i < m_skills.Count; i++)
        {
            if (m_skills[i].Name.EqualsIgnoreCase(skillDef.Name))
                return (SkillLevel)(i + (int)SkillLevel.VeryEasy);
        }

        return SkillLevel.None;
    }

    public int GetNewClusterNumber()
    {
        if (m_clusters.Count == 0)
            return 1;

        return m_clusters.Max(x => x.ClusterNum) + 1;
    }

    public bool TryGetCluster(int clusterNum, [NotNullWhen(true)] out ClusterDef? clusterDef)
    {
        clusterDef = m_clusters.FirstOrDefault(x => x.ClusterNum == clusterNum);
        return clusterDef != null;
    }

    public MapInfoDef GetMapInfoOrDefault(string mapName)
    {
        MapInfoDef? mapInfoDef = m_maps.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(mapName));
        if (mapInfoDef != null)
            return mapInfoDef;

        if (DefaultMap == null)
            return new MapInfoDef() { MapName = mapName.ToUpperInvariant() };

        mapInfoDef = (MapInfoDef)DefaultMap.Clone();
        mapInfoDef.MapName = mapName.ToUpperInvariant();
        return mapInfoDef;
    }

    public void SetDefaultMap(MapInfoDef? map) => DefaultMap = map;
    public FindMapResult GetNextMap(MapInfoDef map) => GetMap(map.Next);
    public FindMapResult GetNextSecretMap(MapInfoDef map) => map.SecretNext != "" ? GetMap(map.SecretNext) : GetMap(map.Next);
    public ClusterDef? GetCluster(int clusterNumber) => m_clusters.FirstOrDefault(c => c.ClusterNum == clusterNumber);
    public static bool IsWarpTrans(string mapName) => mapName.StartsWithIgnoreCase(WarpTrans);

    public FindMapResult GetMap(string name)
    {
        if (EndGameMaps.Contains(name))
            return FindMapResult.CreateEndGame(name);

        var map = m_maps.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(name));
        if (map != null)
            return FindMapResult.Create(map, name);

        if (int.TryParse(name, out int mapNum))
        {
            var findMapName = "MAP" + mapNum;
            map = m_maps.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(findMapName));
            if (map != null)
                return FindMapResult.Create(map, name);
        }

        return FindMapResult.CreateMapNameError(name);
    }

    public MapInfoDef GetStartMapOrDefault(ArchiveCollection archiveCollection, string mapName)
    {
        if (IsWarpTrans(mapName) && MapWarp.GetMap(mapName[WarpTrans.Length..], archiveCollection, out var mapInfoDef))
            return mapInfoDef;

        return GetMapInfoOrDefault(mapName);
    }

    public bool TryGetMapByLevelNumber(int number, [NotNullWhen(true)] out MapInfoDef? mapInfo)
    {
        foreach (var map in Maps)
        {
            if (map.LevelNumber == number)
            {
                mapInfo = map;
                return true;
            }
        }

        mapInfo = null;
        return false;
    }

    public bool IsChangingClusters(MapInfoDef mapDef, FindMapResult nextMapResult, bool secret, out ClusterDef? cluster, out ClusterDef? nextCluster)
    {
        var nextMapInfo = nextMapResult.MapInfo;
        cluster = GetCluster(mapDef.Cluster);
        nextCluster = null;
        if (nextMapInfo != null)
            nextCluster = GetCluster(nextMapInfo.Cluster);

        if (mapDef.ClusterDef != null)
            cluster = mapDef.ClusterDef;

        bool isChangingClusters = cluster != null && nextCluster != null && cluster != nextCluster;
        if (cluster != null && isChangingClusters)
        {
            bool hasExitText = secret ? cluster.SecretExitText.Count > 0 : cluster.ExitText.Count > 0;
            if (!hasExitText && nextCluster == null)
                isChangingClusters = false;
            if (!hasExitText && nextCluster != null && nextCluster.EnterText.Count == 0)
                isChangingClusters = false;
        }

        return isChangingClusters || mapDef.IsEndGame || (nextMapResult.Options & FindMapResultOptions.EndGame) != 0;
    }

    private static void AddOrReplace<T>(List<T> items, T newItem)
    {
        if (newItem == null)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            T item = items[i];
            if (newItem.Equals(item))
            {
                items[i] = newItem;
                return;
            }
        }

        items.Add(newItem);
    }

    /// <summary>
    /// The maps in play order, with secrets exits before regular exits.
    /// </summary>
    public List<MapInfoDef> GetOrderedMaps()
    {
        if (m_builtOrderedMaps)
            return m_orderedMaps;

        foreach (var episode in m_episodes)
        {
            Stack<string> mapStack = new();
            mapStack.Push(episode.StartMap);
            while (mapStack.Count > 0)
            {
                string mapName = mapStack.Pop();
                var map = m_maps.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(mapName));
                if (map != null && !m_orderedMaps.Contains(map))
                {
                    m_orderedMaps.Add(map);
                    mapStack.Push(map.Next);
                    if (map.SecretNext != "")
                        mapStack.Push(map.SecretNext);
                }
                // if a map loops only to itself it ends the stack, so continue at the first map we haven't seen
                if (mapStack.Count == 0 && m_orderedMaps.Count < m_maps.Count)
                {
                    // LevelNumber not necessarily set, fall back to MapName
                    var nextMap = m_maps.OrderBy(x => x.LevelNumber).ThenBy(x => x.MapName).FirstOrDefault(x => !m_orderedMaps.Contains(x));
                    if (nextMap != null)
                        mapStack.Push(nextMap.MapName);
                }
            }
        }

        m_builtOrderedMaps = true;
        return m_orderedMaps;
    }

    public MapInfoDef? GetEpisodeEndGame(MapInfoDef currentMap)
    {
        var maps = GetOrderedMaps();
        for (int i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            if (currentMap == map)
            {
                for (int j = i; j < maps.Count; j++)
                {
                    map = maps[j];
                    if (map.IsEndGame)
                        return map;
                }
            }
        }

        return null;
    }
}
