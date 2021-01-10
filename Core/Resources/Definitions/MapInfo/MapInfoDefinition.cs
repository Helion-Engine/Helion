using Helion.Util;
using Helion.Util.Parser;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Resources.Definitions.MapInfo
{
    public class MapInfoDefinition
    {
        public MapInfo MapInfo { get; private set; } = new();
        public GameInfoDef GameDefinition { get; private set; } = new();

        private static readonly CIString GameInfoName = "gameinfo";
        private static readonly CIString ClearEpisodesName = "clearepisodes";
        private static readonly CIString EpisodeName = "episode";
        private static readonly CIString ClusterName = "cluster";
        private static readonly CIString DefaultMapName = "defaultmap";
        private static readonly CIString AddDefaultMapName = "adddefaultmap";
        private static readonly CIString MapName = "map";

        private static readonly HashSet<CIString> HighLevelNames = new HashSet<CIString>
        {
            GameInfoName,
            ClearEpisodesName,
            EpisodeName,
            ClusterName,
            DefaultMapName,
            AddDefaultMapName,
            MapName
        };

        private bool m_legacy;

        public void Parse(string data)
        {
            m_legacy = true;
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            while (!parser.IsDone())
            {
                CIString item = parser.ConsumeString();

                if (item == "include")
                    ParseInclude(parser);
                else if (item == GameInfoName)
                    GameDefinition = ParseGameInfo(parser);
                else if (item == ClearEpisodesName)
                    MapInfo.ClearEpisodes();
                else if (item == EpisodeName)
                    ParseEpisode(parser);
                else if (item == ClusterName)
                    ParseCluster(parser);
                else if (item == DefaultMapName)
                    MapInfo.SetDefaultMap(ParseMapDef(parser, false));
                else if (item == AddDefaultMapName)
                    ParseMapDef(parser, false, MapInfo.DefaultMap);
                else if (item == MapName)
                    MapInfo.AddMap(ParseMapDef(parser, true));
            }
        }

        private MapInfoDef ParseMapDef(SimpleParser parser, bool parseHeader, MapInfoDef? mapDef = null)
        {
            if (mapDef == null)
                mapDef = new();

            if (parseHeader)
            {
                int defLine = parser.GetCurrentLine();
                mapDef.MapName = parser.ConsumeString();
                if (parser.Peek("lookup"))
                {
                    parser.ConsumeString();
                    mapDef.LookupName = parser.ConsumeString();
                }

                // Have to check current line for nicename thanks to legacy mapinfo
                if (defLine == parser.GetCurrentLine())
                    mapDef.NiceName = parser.ConsumeString();
            }

            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();
                ConsumeEquals(parser);

                if (item == "levelnum")
                    mapDef.LevelNumber = parser.ConsumeInteger();
                else if (item == "titlepatch")
                    mapDef.TitlePatch = parser.ConsumeString();
                else if (item == "next")
                    mapDef.Next = parser.ConsumeString();
                else if (item == "secretnext")
                    mapDef.SecretNext = parser.ConsumeString();
                else if (item == "sky1")
                    mapDef.Sky1 = parser.ConsumeString();
                else if (item == "cluster")
                    mapDef.Cluster = parser.ConsumeInteger();
                else if (item == "par")
                    mapDef.ParTime = parser.ConsumeInteger();
                else if (item == "sucktime")
                    mapDef.SuckTime = parser.ConsumeInteger();
                else if (item == "music")
                    mapDef.Music = parser.ConsumeString();
                else if (item == "nointermission")
                    mapDef.MapOptions |= MapOptions.NoIntermission;
                else if (item == "needclustertext")
                    mapDef.MapOptions |= MapOptions.NeedClusterText;
                else if (item == "allowmonstertelefrags")
                    mapDef.MapOptions |= MapOptions.AllowMonsterTelefrags;
                else if (item == "nocrouch")
                    mapDef.MapOptions |= MapOptions.NoCrouch;
                else if (item == "nojump")
                    mapDef.MapOptions |= MapOptions.NoJump;
                else if (item == "nosoundclipping")
                    continue; // Deprecated, no longer used
                else if (item == "baronspecial")
                    mapDef.MapSpecial = MapSpecial.BaronSpecial;
                else if (item == "cyberdemonspecial")
                    mapDef.MapSpecial = MapSpecial.CyberdemonSpecial;
                else if (item == "spidermastermindspecial")
                    mapDef.MapSpecial = MapSpecial.SpiderMastermindSpecial;
                else if (item == "map07special")
                    mapDef.MapSpecial = MapSpecial.Map07Special;
                else if (item == "specialaction_lowerfloor")
                    mapDef.MapSpecialAction = MapSpecialAction.LowerFloor;
                else if (item == "specialaction_exitlevel")
                    mapDef.MapSpecialAction = MapSpecialAction.ExitLevel;
                else if (item == "specialaction_opendoor")
                    mapDef.MapSpecialAction = MapSpecialAction.OpenDoor;
                else
                {
                    // Warn we do not know what this is
                    parser.ConsumeLine();
                }
            }

            ConsumeBrace(parser, false);
            return mapDef;
        }

        private void ParseCluster(SimpleParser parser)
        {
            // TODO
        }

        private void ParseEpisode(SimpleParser parser)
        {
            EpisodeDef episodeDef = new();
            episodeDef.StartMap = parser.ConsumeString();
            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();
                ConsumeEquals(parser);

                if (item == "picname")
                    episodeDef.PicName = parser.ConsumeString();
                else if (item == "name")
                    episodeDef.Name = parser.ConsumeString();
                else if (item == "key")
                    episodeDef.Key = parser.ConsumeString();
                else if (item == "optional")
                    episodeDef.Optional = true;
                else
                {
                    // Warn we do not know what this is
                    parser.ConsumeLine();
                }
            }

            ConsumeBrace(parser, false);
            MapInfo.AddEpisode(episodeDef);
        }

        private GameInfoDef ParseGameInfo(SimpleParser parser)
        {
            GameInfoDef gameDef = new();
            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();
                ConsumeEquals(parser);

                if (item == "creditpage")
                    gameDef.CreditPages = GetStringList(parser);
                else if (item == "finalepage")
                    gameDef.FinalePages = GetStringList(parser);
                else if (item == "infopage")
                    gameDef.InfoPages = GetStringList(parser);
                else if (item == "quitmessages")
                    gameDef.QuitMessages = GetStringList(parser);
                else if (item == "titlemusic")
                    gameDef.TitleMusic = parser.ConsumeString();
                else if (item == "titletime")
                    gameDef.TitleTime = parser.ConsumeInteger();
                else if (item == "finalemusic")
                    gameDef.FinaleMusic = parser.ConsumeString();
                else if (item == "finaleflat")
                    gameDef.FinaleFlat = parser.ConsumeString();
                else if (item == "quitsound")
                    gameDef.QuitSound = parser.ConsumeString();
                else if (item == "borderflat")
                    gameDef.BorderFlat = parser.ConsumeString();
                else if (item == "drawreadthis")
                    gameDef.DrawReadThis = parser.ConsumeBool();
                else if (item == "intermissionmusic")
                    gameDef.IntermissionMusic = parser.ConsumeString();
                else
                {
                    // Warn we do not know what this is
                    parser.ConsumeLine();
                }
            }

            ConsumeBrace(parser, false);
            return gameDef;
        }

        private void ConsumeBrace(SimpleParser parser, bool start)
        {
            if (m_legacy && !parser.IsDone() && (parser.Peek('{') || parser.Peek('}')))
                m_legacy = false;

            if (m_legacy)
                return;

            if (start)
                parser.ConsumeString("{");
            else
                parser.ConsumeString("}");
        }

        private bool IsBlockComplete(SimpleParser parser)
        {
            if (m_legacy)
            {
                if (parser.IsDone())
                    return true;

                return HighLevelNames.Contains(parser.PeekString());
            }

            return parser.Peek('}');
        }

        private void ConsumeEquals(SimpleParser parser)
        {
            if (m_legacy)
                return;

            if (parser.Peek("="))
                parser.ConsumeString("=");
        }

        private List<string> GetStringList(SimpleParser parser)
        {
            string data = parser.ConsumeLine();
            return data.Split(new char[] { ',' }).ToList();
        }

        private void ParseInclude(SimpleParser parser)
        {
            // Don't care for now
            parser.ConsumeString();
        }
    }
}
