using Helion.Maps.Specials.Vanilla;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Helion.Resources.Definitions.MapInfo;

public partial class MapInfoDefinition
{
    private readonly Dictionary<string, ClusterDef> m_newClusterDefs = new Dictionary<string, ClusterDef>(StringComparer.OrdinalIgnoreCase);
    private readonly ClusterDef Ep1 = new(0)
    {
        Flat = "$BGFLATE1",
        ExitText = ["$E1TEXT"],
        EndGameNext = "EndGame1"
    };
    private readonly ClusterDef Ep2 = new(0)
    {
        Flat = "$BGFLATE2",
        ExitText = ["$E2TEXT"],
        EndGameNext = "EndGame2"
    };
    private readonly ClusterDef Ep3 = new(0)
    {
        Flat = "$BGFLATE3",
        ExitText = ["$E3TEXT"],
        EndGameNext = "EndGame3"
    };
    private readonly ClusterDef Ep4 = new(0)
    {
        Flat = "$BGFLATE4",
        ExitText = ["$E4TEXT"],
        EndGameNext = "EndGame4"
    };

    public void ParseUniversalMapInfo(IWadBaseType iwadType, string data)
    {
        m_legacy = false;
        SimpleParser parser = new();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            MapInfoDef mapDef = MapInfo.DefaultMap == null ? new MapInfoDef() : (MapInfoDef)MapInfo.DefaultMap.Clone();
            parser.ConsumeString("MAP");
            mapDef.MapName = parser.ConsumeString();
            ConsumeBrace(parser, true);

            MapInfoDef? existing = MapInfo.GetMap(mapDef.MapName).MapInfo;
            if (existing != null)
                mapDef = existing;
            MapInfo.AddOrReplaceMap(mapDef);

            bool specifiedTitlePatch = false;
            bool specifiedLevelName = false;

            while (!IsBlockComplete(parser, true))
            {
                int line = parser.GetCurrentLine();
                string item = parser.ConsumeString();
                parser.ConsumeString("=");
                if (item.EqualsIgnoreCase("levelname"))
                {
                    mapDef.NiceName = parser.ConsumeString();
                    specifiedLevelName = true;
                }
                else if (item.EqualsIgnoreCase("levelpic"))
                {
                    specifiedTitlePatch = true;
                    mapDef.TitlePatch = parser.ConsumeString();
                }
                else if (item.EqualsIgnoreCase("label"))
                    ParseLabel(parser, mapDef);
                else if (item.EqualsIgnoreCase("next"))
                    mapDef.Next = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("nextsecret"))
                    mapDef.SecretNext = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("skytexture"))
                    mapDef.Sky1 = new SkyDef() { Name = parser.ConsumeString() };
                else if (item.EqualsIgnoreCase("music"))
                    mapDef.Music = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("exitpic"))
                    mapDef.ExitPic = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("enterpic"))
                    mapDef.EnterPic = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("partime"))
                    mapDef.ParTime = parser.ConsumeInteger();
                else if (item.EqualsIgnoreCase("endgame"))
                    ParseEndGame(parser, mapDef, iwadType);
                else if (item.EqualsIgnoreCase("endpic"))
                    ParseEndPic(parser, mapDef);
                else if (item.EqualsIgnoreCase("endbunny"))
                    ParseEndBunny(parser, mapDef, iwadType);
                else if (item.EqualsIgnoreCase("endcast"))
                    ParseEndCast(parser, mapDef, iwadType);
                else if (item.EqualsIgnoreCase("nointermission"))
                    ParseNoIntermission(parser, mapDef);
                else if (item.EqualsIgnoreCase("intertext"))
                    ParserInterText(parser, mapDef, secret: false, iwadType);
                else if (item.EqualsIgnoreCase("intertextsecret"))
                    ParserInterText(parser, mapDef, secret: true, iwadType);
                else if (item.EqualsIgnoreCase("interbackdrop"))
                    ParseInterTextBackDrop(parser, mapDef, iwadType);
                else if (item.EqualsIgnoreCase("intermusic"))
                    ParseInterMusic(parser, mapDef, iwadType);
                else if (item.EqualsIgnoreCase("episode"))
                    ParseEpisode(parser, mapDef);
                else if (item.EqualsIgnoreCase("bossaction"))
                    ParseBossAction(parser, mapDef);
                else if (item.EqualsIgnoreCase("bossactionednum"))
                    ParseBossActionEditorNumber(parser, mapDef);
                else if (item.EqualsIgnoreCase("author"))
                    mapDef.Author = parser.ConsumeString();
                else
                {
                    WarnMissing("map", item, line);
                    if (line == parser.GetCurrentLine())
                        parser.ConsumeLine();
                }
            }

            if (specifiedLevelName && !specifiedTitlePatch)
                mapDef.TitlePatch = string.Empty;

            ConsumeBrace(parser, false);
        }
    }

    private static void ParseEndPic(SimpleParser parser, MapInfoDef mapDef)
    {
        mapDef.Next = "EndPic";
        mapDef.EndPic = parser.ConsumeString();
    }

    private static void ParseBossAction(SimpleParser parser, MapInfoDef mapDef)
    {
        mapDef.MapSpecial = MapSpecial.None;
        mapDef.MapSpecialAction = MapSpecialAction.None;
        if (parser.ConsumeIf("clear"))
        {
            mapDef.BossActions.Clear();
            return;
        }

        string actorName = parser.ConsumeString();
        parser.ConsumeString(",");
        int action = parser.ConsumeInteger();
        parser.ConsumeString(",");
        int tag = parser.ConsumeInteger();

        mapDef.BossActions.Add(new(actorName, (VanillaLineSpecialType)action, tag));
    }

    private static void ParseBossActionEditorNumber(SimpleParser parser, MapInfoDef mapDef)
    {
        mapDef.MapSpecial = MapSpecial.None;
        mapDef.MapSpecialAction = MapSpecialAction.None;
        if (parser.ConsumeIf("clear"))
        {
            mapDef.BossActions.Clear();
            return;
        }

        int num = parser.ConsumeInteger();
        parser.ConsumeString(",");
        int action = parser.ConsumeInteger();
        parser.ConsumeString(",");
        int tag = parser.ConsumeInteger();

        mapDef.BossActions.Add(new(num, (VanillaLineSpecialType)action, tag));
    }

    private void ParseEpisode(SimpleParser parser, MapInfoDef mapDef)
    {
        if (parser.ConsumeIf("clear"))
        {
            MapInfo.RemoveEpisodeByMapName(mapDef.MapName);
            return;
        }

        string picName = parser.ConsumeString();
        parser.ConsumeString(",");
        string name = parser.ConsumeString();
        string key = string.Empty;
        if (parser.ConsumeIf(","))
            key = parser.ConsumeString();

        MapInfo.AddEpisode(new()
        {
            StartMap = mapDef.MapName,
            PicName = picName,
            Name = name,
            Key = key,
        });
    }

    private void ParseInterMusic(SimpleParser parser, MapInfoDef mapDef, IWadBaseType iwadType)
    {
        var clusterDef = GetOrCreateClusterDef(mapDef, iwadType);
        clusterDef.Music = parser.ConsumeString();
    }

    private void ParseInterTextBackDrop(SimpleParser parser, MapInfoDef mapDef, IWadBaseType iwadType)
    {
        var clusterDef = GetOrCreateClusterDef(mapDef, iwadType);
        clusterDef.Flat = parser.ConsumeString();
    }

    private void ParserInterText(SimpleParser parser, MapInfoDef mapDef, bool secret, IWadBaseType iwadType)
    {
        if (parser.ConsumeIf("clear"))
        {
            if (!m_newClusterDefs.TryGetValue(mapDef.MapName, out var existingCluster))
            {
                if (IsChangingCluster(mapDef) && MapInfo.TryGetCluster(mapDef.Cluster, out existingCluster))
                {
                    existingCluster = existingCluster.Clone(MapInfo.GetNewClusterNumber());
                    m_newClusterDefs[mapDef.MapName] = existingCluster;
                }
            }

            if (existingCluster != null)
            {
                if (secret)
                    existingCluster.SecretExitText.Clear();
                else
                    existingCluster.ExitText.Clear();
            }

            return;
        }

        var clusterDef = GetOrCreateClusterDef(mapDef, iwadType, out var isNew);
        if (secret)
        {
            clusterDef.SecretExitText = GetClusterText(parser);
            if (isNew)
                clusterDef.ExitText.Clear();
        }
        else
        {
            clusterDef.ExitText = GetClusterText(parser);
            if (isNew)
                clusterDef.SecretExitText.Clear();
        }
    }

    private bool IsChangingCluster(MapInfoDef mapDef)
    {
        var nextMapResult = MapInfo.GetNextMap(mapDef);
        var nextMapInfo = nextMapResult.MapInfo;
        if (nextMapInfo == null)
            return false;

        var cluster = MapInfo.GetCluster(mapDef.Cluster);
        var nextCluster = MapInfo.GetCluster(nextMapInfo.Cluster);
        return cluster != null && nextCluster != null && cluster != nextCluster;
    }

    private ClusterDef GetOrCreateClusterDef(MapInfoDef mapDef, IWadBaseType iwadType) =>
        GetOrCreateClusterDef(mapDef, iwadType, out _);

    private ClusterDef GetOrCreateClusterDef(MapInfoDef mapDef, IWadBaseType iwadType, out bool isNew)
    {
        if (m_newClusterDefs.TryGetValue(mapDef.MapName, out var clusterDef))
        {
            isNew = false;
            return clusterDef;
        }

        isNew = true;
        return CreateNewClusterDef(mapDef, iwadType);
    }

    private ClusterDef CreateNewClusterDef(MapInfoDef mapDef, IWadBaseType iwadType)
    {
        var newClusterNum = MapInfo.GetNewClusterNumber();
        var clusterDef = GetEndGameClusterDef(mapDef, iwadType).Clone(newClusterNum);
        mapDef.ClusterDef = clusterDef;
        MapInfo.AddCluster(clusterDef);
        m_newClusterDefs[mapDef.MapName] = clusterDef;
        return clusterDef;
    }

    private ClusterDef GetEndGameClusterDef(MapInfoDef mapDef, IWadBaseType iwadType)
    {
        // Setting just endgame = true triggers the default endgame for the episode. 
        var mapName = mapDef.MapName;
        if (iwadType == IWadBaseType.Doom1 && mapName.Length >= 4 &&
            char.ToUpperInvariant(mapName[0]) == 'E' && char.ToUpperInvariant(mapName[2]) == 'M' &&
            int.TryParse(mapName[1].ToString(), out var episode))
        {
            switch (episode)
            {
                case 1:
                    return Ep1;
                case 2:
                    return Ep2;
                case 3:
                    return Ep3;
                case 4:
                    return Ep4;
            }
        }

        return Ep1;
    }

    private static void ParseNoIntermission(SimpleParser parser, MapInfoDef mapDef)
    {
        bool set = parser.ConsumeString().EqualsIgnoreCase("true");
        mapDef.SetOption(MapOptions.NoIntermission, set);
    }

    private void ParseEndCast(SimpleParser parser, MapInfoDef mapDef, IWadBaseType iwadType)
    {
        if (!parser.ConsumeString().EqualsIgnoreCase("true"))
            return;

        mapDef.Next = "EndGameC";
        GetOrCreateClusterDef(mapDef, iwadType);
    }

    private void ParseEndBunny(SimpleParser parser, MapInfoDef mapDef, IWadBaseType iwadType)
    {
        if (!parser.ConsumeString().EqualsIgnoreCase("true"))
            return;

        mapDef.Next = "EndBunny";
        GetOrCreateClusterDef(mapDef, iwadType);
    }

    private void ParseEndGame(SimpleParser parser, MapInfoDef mapDef, IWadBaseType iwadType)
    {
        if (!parser.ConsumeString().EqualsIgnoreCase("true"))
            return;

        var clusterDef = GetOrCreateClusterDef(mapDef, iwadType);
        mapDef.Next = clusterDef.EndGameNext;
    }

    private static void ParseLabel(SimpleParser parser, MapInfoDef mapDef)
    {
        mapDef.Label = parser.ConsumeString();
        if (mapDef.Label.EqualsIgnoreCase("clear"))
            mapDef.Label = string.Empty;
    }
}
