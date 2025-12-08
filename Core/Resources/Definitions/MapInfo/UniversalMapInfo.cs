using Helion.Maps.Specials.Vanilla;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.MapInfo;

public partial class MapInfoDefinition
{
    private readonly Dictionary<string, ClusterDef> m_newClusterDefs = new(StringComparer.OrdinalIgnoreCase);

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
            if (existing == null)
                mapDef.Label = mapDef.MapName;
            else
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
                else if (item.EqualsIgnoreCase("jumping"))
                    ParsePlayerAction(parser, mapDef, MapOptions.NoJump, item, line);
                else if (item.EqualsIgnoreCase("crouching"))
                    ParsePlayerAction(parser, mapDef, MapOptions.NoCrouch, item, line);
                else if (item.EqualsIgnoreCase("freeaim"))
                    ParsePlayerAction(parser, mapDef, MapOptions.NoFreelook, item, line);
                else
                {
                    WarnMissing("map", item, line);
                    if (line == parser.GetCurrentLine())
                        parser.ConsumeLine();
                }
            }

            if (specifiedLevelName && !specifiedTitlePatch)
                mapDef.TitlePatch = string.Empty;

            if (mapDef.ClusterDef == null && IsDefaultClusterMap(iwadType, mapDef))
            {
                mapDef.ClusterDef = GetOrCreateClusterDef(mapDef, iwadType);
                if (!string.IsNullOrEmpty(mapDef.ClusterDef.EndGameNext))
                    mapDef.Next = mapDef.ClusterDef.EndGameNext;
            }

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
            MapInfo.ClearEpisodes();
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

    private static ClusterDef GetEndGameClusterDef(MapInfoDef mapDef, IWadBaseType iwadType)
    {
        // Setting just endgame = true triggers the default endgame for the episode.
        var mapName = mapDef.MapName;
        if (iwadType == IWadBaseType.Doom1 && mapName.Length >= 4 &&
            char.ToUpperInvariant(mapName[0]) == 'E' && char.ToUpperInvariant(mapName[2]) == 'M' &&
            int.TryParse(mapName[1].ToString(), out var episode))
        {
            return CreateDefaultDoom1ClusterDef(episode);
        }

        return CreateDefaultClusterDef(iwadType, mapName);
    }

    private static ClusterDef CreateDefaultDoom1ClusterDef(int episode)
    {
        if (episode < 1 || episode > 4)
            episode = 1;

        return new ClusterDef(0)
        {
            Flat = $"$BGFLATE{episode}",
            ExitText = [$"$E{episode}TEXT"],
            EndGameNext = $"EndGame{episode}"
        };
    }

    private static bool IsDefaultClusterMap(IWadBaseType iwadType, MapInfoDef mapDef)
    {
        var mapName = mapDef.MapName;
        if (iwadType == IWadBaseType.Doom1)
        {
            if (mapName.Length != 4)
                return false;

            return mapName[3] == '8';
        }

        return mapName.EqualsIgnoreCase("MAP06") || mapName.EqualsIgnoreCase("MAP11") ||
            mapName.EqualsIgnoreCase("MAP20") || mapName.EqualsIgnoreCase("MAP30") ||
            mapName.EqualsIgnoreCase("MAP31") || mapName.EqualsIgnoreCase("MAP32");
    }

    private static ClusterDef CreateDefaultClusterDef(IWadBaseType type, string mapName)
    {
        string prefix = type switch
        {
            IWadBaseType.Plutonia => "$P",
            IWadBaseType.TNT => "$T",
            _ => "$C",
        };

        string flat = "$BGFLAT06";
        List<string>? exitText = null;
        List<string>? secretExitText = null;

        if (mapName.EqualsIgnoreCase("MAP06"))
        {
            flat = "$BGFLAT06";
            exitText = [prefix + "1TEXT"];
        }
        if (mapName.EqualsIgnoreCase("MAP11"))
        {
            flat = "$BGFLAT11";
            exitText = [prefix + "2TEXT"];
        }
        if (mapName.EqualsIgnoreCase("MAP15"))
        {
            flat = "$BGFLAT15";
            secretExitText = [prefix + "5TEXT"];
        }
        else if (mapName.EqualsIgnoreCase("MAP20"))
        {
            flat = "$BGFLAT20";
            exitText = [prefix + "3TEXT"];
        }
        else if (mapName.EqualsIgnoreCase("MAP30"))
        {
            flat = "$BGFLAT30";
            exitText = [prefix + "4TEXT"];
        }
        else if (mapName.EqualsIgnoreCase("MAP31"))
        {
            flat = "$BGFLAT31";
            secretExitText = [prefix + "6TEXT"];
        }

        return new(0)
        {
            Flat = flat,
            ExitText = exitText ?? [],
            SecretExitText = secretExitText ?? []
        };
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

    private static void ParsePlayerAction(SimpleParser parser, MapInfoDef mapDef, MapOptions option, string item, int line)
    {
        var value = parser.ConsumeString();
        if (value.EqualsIgnoreCase("disallow"))
            mapDef.SetOption(option, true);
        else if (value.EqualsIgnoreCase("require") || value.EqualsIgnoreCase("allow"))
            mapDef.SetOption(option, false);
        else
            Log.Warn($"MapInfo: Line {line}: Expected one of 'disallow', 'require', or 'allow' for key '{item}' but found {value}.");
    }
}
