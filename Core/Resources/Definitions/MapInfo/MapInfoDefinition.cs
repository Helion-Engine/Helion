using Helion.Util;
using Helion.Util.Parser;
using System;
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

        private static readonly CIString GameCreditPageName = "creditpage";
        private static readonly CIString GameFinalePageName = "finalepage";
        private static readonly CIString GameInfoPageName = "infopage";
        private static readonly CIString GameQuitMessagesName = "quitmessages";
        private static readonly CIString GameTitleMusicName = "titlemusic";
        private static readonly CIString GameTitleTimeName = "titletime";
        private static readonly CIString GameFinaleMusicName = "finalemusic";
        private static readonly CIString GameFinaleFlatName = "finaleflat";
        private static readonly CIString GameQuitSoundName = "quitsound";
        private static readonly CIString GameBorderFlatName = "borderflat";
        private static readonly CIString GameDrawReadThisName = "drawreadthis";
        private static readonly CIString GameIntermissionMusicName = "intermissionmusic";

        private static readonly HashSet<CIString> GameInfoNames = new HashSet<CIString>
        {
            GameCreditPageName,
            GameFinalePageName,
            GameInfoPageName,
            GameQuitMessagesName,
            GameTitleMusicName,
            GameTitleTimeName,
            GameFinaleMusicName,
            GameFinaleFlatName,
            GameQuitSoundName,
            GameBorderFlatName,
            GameDrawReadThisName,
            GameIntermissionMusicName,
        };

        private static readonly CIString EpisodePicName = "picname";
        private static readonly CIString EpisodeEpName = "name";
        private static readonly CIString EpisodeKeyName = "key";

        private static readonly HashSet<CIString> EpisodeNames = new HashSet<CIString>
        {
            EpisodePicName,
            EpisodeEpName,
            EpisodeKeyName
        };

        private static readonly CIString MapLevelNumName = "levelnum";
        private static readonly CIString MapTitlePatchName = "titlepatch";
        private static readonly CIString MapNextName = "next";
        private static readonly CIString MapSecretName = "secretnext";
        private static readonly CIString MapSky1Name = "sky1";
        private static readonly CIString MapSky2Name = "sky2";
        private static readonly CIString MapClusterName = "cluster";
        private static readonly CIString MapParName = "par";
        private static readonly CIString MapSuckName = "sucktime";
        private static readonly CIString MapMusicName = "music";

        private static readonly HashSet<CIString> MapNames = new HashSet<CIString>
        {
            MapLevelNumName,
            MapTitlePatchName,
            MapNextName,
            MapSecretName,
            MapSky1Name,
            MapSky2Name,
            MapClusterName,
            MapParName,
            MapSuckName,
            MapMusicName
        };

        private static readonly CIString ClusterEnterTextName = "entertext";
        private static readonly CIString ClusterExitTextName = "exittext";
        private static readonly CIString ClusterExitTextIsLumpName = "exittextislump";
        private static readonly CIString ClusterMusicName = "music";
        private static readonly CIString ClusterFlatName = "flat";
        private static readonly CIString ClusterPicName = "pic";
        private static readonly CIString ClusterHubName = "hub";
        private static readonly CIString ClusterAllowIntermissionName = "allowintermission";

        private static readonly HashSet<CIString> ClusterNames = new HashSet<CIString>
        {
            ClusterEnterTextName,
            ClusterExitTextName,
            ClusterMusicName,
            ClusterMusicName,
            ClusterFlatName,
            ClusterPicName,
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
                    MapInfo.AddCluster(ParseCluster(parser));
                else if (item == DefaultMapName)
                    MapInfo.SetDefaultMap(ParseMapDef(parser, false));
                else if (item == AddDefaultMapName)
                    ParseMapDef(parser, false, MapInfo.DefaultMap);
                else if (item == MapName)
                    MapInfo.AddMap(ParseMapDef(parser, true, (MapInfoDef)MapInfo.DefaultMap.Clone()));
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
                    mapDef.LookupName = "$" + parser.ConsumeString();
                }

                // Have to check current line for nicename thanks to legacy mapinfo
                if (defLine == parser.GetCurrentLine())
                {
                    mapDef.NiceName = parser.ConsumeString();
                    if (mapDef.NiceName.StartsWith('$'))
                    {
                        mapDef.LookupName = mapDef.NiceName;
                        mapDef.NiceName = string.Empty;
                    }
                }
            }

            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();

                if (MapNames.Contains(item))
                {
                    ConsumeEquals(parser);

                    if (item == MapLevelNumName)
                        mapDef.LevelNumber = parser.ConsumeInteger();
                    else if (item == MapTitlePatchName)
                        mapDef.TitlePatch = parser.ConsumeString();
                    else if (item == MapNextName)
                        mapDef.Next = parser.ConsumeString();
                    else if (item == MapSecretName)
                        mapDef.SecretNext = parser.ConsumeString();
                    else if (item == MapSky1Name)
                        mapDef.Sky1 = ParseMapSky(parser);
                    else if (item == MapSky2Name)
                        mapDef.Sky2 = ParseMapSky(parser);
                    else if (item == MapClusterName)
                        mapDef.Cluster = parser.ConsumeInteger();
                    else if (item == MapParName)
                        mapDef.ParTime = parser.ConsumeInteger();
                    else if (item == MapSuckName)
                        mapDef.SuckTime = parser.ConsumeInteger();
                    else if (item == MapMusicName)
                        mapDef.Music = parser.ConsumeString();
                }
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

        private SkyDef ParseMapSky(SimpleParser parser)
        {
            SkyDef sky = new();
            sky.Name = parser.ConsumeString();
            if (!MapNames.Contains(parser.PeekString()))
                sky.ScrollSpeed = parser.ConsumeInteger();
            return sky;
        }

        private ClusterDef ParseCluster(SimpleParser parser)
        {
            ClusterDef clusterDef = new ClusterDef();
            clusterDef.ClusterNum = parser.ConsumeInteger();

            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();

                if (ClusterNames.Contains(item))
                {
                    ConsumeEquals(parser);

                    if (item == ClusterEnterTextName)
                        clusterDef.EnterText = GetClusterText(parser);
                    else if (item == ClusterExitTextName)
                        clusterDef.ExitText = GetClusterText(parser);
                    else if (item == ClusterMusicName)
                        clusterDef.Music = parser.ConsumeString();
                    else if (item == ClusterFlatName)
                        clusterDef.Flat = parser.ConsumeString();
                    else if (item == ClusterPicName)
                        clusterDef.Pic = parser.ConsumeString();
                }
                else if (item == ClusterExitTextIsLumpName)
                    clusterDef.IsExitTextLump = true;
                else if (item == ClusterHubName)
                    clusterDef.IsHub = true;
                else if (item == ClusterAllowIntermissionName)
                    clusterDef.AllowIntermission = true;
                else
                {
                    // Warn we do not know what this is
                    parser.ConsumeLine();
                }
            }

            return clusterDef;
        }

        private List<string> GetClusterText(SimpleParser parser)
        {
            List<string> textItems = new List<string>();
            while (!ClusterNames.Contains(parser.PeekString()))
            {
                string text = parser.ConsumeString();
                bool hasComma = text.EndsWith(',');
                if (text.EndsWith(','))
                    text = text[..^1];

                if (text.Equals("lookup", StringComparison.OrdinalIgnoreCase))
                {
                    textItems.Add("$" + parser.ConsumeString());
                    break;
                }

                textItems.Add(text);

                if (!hasComma)
                    break;
            }

            return textItems;
        }

        private void ParseEpisode(SimpleParser parser)
        {
            EpisodeDef episodeDef = new();
            episodeDef.StartMap = parser.ConsumeString();
            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();

                if (EpisodeNames.Contains(item))
                {
                    ConsumeEquals(parser);
                    if (item == EpisodePicName)
                        episodeDef.PicName = parser.ConsumeString();
                    else if (item == EpisodeEpName)
                        episodeDef.Name = parser.ConsumeString();
                    else if (item == EpisodeKeyName)
                        episodeDef.Key = parser.ConsumeString();
                }
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

                if (GameInfoNames.Contains(item))
                {
                    ConsumeEquals(parser);

                    if (item == GameCreditPageName)
                        gameDef.CreditPages = GetStringList(parser);
                    else if (item == GameFinalePageName)
                        gameDef.FinalePages = GetStringList(parser);
                    else if (item == GameInfoPageName)
                        gameDef.InfoPages = GetStringList(parser);
                    else if (item == GameQuitMessagesName)
                        gameDef.QuitMessages = GetStringList(parser);
                    else if (item == GameTitleMusicName)
                        gameDef.TitleMusic = parser.ConsumeString();
                    else if (item == GameTitleTimeName)
                        gameDef.TitleTime = parser.ConsumeInteger();
                    else if (item == GameFinaleMusicName)
                        gameDef.FinaleMusic = parser.ConsumeString();
                    else if (item == GameFinaleFlatName)
                        gameDef.FinaleFlat = parser.ConsumeString();
                    else if (item == GameQuitSoundName)
                        gameDef.QuitSound = parser.ConsumeString();
                    else if (item == GameBorderFlatName)
                        gameDef.BorderFlat = parser.ConsumeString();
                    else if (item == GameDrawReadThisName)
                        gameDef.DrawReadThis = parser.ConsumeBool();
                    else if (item == GameIntermissionMusicName)
                        gameDef.IntermissionMusic = parser.ConsumeString();
                }
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
