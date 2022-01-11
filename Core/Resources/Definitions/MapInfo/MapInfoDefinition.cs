using Helion.Maps.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util.Parser;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Helion.Resources.Definitions.MapInfo;

public partial class MapInfoDefinition
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public MapInfo MapInfo { get; private set; } = new();
    public GameInfoDef GameDefinition { get; private set; } = new();

    private bool m_legacy;

    public void Parse(ArchiveCollection archiveCollection, string data)
    {
        m_legacy = true;
        SimpleParser parser = new SimpleParser();
        parser.Parse(data);

        while (!parser.IsDone())
        {
            string item = parser.ConsumeString();

            if (item.Equals("include", StringComparison.OrdinalIgnoreCase))
                ParseInclude(archiveCollection, parser);
            else if (item.Equals(GameInfoName, StringComparison.OrdinalIgnoreCase))
                ParseGameInfo(parser, GameDefinition);
            else if (item.Equals(ClearEpisodesName, StringComparison.OrdinalIgnoreCase))
                MapInfo.ClearEpisodes();
            else if (item.Equals(EpisodeName, StringComparison.OrdinalIgnoreCase))
                ParseEpisode(parser);
            else if (item.Equals(ClusterName, StringComparison.OrdinalIgnoreCase) || item.Equals(ClusterDefName, StringComparison.OrdinalIgnoreCase))
                MapInfo.AddCluster(ParseCluster(parser));
            else if (item.Equals(DefaultMapName, StringComparison.OrdinalIgnoreCase))
                MapInfo.SetDefaultMap(ParseMapDef(parser, false));
            else if (item.Equals(AddDefaultMapName, StringComparison.OrdinalIgnoreCase))
                ParseMapDef(parser, false, MapInfo.DefaultMap);
            else if (item.Equals(MapName, StringComparison.OrdinalIgnoreCase))
                MapInfo.AddMap(ParseMapDef(parser, true, (MapInfoDef)MapInfo.DefaultMap.Clone()));
            else if (item.Equals(SkillName, StringComparison.OrdinalIgnoreCase))
                MapInfo.AddSkill(ParseSkillDef(parser));
            else if (item.Equals(ClearSkillsName, StringComparison.OrdinalIgnoreCase))
                MapInfo.ClearSkills();
            else
                ConsumeUnknownSection(parser, item);
        }
    }

    private void ConsumeUnknownSection(SimpleParser parser, string item)
    {
        Log.Warn($"MapInfo: Unknown section {item}");

        if (m_legacy)
        {
            while (!IsBlockComplete(parser, false))
                parser.ConsumeLine();
        }
        else
        {
            int subCount = 1;
            while (!parser.Peek("{"))
                parser.ConsumeString();

            parser.ConsumeString();
            while (subCount != 0)
            {
                if (parser.Peek("{"))
                    subCount++;
                else if (parser.Peek("}"))
                    subCount--;

                parser.ConsumeLine();
            }
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

        while (!IsBlockComplete(parser, true))
        {
            int line = parser.GetCurrentLine();
            string item = parser.ConsumeString();
            if (MapNames.Contains(item))
            {
                ConsumeEquals(parser);

                if (item.Equals(MapLevelNumName, StringComparison.OrdinalIgnoreCase))
                    mapDef.LevelNumber = parser.ConsumeInteger();
                else if (item.Equals(MapTitlePatchName, StringComparison.OrdinalIgnoreCase))
                    mapDef.TitlePatch = parser.ConsumeString();
                else if (item.Equals(MapNextName, StringComparison.OrdinalIgnoreCase))
                {
                    mapDef.Next = parser.ConsumeString();
                    if (mapDef.Next.Equals(MapEndGame, StringComparison.OrdinalIgnoreCase))
                        mapDef.EndGame = ParseEndGame(parser);
                    else if (mapDef.Next.Equals(MapEndPicName, StringComparison.OrdinalIgnoreCase))
                        mapDef.EndPic = ParseEndPic(parser);
                }
                else if (item.Equals(MapSecretName, StringComparison.OrdinalIgnoreCase))
                {
                    mapDef.SecretNext = parser.ConsumeString();
                    if (mapDef.SecretNext.Equals(MapEndGame, StringComparison.OrdinalIgnoreCase))
                        mapDef.EndGameSecret = ParseEndGame(parser);
                }
                else if (item.Equals(MapSky1Name, StringComparison.OrdinalIgnoreCase))
                    mapDef.Sky1 = ParseMapSky(parser);
                else if (item.Equals(MapSky2Name, StringComparison.OrdinalIgnoreCase))
                    mapDef.Sky2 = ParseMapSky(parser);
                else if (item.Equals(MapClusterName, StringComparison.OrdinalIgnoreCase))
                    mapDef.Cluster = parser.ConsumeInteger();
                else if (item.Equals(MapParName, StringComparison.OrdinalIgnoreCase))
                    mapDef.ParTime = parser.ConsumeInteger();
                else if (item.Equals(MapSuckName, StringComparison.OrdinalIgnoreCase))
                    mapDef.SuckTime = parser.ConsumeInteger();
                else if (item.Equals(MapMusicName, StringComparison.OrdinalIgnoreCase))
                    mapDef.Music = parser.ConsumeString();
                else if (item.Equals(MapEnterPicName, StringComparison.OrdinalIgnoreCase))
                    mapDef.EnterPic = parser.ConsumeString();
                else if (item.Equals(MapExitPicName, StringComparison.OrdinalIgnoreCase))
                    mapDef.ExitPic = parser.ConsumeString();
            }
            else if (item.Equals("nosoundclipping", StringComparison.OrdinalIgnoreCase))
                continue; // Deprecated, no longer used
            else if (item.Equals("baronspecial", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecial = MapSpecial.BaronSpecial;
            else if (item.Equals("cyberdemonspecial", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecial = MapSpecial.CyberdemonSpecial;
            else if (item.Equals("spidermastermindspecial", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecial = MapSpecial.SpiderMastermindSpecial;
            else if (item.Equals("map07special", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecial = MapSpecial.Map07Special;
            else if (item.Equals("specialaction_lowerfloor", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecialAction = MapSpecialAction.LowerFloor;
            else if (item.Equals("specialaction_exitlevel", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecialAction = MapSpecialAction.ExitLevel;
            else if (item.Equals("specialaction_opendoor", StringComparison.OrdinalIgnoreCase))
                mapDef.MapSpecialAction = MapSpecialAction.OpenDoor;
            else if (item.Equals("normalinfighting", StringComparison.OrdinalIgnoreCase))
            {
                mapDef.SetOption(MapOptions.NoInfighting, false);
                mapDef.SetOption(MapOptions.TotalInfighting, false);
            }
            else if (item.Equals("noinfighting", StringComparison.OrdinalIgnoreCase))
            {
                mapDef.SetOption(MapOptions.NoInfighting, true);
                mapDef.SetOption(MapOptions.TotalInfighting, false);
            }
            else if (item.Equals("totalinfighting", StringComparison.OrdinalIgnoreCase))
            {
                mapDef.SetOption(MapOptions.NoInfighting, false);
                mapDef.SetOption(MapOptions.TotalInfighting, true);
            }
            else if (MapOptionsLookup.TryGetValue(item, out MapOptionSet? set))
            {
                mapDef.SetOption(set.Option, set.Value);
            }
            else
            {
                WarnMissing("map", item, line);
                if (line == parser.GetCurrentLine())
                    parser.ConsumeLine();
            }
        }

        ConsumeBrace(parser, false);
        return mapDef;
    }

    private static void WarnMissing(string def, string item, int line) =>
        Log.Warn($"MapInfo: Unknown {def} item: {item} line:{line}");

    private static string ParseEndPic(SimpleParser parser)
    {
        parser.ConsumeString(",");
        return parser.ConsumeString();
    }

    private EndGameDef ParseEndGame(SimpleParser parser)
    {
        EndGameDef endGameDef = new();
        ConsumeBrace(parser, true);

        while (!IsBlockComplete(parser, false))
        {
            int line = parser.GetCurrentLine();
            string item = parser.ConsumeString();
            if (EndGameNames.Contains(item))
            {
                if (item.Equals(EndGame_PicName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    endGameDef.Pic = parser.ConsumeString();
                }
                else if (item.Equals(EndGame_MusicName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    endGameDef.Music = parser.ConsumeString();
                }
                else if (item.Equals(EndGame_HScrollName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    endGameDef.HorizontalScroll = GetEndGameHScroll(parser);
                }
                else if (item.Equals(EndGame_VScollName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    endGameDef.VerticalScroll = GetEndGameVScroll(parser);
                }
                else if (item.Equals(EndGame_CastName, StringComparison.OrdinalIgnoreCase))
                {
                    endGameDef.Cast = true;
                }
            }
            else
            {
                WarnMissing("endgame", item, line);
                if (line == parser.GetCurrentLine())
                    parser.ConsumeLine();
            }
        }

        ConsumeBrace(parser, false);
        return endGameDef;
    }

    private static VerticalScroll GetEndGameVScroll(SimpleParser parser)
    {
        string data = parser.ConsumeString();
        if (data.StartsWith("top", StringComparison.OrdinalIgnoreCase))
            return VerticalScroll.Top;
        if (data.StartsWith("bottom", StringComparison.OrdinalIgnoreCase))
            return VerticalScroll.Bottom;

        throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Invalid vscroll {data}");
    }

    private static HorizontalScroll GetEndGameHScroll(SimpleParser parser)
    {
        string data = parser.ConsumeString();
        if (data.StartsWith("left", StringComparison.OrdinalIgnoreCase))
            return HorizontalScroll.Left;
        if (data.StartsWith("right", StringComparison.OrdinalIgnoreCase))
            return HorizontalScroll.Right;

        throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Invalid vscroll {data}");
    }

    private static SkyDef ParseMapSky(SimpleParser parser)
    {
        SkyDef sky = new();
        sky.Name = parser.ConsumeString();
        if (!MapNames.Contains(parser.PeekString()) && parser.PeekInteger(out _))
            sky.ScrollSpeed = parser.ConsumeInteger();
        else if (parser.ConsumeIf(","))
            sky.ScrollSpeed = parser.ConsumeInteger();
        return sky;
    }

    private ClusterDef ParseCluster(SimpleParser parser)
    {
        ClusterDef clusterDef = new();
        clusterDef.ClusterNum = parser.ConsumeInteger();

        ConsumeBrace(parser, true);

        while (!IsBlockComplete(parser, false))
        {
            int line = parser.GetCurrentLine();
            string item = parser.ConsumeString();
            if (ClusterNames.Contains(item))
            {
                ConsumeEquals(parser);

                if (item.Equals(ClusterEnterTextName, StringComparison.OrdinalIgnoreCase))
                    clusterDef.EnterText = GetClusterText(parser);
                else if (item.Equals(ClusterExitTextName, StringComparison.OrdinalIgnoreCase))
                    clusterDef.ExitText = GetClusterText(parser);
                else if (item.Equals(ClusterMusicName, StringComparison.OrdinalIgnoreCase))
                    clusterDef.Music = parser.ConsumeString();
                else if (item.Equals(ClusterFlatName, StringComparison.OrdinalIgnoreCase))
                    clusterDef.Flat = parser.ConsumeString();
                else if (item.Equals(ClusterPicName, StringComparison.OrdinalIgnoreCase))
                    clusterDef.Pic = parser.ConsumeString();
            }
            else if (item.Equals(ClusterExitTextIsLumpName, StringComparison.OrdinalIgnoreCase))
                clusterDef.IsExitTextLump = true;
            else if (item.Equals(ClusterHubName, StringComparison.OrdinalIgnoreCase))
                clusterDef.IsHub = true;
            else if (item.Equals(ClusterAllowIntermissionName, StringComparison.OrdinalIgnoreCase))
                clusterDef.AllowIntermission = true;
            else
            {
                WarnMissing("cluster", item, line);
                if (line == parser.GetCurrentLine())
                    parser.ConsumeLine();
            }
        }

        ConsumeBrace(parser, false);

        return clusterDef;
    }

    private List<string> GetClusterText(SimpleParser parser)
    {
        List<string> textItems = new List<string>();
        while (!ClusterNames.Contains(parser.PeekString()))
        {
            string text = parser.ConsumeString();
            bool hasComma = parser.Peek(',');
            if (hasComma)
                parser.Consume(',');

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

        while (!IsBlockComplete(parser, false))
        {
            int line = parser.GetCurrentLine();
            string item = parser.ConsumeString();
            if (EpisodeNames.Contains(item))
            {
                ConsumeEquals(parser);
                if (item.Equals(EpisodePicName, StringComparison.OrdinalIgnoreCase))
                    episodeDef.PicName = parser.ConsumeString();
                else if (item.Equals(EpisodeEpName, StringComparison.OrdinalIgnoreCase))
                    episodeDef.Name = parser.ConsumeString();
                else if (item.Equals(EpisodeKeyName, StringComparison.OrdinalIgnoreCase))
                    episodeDef.Key = parser.ConsumeString();
            }
            else if (item.Equals("optional", StringComparison.OrdinalIgnoreCase))
                episodeDef.Optional = true;
            else
            {
                WarnMissing("episode", item, line);
                if (line == parser.GetCurrentLine())
                    parser.ConsumeLine();
            }
        }

        ConsumeBrace(parser, false);
        MapInfo.AddEpisode(episodeDef);
    }

    private void ParseGameInfo(SimpleParser parser, GameInfoDef gameDef)
    {
        ConsumeBrace(parser, true);

        while (!IsBlockComplete(parser, false))
        {
            int line = parser.GetCurrentLine();
            string item = parser.ConsumeString();
            if (GameInfoNames.Contains(item))
            {
                ConsumeEquals(parser);

                if (item.Equals(GameCreditPageName, StringComparison.OrdinalIgnoreCase))
                    gameDef.CreditPages = GetStringList(parser);
                else if (item.Equals(GameFinalePageName, StringComparison.OrdinalIgnoreCase))
                    gameDef.FinalePages = GetStringList(parser);
                else if (item.Equals(GameInfoPageName, StringComparison.OrdinalIgnoreCase))
                    gameDef.InfoPages = GetStringList(parser);
                else if (item.Equals(GameQuitMessagesName, StringComparison.OrdinalIgnoreCase))
                    gameDef.QuitMessages = GetStringList(parser);
                else if (item.Equals(GameTitleMusicName, StringComparison.OrdinalIgnoreCase))
                    gameDef.TitleMusic = parser.ConsumeString();
                else if (item.Equals(GameTitleTimeName, StringComparison.OrdinalIgnoreCase))
                    gameDef.TitleTime = parser.ConsumeInteger();
                else if (item.Equals(GamePageTimeName, StringComparison.OrdinalIgnoreCase))
                    gameDef.PageTime = parser.ConsumeInteger();
                else if (item.Equals(GameFinaleMusicName, StringComparison.OrdinalIgnoreCase))
                    gameDef.FinaleMusic = parser.ConsumeString();
                else if (item.Equals(GameFinaleFlatName, StringComparison.OrdinalIgnoreCase))
                    gameDef.FinaleFlat = parser.ConsumeString();
                else if (item.Equals(GameQuitSoundName, StringComparison.OrdinalIgnoreCase))
                    gameDef.QuitSound = parser.ConsumeString();
                else if (item.Equals(GameBorderFlatName, StringComparison.OrdinalIgnoreCase))
                    gameDef.BorderFlat = parser.ConsumeString();
                else if (item.Equals(GameDrawReadThisName, StringComparison.OrdinalIgnoreCase))
                    gameDef.DrawReadThis = parser.ConsumeBool();
                else if (item.Equals(GameIntermissionMusicName, StringComparison.OrdinalIgnoreCase))
                    gameDef.IntermissionMusic = parser.ConsumeString();
                else if (item.Equals(GameDefKickBack, StringComparison.OrdinalIgnoreCase))
                    gameDef.DefKickBack = parser.ConsumeInteger();
                else if (item.Equals(GameSkyFlatName, StringComparison.OrdinalIgnoreCase))
                    gameDef.SkyFlatName = parser.ConsumeString();
                else if (item.Equals(GameTitlePageName, StringComparison.OrdinalIgnoreCase))
                    gameDef.TitlePage = parser.ConsumeString();
                else if (item.Equals(GameIntermissionCounterName, StringComparison.OrdinalIgnoreCase))
                    gameDef.IntermissionCounter = parser.ConsumeBool();
                else if (item.Equals(GameChatSoundName, StringComparison.OrdinalIgnoreCase))
                    gameDef.ChatSound = parser.ConsumeString();
                else if (item.Equals(GameAdvisoryTimeName, StringComparison.OrdinalIgnoreCase))
                    gameDef.AdvisoryTime = parser.ConsumeInteger();
                else if (item.Equals(GameTelefogHeightName, StringComparison.OrdinalIgnoreCase))
                    gameDef.TelefogHeight = parser.ConsumeInteger();
                else if (item.Equals(GameWeaponSlotName, StringComparison.OrdinalIgnoreCase))
                    ParseWeaponSlot(gameDef, parser);
            }
            else
            {
                WarnMissing("gameinfo", item, line);
                if (line == parser.GetCurrentLine())
                    parser.ConsumeLine();
            }
        }

        ConsumeBrace(parser, false);
    }

    private static void ParseWeaponSlot(GameInfoDef gameDef, SimpleParser parser)
    {
        int slot = parser.ConsumeInteger();
        if (gameDef.WeaponSlots.ContainsKey(slot))
            gameDef.WeaponSlots[slot].Clear();
        else
            gameDef.WeaponSlots.Add(slot, new List<string>());

        parser.Consume(',');
        gameDef.WeaponSlots[slot].AddRange(GetStringList(parser));
    }

    private SkillDef ParseSkillDef(SimpleParser parser)
    {
        SkillDef skillDef = new();
        skillDef.SkillName = parser.ConsumeString();
        ConsumeBrace(parser, true);

        while (!IsBlockComplete(parser, false))
        {
            int line = parser.GetCurrentLine();
            string item = parser.ConsumeString();
            if (SkillNames.Contains(item))
            {
                if (item.Equals(Skill_AmmoFactorName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.AmmoFactor = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_DropAmmoFactorName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.DropAmmoFactor = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_DoubleAmmoFactorName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.DoubleAmmoFactor = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_DamageFactorName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.DamageFator = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_RespawnTimeName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.RespawnTime = TimeSpan.FromSeconds(parser.ConsumeInteger());
                }
                else if (item.Equals(Skill_RespawnLimit, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.RespawnLimit = parser.ConsumeInteger();
                }
                else if (item.Equals(Skill_AggressivenessName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.MonsterAggressiveness = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_SpawnFilterName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.SpawnFilter = ParseSpawnFilter(parser);
                }
                else if (item.Equals(Skill_ACSReturnName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.ACSReturn = parser.ConsumeString();
                }
                else if (item.Equals(Skill_KeyName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.Key = parser.ConsumeString();
                }
                else if (item.Equals(Skill_Name, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.Name = parser.ConsumeString();
                }
                else if (item.Equals(Skill_PlayerClassNameName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.PlayerClassName = parser.ConsumeString();
                }
                else if (item.Equals(Skill_PicNameName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.PicName = parser.ConsumeString();
                }
                else if (item.Equals(Skill_TextColorName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.TextColor = Color.FromName(parser.ConsumeString());
                }
                else if (item.Equals(Skill_MonsterHealthName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.MonsterHealthFactor = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_FriendlyHealthName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsumeEquals(parser);
                    skillDef.FriendlyHealthFactor = parser.ConsumeDouble();
                }
                else if (item.Equals(Skill_EasyBossBrainName, StringComparison.OrdinalIgnoreCase))
                    skillDef.EasyBossBrain = true;
                else if (item.Equals(Skill_EasyKeyName, StringComparison.OrdinalIgnoreCase))
                    skillDef.EasyKey = true;
                else if (item.Equals(Skill_FastMonstersName, StringComparison.OrdinalIgnoreCase))
                    skillDef.FastMonsters = true;
                else if (item.Equals(Skill_SlowMonstersName, StringComparison.OrdinalIgnoreCase))
                    skillDef.SlowMonsters = true;
                else if (item.Equals(Skill_DisableCheatsName, StringComparison.OrdinalIgnoreCase))
                    skillDef.DisableCheats = true;
                else if (item.Equals(Skill_AutoUseHealthName, StringComparison.OrdinalIgnoreCase))
                    skillDef.AutoUseHealth = true;
                else if (item.Equals(Skill_NoPainName, StringComparison.OrdinalIgnoreCase))
                    skillDef.NoPain = true;
                else if (item.Equals(Skill_DefaultSkillName, StringComparison.OrdinalIgnoreCase))
                    skillDef.Default = true;
                else if (item.Equals(Skill_ArmorFactorName, StringComparison.OrdinalIgnoreCase))
                    skillDef.ArmorFactor = parser.ConsumeDouble();
                else if (item.Equals(Skill_NoInfightingName, StringComparison.OrdinalIgnoreCase))
                    skillDef.NoInfighting = true;
                else if (item.Equals(Skill_TotalInfightingName, StringComparison.OrdinalIgnoreCase))
                    skillDef.TotalInfighting = true;
                else if (item.Equals(Skill_HealthFactorName, StringComparison.OrdinalIgnoreCase))
                    skillDef.HealthFactor = parser.ConsumeDouble();
                else if (item.Equals(Skill_KickbackFactorName, StringComparison.OrdinalIgnoreCase))
                    skillDef.KickbackFactor = parser.ConsumeDouble();
                else if (item.Equals(Skill_NoMenuName, StringComparison.OrdinalIgnoreCase))
                    skillDef.NoMenu = true;
                else if (item.Equals(Skill_PlayerRespawnName, StringComparison.OrdinalIgnoreCase))
                    skillDef.PlayerRespawn = true;
                else if (item.Equals(Skill_MustConfirmName, StringComparison.OrdinalIgnoreCase))
                    skillDef.MustConfirm = true;
            }
            else
            {
                WarnMissing("skill", item, line);
                if (line == parser.GetCurrentLine())
                    parser.ConsumeLine();
            }
        }

        ConsumeBrace(parser, false);

        return skillDef;
    }

    private static int ParseSpawnFilter(SimpleParser parser)
    {
        string filter = parser.ConsumeString();
        if (int.TryParse(filter.ToString(), out int i))
            return i;

        if (filter.Equals("baby", StringComparison.OrdinalIgnoreCase))
            return (int)SkillLevel.VeryEasy;
        else if (filter.Equals("easy", StringComparison.OrdinalIgnoreCase))
            return (int)SkillLevel.Easy;
        else if (filter.Equals("normal", StringComparison.OrdinalIgnoreCase))
            return (int)SkillLevel.Medium;
        else if (filter.Equals("hard", StringComparison.OrdinalIgnoreCase))
            return (int)SkillLevel.Hard;
        else if (filter.Equals("nightmare", StringComparison.OrdinalIgnoreCase))
            return (int)SkillLevel.Nightmare;

        throw new ParserException(parser.GetCurrentLine(), 0, 0, $"Invalid spawn filter {filter}");
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

    private bool IsBlockComplete(SimpleParser parser, bool isMapInfo)
    {
        if (m_legacy)
        {
            if (parser.IsDone())
                return true;

            // Legacy mapinfo has a cluster definition and is also a valid map property...
            if (isMapInfo && parser.Peek(ClusterName))
                return false;

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

    private static IList<string> GetStringList(SimpleParser parser)
    {
        List<string> items = new List<string>();

        while (true)
        {
            items.Add(parser.ConsumeString());
            if (parser.Peek(','))
                parser.Consume(',');
            else
                break;
        }

        return items;
    }

    private void ParseInclude(ArchiveCollection archiveCollection, SimpleParser parser)
    {
        string file = parser.ConsumeString();
        Entry? entry = archiveCollection.Entries.FindByPath(file);

        if (entry == null)
            throw new ParserException(parser.GetCurrentLine(), 0, 0, $"Failed to find include file {file}");

        Parse(archiveCollection, entry.ReadDataAsString());
    }
}
