using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Resources.Definitions.MapInfo
{
    public class MapInfo
    {
        public IReadOnlyList<EpisodeDef> Episodes => m_episodes.AsReadOnly();
        public IReadOnlyList<MapInfoDef> Maps => m_maps.AsReadOnly();
        public MapInfoDef DefaultMap { get; private set; } = new();

        private readonly List<EpisodeDef> m_episodes = new List<EpisodeDef>();
        private readonly List<MapInfoDef> m_maps = new List<MapInfoDef>();

        public void ClearEpisodes() => m_episodes.Clear();

        public void AddEpisode(EpisodeDef episode) => m_episodes.Add(episode);

        public void AddMap(MapInfoDef newMap)
        {
            for (int i=0; i < m_maps.Count; i++)
            {
                MapInfoDef mapDef = m_maps[i];
                if (newMap.MapName.Equals(mapDef.MapName, StringComparison.InvariantCultureIgnoreCase))
                {
                    m_maps[i] = newMap;
                    break;
                }
            }

            m_maps.Add(newMap);
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