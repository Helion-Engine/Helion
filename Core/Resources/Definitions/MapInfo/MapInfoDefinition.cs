using Helion.Maps.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Parser;
using System;
using System.Collections.Generic;
using System.Drawing;

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
        private static readonly CIString ClusterDefName = "clusterdef";
        private static readonly CIString DefaultMapName = "defaultmap";
        private static readonly CIString AddDefaultMapName = "adddefaultmap";
        private static readonly CIString MapName = "map";
        private static readonly CIString SkillName = "skill";
        private static readonly CIString ClearSkillsName = "clearskills";

        private static readonly HashSet<CIString> HighLevelNames = new HashSet<CIString>
        {
            GameInfoName,
            ClearEpisodesName,
            EpisodeName,
            ClusterName,
            ClusterDefName,
            DefaultMapName,
            AddDefaultMapName,
            MapName,
            SkillName,
            ClearSkillsName
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
        private static readonly CIString GameWeaponSlotName = "WeaponSlot";

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
            GameWeaponSlotName
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
        private static readonly CIString MapEndGame = "endgame";

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

        private static readonly CIString Skill_AmmoFactorName = "AmmoFactor";
        private static readonly CIString Skill_DropAmmoFactorName = "DropAmmoFactor";
        private static readonly CIString Skill_DoubleAmmoFactorName = "DoubleAmmoFactor";
        private static readonly CIString Skill_DamageFactorName = "DamageFactor";
        private static readonly CIString Skill_RespawnTimeName = "RespawnTime";
        private static readonly CIString Skill_RespawnLimit= "RespawnLimit";
        private static readonly CIString Skill_AggressivenessName = "Aggressiveness";
        private static readonly CIString Skill_SpawnFilterName = "SpawnFilter";
        private static readonly CIString Skill_ACSReturnName = "ACSReturn";
        private static readonly CIString Skill_KeyName = "Key";
        private static readonly CIString Skill_MustConfirmName = "MustConfirm";
        private static readonly CIString Skill_Name = "Name";
        private static readonly CIString Skill_PlayerClassNameName = "PlayerClassName";
        private static readonly CIString Skill_PicNameName = "PicName";
        private static readonly CIString Skill_TextColorName = "TextColorName";
        private static readonly CIString Skill_EasyBossBrainName = "EasyBossBrain";
        private static readonly CIString Skill_EasyKeyName = "EasyKey";
        private static readonly CIString Skill_FastMonstersName = "FastMonsters";
        private static readonly CIString Skill_SlowMonstersName = "SlowMonsters";
        private static readonly CIString Skill_DisableCheatsName = "DisableCheats";
        private static readonly CIString Skill_AutoUseHealthName = "AutoUseHealth";
        private static readonly CIString Skill_ReplaceActorName = "ReplaceActor";
        private static readonly CIString Skill_MonsterHealthName = "MonsterHealth";
        private static readonly CIString Skill_FriendlyHealthName = "FriendlyHealth";
        private static readonly CIString Skill_NoPainName = "NoPain";
        private static readonly CIString Skill_DefaultSkillName = "DefaultSkill";
        private static readonly CIString Skill_ArmorFactorName = "ArmorFactor";
        private static readonly CIString Skill_NoInfightingName = "NoInfighting";
        private static readonly CIString Skill_TotalInfightingName = "TotalInfighting";
        private static readonly CIString Skill_HealthFactorName = "HealthFactor";
        private static readonly CIString Skill_KickbackFactorName = "KickbackFactor";
        private static readonly CIString Skill_NoMenuName = "NoMenu";
        private static readonly CIString Skill_PlayerRespawnName = "PlayerRespawn";

        private static readonly HashSet<CIString> SkillNames = new HashSet<CIString>()
        {
            Skill_AmmoFactorName,
            Skill_DropAmmoFactorName,
            Skill_DoubleAmmoFactorName,
            Skill_DamageFactorName,
            Skill_RespawnTimeName,
            Skill_RespawnLimit,
            Skill_AggressivenessName,
            Skill_SpawnFilterName,
            Skill_ACSReturnName,
            Skill_KeyName,
            Skill_MustConfirmName,
            Skill_Name,
            Skill_PlayerClassNameName,
            Skill_PicNameName,
            Skill_TextColorName,
            Skill_EasyBossBrainName,
            Skill_EasyKeyName,
            Skill_FastMonstersName,
            Skill_SlowMonstersName,
            Skill_DisableCheatsName,
            Skill_AutoUseHealthName,
            Skill_ReplaceActorName,
            Skill_MonsterHealthName,
            Skill_FriendlyHealthName,
            Skill_NoPainName,
            Skill_DefaultSkillName,
            Skill_ArmorFactorName,
            Skill_NoInfightingName,
            Skill_TotalInfightingName,
            Skill_HealthFactorName,
            Skill_KickbackFactorName,
            Skill_NoMenuName,
            Skill_PlayerRespawnName
        };

        private static readonly CIString EndGame_PicName = "pic";
        private static readonly CIString EndGame_MusicName = "music";
        private static readonly CIString EndGame_HScrollName = "hscroll";
        private static readonly CIString EndGame_VScollName = "vscroll";
        private static readonly CIString EndGame_CastName = "cast";

        private static readonly HashSet<CIString> EndGameNames = new HashSet<CIString>()
        {
            EndGame_PicName,
            EndGame_MusicName,
            EndGame_HScrollName,
            EndGame_VScollName,
            EndGame_CastName
        };

        private bool m_legacy;

        public void Parse(ArchiveCollection archiveCollection, string data)
        {
            m_legacy = true;
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            while (!parser.IsDone())
            {
                CIString item = parser.ConsumeString();

                if (item == "include")
                    ParseInclude(archiveCollection, parser);
                else if (item == GameInfoName)
                    ParseGameInfo(parser, GameDefinition);
                else if (item == ClearEpisodesName)
                    MapInfo.ClearEpisodes();
                else if (item == EpisodeName)
                    ParseEpisode(parser);
                else if (item == ClusterName || item == ClusterDefName)
                    MapInfo.AddCluster(ParseCluster(parser));
                else if (item == DefaultMapName)
                    MapInfo.SetDefaultMap(ParseMapDef(parser, false));
                else if (item == AddDefaultMapName)
                    ParseMapDef(parser, false, MapInfo.DefaultMap);
                else if (item == MapName)
                    MapInfo.AddMap(ParseMapDef(parser, true, (MapInfoDef)MapInfo.DefaultMap.Clone()));
                else if (item == SkillName)
                    MapInfo.AddSkill(ParseSkillDef(parser));
                else if (item == ClearSkillsName)
                    MapInfo.ClearSkills();
                else
                    throw new ParserException(parser.GetCurrentLine(), parser.GetCurrentCharOffset(), 0, $"Unknown item {item}");
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
                    {
                        mapDef.Next = parser.ConsumeString();
                        if (mapDef.Next == MapEndGame)
                            mapDef.EndGame = ParseEndGame(parser);
                    }
                    else if (item == MapSecretName)
                    {
                        mapDef.SecretNext = parser.ConsumeString();
                        if (mapDef.SecretNext == MapEndGame)
                            mapDef.EndGameSecret = ParseEndGame(parser);
                    }
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

        private EndGameDef ParseEndGame(SimpleParser parser)
        {
            EndGameDef endGameDef = new();
            ConsumeBrace(parser, true);

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();

                if (EndGameNames.Contains(item))
                {
                    if (item == EndGame_PicName)
                    {
                        ConsumeEquals(parser);
                        endGameDef.Pic = parser.ConsumeString();
                    }
                    else if (item == EndGame_MusicName)
                    {
                        ConsumeEquals(parser);
                        endGameDef.Music = parser.ConsumeString();
                    }
                    else if (item == EndGame_HScrollName)
                    {
                        ConsumeEquals(parser);
                        endGameDef.HorizontalScroll = GetEndGameHScroll(parser);
                    }
                    else if (item == EndGame_VScollName)
                    {
                        ConsumeEquals(parser);
                        endGameDef.VerticalScroll = GetEndGameVScroll(parser);
                    }
                    else if (item == EndGame_CastName)
                    {
                        endGameDef.Cast = true;
                    }
                }
                else
                {
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
            return sky;
        }

        private ClusterDef ParseCluster(SimpleParser parser)
        {
            ClusterDef clusterDef = new();
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

        private void ParseGameInfo(SimpleParser parser, GameInfoDef gameDef)
        {
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
                    else if (item == GameWeaponSlotName)
                        ParseWeaponSlot(gameDef, parser);
                }
                else
                {
                    // Warn we do not know what this is
                    parser.ConsumeLine();
                }
            }

            ConsumeBrace(parser, false);
        }

        private void ParseWeaponSlot(GameInfoDef gameDef, SimpleParser parser)
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

            while (!IsBlockComplete(parser))
            {
                CIString item = parser.ConsumeString();

                if (SkillNames.Contains(item))
                {
                    if (item == Skill_AmmoFactorName)
                    {
                        ConsumeEquals(parser);
                        skillDef.AmmoFactor = parser.ConsumeDouble();
                    }
                    else if (item == Skill_DropAmmoFactorName)
                    {
                        ConsumeEquals(parser);
                        skillDef.DropAmmoFactor = parser.ConsumeDouble();
                    }
                    else if (item == Skill_DoubleAmmoFactorName)
                    {
                        ConsumeEquals(parser);
                        skillDef.DoubleAmmoFactor = parser.ConsumeDouble();
                    }
                    else if (item == Skill_DamageFactorName)
                    {
                        ConsumeEquals(parser);
                        skillDef.DamageFator = parser.ConsumeDouble();
                    }
                    else if (item == Skill_RespawnTimeName)
                    {
                        ConsumeEquals(parser);
                        skillDef.RespawnTime = TimeSpan.FromSeconds(parser.ConsumeInteger());
                    }
                    else if (item == Skill_RespawnLimit)
                    {
                        ConsumeEquals(parser);
                        skillDef.RespawnLimit = parser.ConsumeInteger();
                    }
                    else if (item == Skill_AggressivenessName)
                    {
                        ConsumeEquals(parser);
                        skillDef.MonsterAggressiveness = parser.ConsumeDouble();
                    }
                    else if (item == Skill_SpawnFilterName)
                    {
                        ConsumeEquals(parser);
                        skillDef.SpawnFilter = ParseSpawnFilter(parser);
                    }
                    else if (item == Skill_ACSReturnName)
                    {
                        ConsumeEquals(parser);
                        skillDef.ACSReturn = parser.ConsumeString();
                    }
                    else if (item == Skill_KeyName)
                    {
                        ConsumeEquals(parser);
                        skillDef.Key = parser.ConsumeString();
                    }
                    else if (item == Skill_Name)
                    {
                        ConsumeEquals(parser);
                        skillDef.Name = parser.ConsumeString();
                    }
                    else if (item == Skill_PlayerClassNameName)
                    {
                        ConsumeEquals(parser);
                        skillDef.PlayerClassName = parser.ConsumeString();
                    }
                    else if (item == Skill_PicNameName)
                    {
                        ConsumeEquals(parser);
                        skillDef.PicName = parser.ConsumeString();
                    }
                    else if (item == Skill_TextColorName)
                    {
                        ConsumeEquals(parser);
                        skillDef.TextColor = Color.FromName(parser.ConsumeString());
                    }
                    else if (item == Skill_MonsterHealthName)
                    {
                        ConsumeEquals(parser);
                        skillDef.MonsterHealthFactor = parser.ConsumeDouble();
                    }
                    else if (item == Skill_FriendlyHealthName)
                    {
                        ConsumeEquals(parser);
                        skillDef.FriendlyHealthFactor = parser.ConsumeDouble();
                    }
                    else if (item == Skill_EasyBossBrainName)
                        skillDef.EasyBossBrain = true;
                    else if (item == Skill_EasyKeyName)
                        skillDef.EasyKey = true;
                    else if (item == Skill_FastMonstersName)
                        skillDef.FastMonsters = true;
                    else if (item == Skill_SlowMonstersName)
                        skillDef.SlowMonsters = true;
                    else if (item == Skill_DisableCheatsName)
                        skillDef.DisableCheats = true;
                    else if (item == Skill_AutoUseHealthName)
                        skillDef.AutoUseHealth = true;
                    else if (item == Skill_NoPainName)
                        skillDef.NoPain = true;
                    else if (item == Skill_DefaultSkillName)
                        skillDef.Default = true;
                    else if (item == Skill_ArmorFactorName)
                        skillDef.ArmorFactor = parser.ConsumeDouble();
                    else if (item == Skill_NoInfightingName)
                        skillDef.NoInfighting = true;
                    else if (item == Skill_TotalInfightingName)
                        skillDef.TotalInfighting = true;
                    else if (item == Skill_HealthFactorName)
                        skillDef.HealthFactor = parser.ConsumeDouble();
                    else if (item == Skill_KickbackFactorName)
                        skillDef.KickbackFactor = parser.ConsumeDouble();
                    else if (item == Skill_NoMenuName)
                        skillDef.NoMenu = true;
                    else if (item == Skill_PlayerRespawnName)
                        skillDef.PlayerRespawn = true;
                    else if (item == Skill_MustConfirmName)
                        skillDef.MustConfirm = true;
                }
                else
                {
                    // Warn we do not know what this is
                    parser.ConsumeLine();
                }
            }

            ConsumeBrace(parser, false);

            return skillDef;
        }

        private int ParseSpawnFilter(SimpleParser parser)
        {
            CIString filter = parser.ConsumeString();
            if (int.TryParse(filter.ToString(), out int i))
                return i;

            if (filter == "baby")
                return (int)SkillLevel.VeryEasy;
            else if (filter == "easy")
                return (int)SkillLevel.Easy;
            else if (filter == "normal")
                return (int)SkillLevel.Medium;
            else if (filter == "hard")
                return (int)SkillLevel.Hard;
            else if (filter == "nightmare")
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

        private IList<string> GetStringList(SimpleParser parser)
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
}
