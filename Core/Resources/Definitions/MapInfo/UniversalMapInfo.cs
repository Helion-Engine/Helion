using Helion.Maps.Specials.Vanilla;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.MapInfo;

public partial class MapInfoDefinition
{
    public void ParseUniversalMapInfo(MapInfo mapInfo, string data)
    {
        m_legacy = false;
        SimpleParser parser = new SimpleParser();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            MapInfoDef mapDef = (MapInfoDef)MapInfo.DefaultMap.Clone();
            parser.ConsumeString("MAP");
            mapDef.MapName = parser.ConsumeString();
            ConsumeBrace(parser, true);

            MapInfoDef? existing = MapInfo.GetMap(mapDef.MapName);
            if (existing != null)
                mapDef = existing;

            mapDef.LookupName = string.Empty;
            while (!IsBlockComplete(parser, true))
            {
                int line = parser.GetCurrentLine();
                string item = parser.ConsumeString();
                parser.ConsumeString("=");
                if (item.EqualsIgnoreCase("levelname"))
                    mapDef.NiceName = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("label"))
                    ParseLabel(parser, mapDef);
                else if (item.EqualsIgnoreCase("levelpic"))
                    mapDef.TitlePatch = parser.ConsumeString();
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
                    ParseEndGame(parser, mapDef);
                else if (item.EqualsIgnoreCase("endpic"))
                    mapDef.EndPic = parser.ConsumeString();
                else if (item.EqualsIgnoreCase("endbunny"))
                    ParseEndBunny(parser, mapDef);
                else if (item.EqualsIgnoreCase("endcast"))
                    ParseEndCast(parser, mapDef);
                else if (item.EqualsIgnoreCase("nointermission"))
                    ParseNoIntermission(parser, mapDef);
                else if (item.EqualsIgnoreCase("intertext"))
                    ParserInterText(parser, mapDef, secret: false);
                else if (item.EqualsIgnoreCase("intertextsecret"))
                    ParserInterText(parser, mapDef, secret: true);
                else if (item.EqualsIgnoreCase("interbackdrop"))
                    ParseInterTextBackDrop(parser, mapDef);
                else if (item.EqualsIgnoreCase("intermusic"))
                    ParseInterMusic(parser, mapDef);
                else if (item.EqualsIgnoreCase("episode"))
                    ParseEpisode(parser, mapDef);
                else if (item.EqualsIgnoreCase("bossaction"))
                    ParseBossAction(parser, mapDef);
                else if (item.EqualsIgnoreCase("author"))
                    mapDef.Author = parser.ConsumeString();
                else
                {
                    WarnMissing("map", item, line);
                    if (line == parser.GetCurrentLine())
                        parser.ConsumeLine();
                }
            }

            ConsumeBrace(parser, false);
        }
    }

    private void ParseBossAction(SimpleParser parser, MapInfoDef mapDef)
    {
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
        parser.ConsumeString(",");
        string key = parser.ConsumeString();

        MapInfo.AddEpisode(new()
        {
            StartMap = mapDef.MapName,
            PicName = picName,
            Name = name,
            Key = key,
        });
    }

    private void ParseInterMusic(SimpleParser parser, MapInfoDef mapDef)
    {
        var clusterDef = GetOrCreateClusterDef(mapDef);
        clusterDef.Music = parser.ConsumeString();
    }

    private void ParseInterTextBackDrop(SimpleParser parser, MapInfoDef mapDef)
    {
        var clusterDef = GetOrCreateClusterDef(mapDef);
        clusterDef.Flat = parser.ConsumeString();
    }

    private void ParserInterText(SimpleParser parser, MapInfoDef mapDef, bool secret)
    {
        if (parser.ConsumeIf("clear"))
        {
            if (MapInfo.TryGetCluster(mapDef.Cluster, out var existingCluster))
            {
                if (secret)
                    existingCluster.SecretExitText.Clear();
                else
                    existingCluster.ExitText.Clear();
            }
            return;
        }

        var clusterDef = GetOrCreateClusterDef(mapDef);
        if (secret)
            clusterDef.SecretExitText = GetClusterText(parser);
        else
            clusterDef.ExitText = GetClusterText(parser);
    }

    private ClusterDef GetOrCreateClusterDef(MapInfoDef mapDef)
    {
        if (!MapInfo.TryGetCluster(mapDef.Cluster, out var clusterDef))
            clusterDef = new ClusterDef() { ClusterNum = MapInfo.GetNewClusterNumber() };

        return clusterDef;
    }

    private void ParseNoIntermission(SimpleParser parser, MapInfoDef mapDef)
    {
        bool set = parser.ConsumeString().EqualsIgnoreCase("true");
        mapDef.SetOption(MapOptions.NoIntermission, set);
    }

    private void ParseEndCast(SimpleParser parser, MapInfoDef mapDef)
    {
        if (parser.ConsumeString().EqualsIgnoreCase("true"))
            mapDef.Next = "EndGameC";
    }

    private void ParseEndBunny(SimpleParser parser, MapInfoDef mapDef)
    {
        if (parser.ConsumeString().EqualsIgnoreCase("true"))
            mapDef.Next = "EndBunny";
    }

    private void ParseEndGame(SimpleParser parser, MapInfoDef mapDef)
    {
        if (!parser.ConsumeString().EqualsIgnoreCase("true"))
            return;

        mapDef.SetOption(MapOptions.NoIntermission, true);
        mapDef.Next = "EndGameW";
    }

    private static void ParseLabel(SimpleParser parser, MapInfoDef mapDef)
    {
        mapDef.Label = parser.ConsumeString();
        if (mapDef.Label.EqualsIgnoreCase("clear"))
            mapDef.Label = string.Empty;
    }
}
