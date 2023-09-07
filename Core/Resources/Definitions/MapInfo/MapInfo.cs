using Helion.Maps.Shared;
using Helion.Util.Extensions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Helion.Resources.Definitions.MapInfo;

public class MapInfo
{
    public IReadOnlyList<EpisodeDef> Episodes => m_episodes.AsReadOnly();
    public IReadOnlyList<MapInfoDef> Maps => m_maps.AsReadOnly();
    public IReadOnlyList<ClusterDef> Clusters => m_clusters.AsReadOnly();
    public IReadOnlyList<SkillDef> Skills => m_skills.AsReadOnly();
    public MapInfoDef? DefaultMap { get; private set; }

    private readonly List<EpisodeDef> m_episodes = new();
    private readonly List<MapInfoDef> m_maps = new();
    private readonly List<ClusterDef> m_clusters = new();
    private readonly List<SkillDef> m_skills = new ();

    public void ClearEpisodes() => m_episodes.Clear();

    public void AddEpisode(EpisodeDef episode) =>
        AddOrReplace(m_episodes, episode);

    public void RemoveEpisodeByMapName(string mapName)
    {
        var episode = m_episodes.FirstOrDefault(x => x.StartMap.EqualsIgnoreCase(mapName));
        if (episode != null)
            m_episodes.Remove(episode);
    }

    public void AddMap(MapInfoDef newMap)
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
            return 0;

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

        mapInfoDef =  (MapInfoDef)DefaultMap.Clone();
        mapInfoDef.MapName = mapName.ToUpperInvariant();
        return mapInfoDef;
    }

    public void SetDefaultMap(MapInfoDef? map) => DefaultMap = map;
    public MapInfoDef? GetNextMap(MapInfoDef map) => GetMap(map.Next);
    public MapInfoDef? GetNextSecretMap(MapInfoDef map) => GetMap(map.SecretNext);
    public MapInfoDef? GetMap(string name) => m_maps.FirstOrDefault(x => x.MapName.EqualsIgnoreCase(name));
    public ClusterDef? GetCluster(int clusterNumber) => m_clusters.FirstOrDefault(c => c.ClusterNum == clusterNumber);

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
                break;
            }
        }

        items.Add(newItem);
    }
}
