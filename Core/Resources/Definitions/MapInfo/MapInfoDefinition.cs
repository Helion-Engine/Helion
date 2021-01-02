using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Parser;
using System;
using System.Collections.Generic;

namespace Helion.Resources.Definitions.MapInfo
{
    public class MapInfoDefinition
    {
        public MapInfo MapInfo { get; private set; } = new();
        public GameInfoDef GameDefinition { get; private set; } = new();

        private readonly SimpleParser m_parser = new();

        public void Parse(Entry entry)
        {
            m_parser.Parse(System.Text.Encoding.UTF8.GetString(entry.ReadData()));

            while (!m_parser.IsDone())
            {
                CIString item = m_parser.ConsumeString();

                if (item == "include")
                    ParseInclude();
                else if (item == "gameinfo")
                    GameDefinition = ParseGameInfo();
                else if (item == "clearepisodes")
                    MapInfo.ClearEpisodes();
                else if (item == "episode")
                    ParseEpisode();
                else if (item == "defaultmap")
                    MapInfo.SetDefaultMap(ParseMapDef(false));
                else if (item == "map")
                    MapInfo.AddMap(ParseMapDef(true));
            }
        }

        private MapInfoDef ParseMapDef(bool parseHeader)
        {
            MapInfoDef mapDef = new();

            if (parseHeader)
            {
                mapDef.MapName = m_parser.ConsumeString();
                if (m_parser.Peek("lookup"))
                {
                    m_parser.ConsumeString();
                    mapDef.Lookup = m_parser.ConsumeString();
                }
            }

            m_parser.ConsumeString("{");

            while (!m_parser.Peek('}'))
            {
                CIString item = m_parser.ConsumeString();
                if (m_parser.Peek("="))
                    m_parser.ConsumeString("=");

                if (item == "levelnum")
                    mapDef.LevelNumber = m_parser.ConsumeInteger();
                else if (item == "titlepatch")
                    mapDef.TitlePatch = m_parser.ConsumeString();
                else if (item == "next")
                    mapDef.Next = m_parser.ConsumeString();
                else if (item == "secretnext")
                    mapDef.SecretNext = m_parser.ConsumeString();
                else if (item == "sky1")
                    mapDef.Sky1 = m_parser.ConsumeString();
                else if (item == "cluster")
                    mapDef.Cluster = m_parser.ConsumeInteger();
                else if (item == "par")
                    mapDef.ParTime = m_parser.ConsumeInteger();
                else if (item == "sucktime")
                    mapDef.SuckTime = m_parser.ConsumeInteger();
                else if (item == "music")
                    mapDef.Music = m_parser.ConsumeString();
                else if (item == "nointermission")
                    mapDef.MapOptions |= MapOptions.NoIntermission;
                else if (item == "needclustertext")
                    mapDef.MapOptions |= MapOptions.NeedClusterText;
                else if (item == "nosoundclipping")
                    continue; // Deprecated, no longer used
                else if (item == "baronspecial")
                    mapDef.MapSpecial = MapSpecial.BaronSpecial;
                else if (item == "cyberdemonspecial")
                    mapDef.MapSpecial = MapSpecial.CyberdemonSpecial;
                else if (item == "spidermastermindspecial")
                    mapDef.MapSpecial = MapSpecial.SpiderMastermindSpecial;
                else if (item == "specialaction_lowerfloor")
                    mapDef.MapSpecialAction = MapSpecialAction.LowerFloor;
                else if (item == "specialaction_exitlevel")
                    mapDef.MapSpecialAction = MapSpecialAction.ExitLevel;
                else if (item == "specialaction_opendoor")
                    mapDef.MapSpecialAction = MapSpecialAction.OpenDoor;
                else
                {
                    // Warn we do not know what this is
                    m_parser.ConsumeLine();
                }
            }

            m_parser.ConsumeString("}");
            return mapDef;
        }

        private void ParseEpisode()
        {
            EpisodeDef episodeDef = new();
            episodeDef.StartMap = m_parser.ConsumeString();
            m_parser.ConsumeString("{");

            while (!m_parser.Peek('}'))
            {
                CIString item = m_parser.ConsumeString();
                if (m_parser.Peek("="))
                    m_parser.ConsumeString("=");

                if (item == "picname")
                    episodeDef.PicName = m_parser.ConsumeString();
                else if (item == "name")
                    episodeDef.Name = m_parser.ConsumeString();
                else if (item == "key")
                    episodeDef.Key = m_parser.ConsumeString();
                else if (item == "optional")
                    episodeDef.Optional = true;
                else
                {
                    // Warn we do not know what this is
                    m_parser.ConsumeLine();
                }
            }

            m_parser.ConsumeString("}");
            MapInfo.AddEpisode(episodeDef);
        }

        private GameInfoDef ParseGameInfo()
        {
            GameInfoDef gameDef = new();
            m_parser.ConsumeString("{");

            while (!m_parser.Peek('}'))
            {
                CIString item = m_parser.ConsumeString();
                if (m_parser.Peek("="))
                    m_parser.ConsumeString("=");

                if (item == "creditpage")
                    gameDef.CreditPages = GetStringList();
                else if (item == "finalepage")
                    gameDef.FinalePages = GetStringList();
                else if (item == "infopage")
                    gameDef.InfoPages = GetStringList();
                else if (item == "quitmessages")
                    gameDef.QuitMessages = GetStringList();
                else if (item == "titlemusic")
                    gameDef.TitleMusic = m_parser.ConsumeString();
                else if (item == "titletime")
                    gameDef.TitleTime = m_parser.ConsumeInteger();
                else if (item == "finalemusic")
                    gameDef.FinaleMusic = m_parser.ConsumeString();
                else if (item == "finaleflat")
                    gameDef.FinaleFlat = m_parser.ConsumeString();
                else if (item == "quitsound")
                    gameDef.QuitSound = m_parser.ConsumeString();
                else if (item == "borderflat")
                    gameDef.BorderFlat = m_parser.ConsumeString();
                else if (item == "drawreadthis")
                    gameDef.DrawReadThis = m_parser.ConsumeBool();
                else if (item == "intermissionmusic")
                    gameDef.IntermissionMusic = m_parser.ConsumeString();
                else
                {
                    // Warn we do not know what this is
                    m_parser.ConsumeLine();
                }
            }

            m_parser.ConsumeString("}");
            return gameDef;
        }

        private List<string> GetStringList()
        {
            List<string> items = new();
            int line = m_parser.GetCurrentLine();

            while (m_parser.GetCurrentLine() == line)
            {
                items.Add(m_parser.ConsumeString());

                if (m_parser.GetCurrentLine() == line)
                    m_parser.ConsumeString(",");
            }

            return items;
        }


        private void ParseInclude()
        {
            // Don't care for now
            m_parser.ConsumeString();
        }

        private void ConsumeThing()
        {
            throw new NotImplementedException();
        }
    }
}
