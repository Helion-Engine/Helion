using Helion.Maps.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Resources.Definitions.MapInfo
{
    public class MapInfo
    {
        public IReadOnlyList<EpisodeDef> Episodes => m_episodes.AsReadOnly();
        public IReadOnlyList<MapInfoDef> Maps => m_maps.AsReadOnly();
        public IReadOnlyList<ClusterDef> Clusters => m_clusters.AsReadOnly();
        public IReadOnlyList<SkillDef> Skills => m_skills.AsReadOnly();
        public MapInfoDef DefaultMap { get; private set; } = new();

        private readonly List<EpisodeDef> m_episodes = new List<EpisodeDef>();
        private readonly List<MapInfoDef> m_maps = new List<MapInfoDef>();
        private readonly List<ClusterDef> m_clusters = new List<ClusterDef>();
        private readonly List<SkillDef> m_skills = new List<SkillDef>();

        public void ClearEpisodes() => m_episodes.Clear();

        public void AddEpisode(EpisodeDef episode) => m_episodes.Add(episode);

        public void AddMap(MapInfoDef newMap)
            => AddOrReplace(m_maps, newMap);

        public void AddCluster(ClusterDef newCluster)
            => AddOrReplace(m_clusters, newCluster);

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

        public List<MapInfoDef> GetMaps(EpisodeDef episode)
        {
            int index = m_episodes.FindIndex(x => x == episode);
            return m_maps.Where(x => x.Cluster == index).ToList();
        }

        public MapInfoDef GetMapInfoOrDefault(string mapName)
        {
            MapInfoDef? mapInfoDef = m_maps.FirstOrDefault(x => x.MapName.Equals(mapName, StringComparison.InvariantCultureIgnoreCase));
            if (mapInfoDef != null)
                return mapInfoDef;

            return DefaultMap;
        }

        public void SetDefaultMap(MapInfoDef map) => DefaultMap = map;
        public MapInfoDef? GetNextMap(MapInfoDef map) => GetMap(map.Next);
        public MapInfoDef? GetNextSecretMap(MapInfoDef map) => GetMap(map.SecretNext);
        public MapInfoDef? GetMap(string name) => m_maps.FirstOrDefault(x => x.MapName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
    }
}