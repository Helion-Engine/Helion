using System;
using System.Collections.Generic;
using Helion.Util.Configs.Components;

namespace Helion.Resources.Definitions.MapInfo;

public partial class MapInfoDefinition
{
    private static readonly string GameInfoName = "gameinfo";
    private static readonly string ClearEpisodesName = "clearepisodes";
    private static readonly string EpisodeName = "episode";
    private static readonly string ClusterName = "cluster";
    private static readonly string ClusterDefName = "clusterdef";
    private static readonly string DefaultMapName = "defaultmap";
    private static readonly string AddDefaultMapName = "adddefaultmap";
    private static readonly string MapName = "map";
    private static readonly string SkillName = "skill";
    private static readonly string ClearSkillsName = "clearskills";

    private static readonly HashSet<string> HighLevelNames = new(StringComparer.OrdinalIgnoreCase)
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

    private static readonly string GameCreditPageName = "creditpage";
    private static readonly string GameFinalePageName = "finalepage";
    private static readonly string GameInfoPageName = "infopage";
    private static readonly string GameQuitMessagesName = "quitmessages";
    private static readonly string GameTitleMusicName = "titlemusic";
    private static readonly string GameTitleTimeName = "titletime";
    private static readonly string GamePageTimeName = "pagetime";
    private static readonly string GameFinaleMusicName = "finalemusic";
    private static readonly string GameFinaleFlatName = "finaleflat";
    private static readonly string GameQuitSoundName = "quitsound";
    private static readonly string GameBorderFlatName = "borderflat";
    private static readonly string GameDrawReadThisName = "drawreadthis";
    private static readonly string GameIntermissionMusicName = "intermissionmusic";
    private static readonly string GameWeaponSlotName = "WeaponSlot";
    private static readonly string GameDefKickBack = "DefKickback";
    private static readonly string GameSkyFlatName = "skyflatname";
    private static readonly string GameTitlePageName = "TitlePage";
    private static readonly string GameIntermissionCounterName = "IntermissionCounter";
    private static readonly string GameChatSoundName = "ChatSound";
    private static readonly string GameAdvisoryTimeName = "AdvisoryTime";
    private static readonly string GameTelefogHeightName = "TelefogHeight";


    private static readonly HashSet<string> GameInfoNames = new(StringComparer.OrdinalIgnoreCase)
    {
        GameCreditPageName,
        GameFinalePageName,
        GameInfoPageName,
        GameQuitMessagesName,
        GameTitleMusicName,
        GameTitleTimeName,
        GamePageTimeName,
        GameFinaleMusicName,
        GameFinaleFlatName,
        GameQuitSoundName,
        GameBorderFlatName,
        GameDrawReadThisName,
        GameIntermissionMusicName,
        GameWeaponSlotName,
        GameDefKickBack,
        GameSkyFlatName,
        GameTitlePageName,
        GameIntermissionCounterName,
        GameChatSoundName,
        GameAdvisoryTimeName,
        GameTelefogHeightName,

    };

    private static readonly string EpisodePicName = "picname";
    private static readonly string EpisodeEpName = "name";
    private static readonly string EpisodeKeyName = "key";

    private static readonly HashSet<string> EpisodeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        EpisodePicName,
        EpisodeEpName,
        EpisodeKeyName
    };

    private static readonly string MapLevelNumName = "levelnum";
    private static readonly string MapTitlePatchName = "titlepatch";
    private static readonly string MapNextName = "next";
    private static readonly string MapSecretName = "secretnext";
    private static readonly string MapSky1Name = "sky1";
    private static readonly string MapSky2Name = "sky2";
    private static readonly string MapClusterName = "cluster";
    private static readonly string MapParName = "par";
    private static readonly string MapSuckName = "sucktime";
    private static readonly string MapMusicName = "music";
    private static readonly string MapEndGame = "endgame";
    private static readonly string MapEnterPicName = "enterpic";
    private static readonly string MapExitPicName = "exitpic";
    private static readonly string MapEndPicName = "endpic";
    private static readonly string MapAuthorName = "author";

    private static readonly HashSet<string> MapNames = new(StringComparer.OrdinalIgnoreCase)
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
        MapMusicName,
        MapEnterPicName,
        MapExitPicName,
        MapEndPicName,
        MapAuthorName,
    };

    private static readonly string ClusterEnterTextName = "entertext";
    private static readonly string ClusterExitTextName = "exittext";
    private static readonly string ClusterExitTextIsLumpName = "exittextislump";
    private static readonly string ClusterMusicName = "music";
    private static readonly string ClusterFlatName = "flat";
    private static readonly string ClusterPicName = "pic";
    private static readonly string ClusterHubName = "hub";
    private static readonly string ClusterAllowIntermissionName = "allowintermission";

    private static readonly HashSet<string> ClusterNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ClusterEnterTextName,
        ClusterExitTextName,
        ClusterMusicName,
        ClusterMusicName,
        ClusterFlatName,
        ClusterPicName,
    };

    private static readonly string Skill_AmmoFactorName = "AmmoFactor";
    private static readonly string Skill_DropAmmoFactorName = "DropAmmoFactor";
    private static readonly string Skill_DoubleAmmoFactorName = "DoubleAmmoFactor";
    private static readonly string Skill_DamageFactorName = "DamageFactor";
    private static readonly string Skill_RespawnTimeName = "RespawnTime";
    private static readonly string Skill_RespawnLimit = "RespawnLimit";
    private static readonly string Skill_AggressivenessName = "Aggressiveness";
    private static readonly string Skill_SpawnFilterName = "SpawnFilter";
    private static readonly string Skill_ACSReturnName = "ACSReturn";
    private static readonly string Skill_KeyName = "Key";
    private static readonly string Skill_MustConfirmName = "MustConfirm";
    private static readonly string Skill_Name = "Name";
    private static readonly string Skill_PlayerClassNameName = "PlayerClassName";
    private static readonly string Skill_PicNameName = "PicName";
    private static readonly string Skill_TextColorName = "TextColorName";
    private static readonly string Skill_EasyBossBrainName = "EasyBossBrain";
    private static readonly string Skill_EasyKeyName = "EasyKey";
    private static readonly string Skill_FastMonstersName = "FastMonsters";
    private static readonly string Skill_SlowMonstersName = "SlowMonsters";
    private static readonly string Skill_DisableCheatsName = "DisableCheats";
    private static readonly string Skill_AutoUseHealthName = "AutoUseHealth";
    private static readonly string Skill_ReplaceActorName = "ReplaceActor";
    private static readonly string Skill_MonsterHealthName = "MonsterHealth";
    private static readonly string Skill_FriendlyHealthName = "FriendlyHealth";
    private static readonly string Skill_NoPainName = "NoPain";
    private static readonly string Skill_DefaultSkillName = "DefaultSkill";
    private static readonly string Skill_ArmorFactorName = "ArmorFactor";
    private static readonly string Skill_NoInfightingName = "NoInfighting";
    private static readonly string Skill_TotalInfightingName = "TotalInfighting";
    private static readonly string Skill_HealthFactorName = "HealthFactor";
    private static readonly string Skill_KickbackFactorName = "KickbackFactor";
    private static readonly string Skill_NoMenuName = "NoMenu";
    private static readonly string Skill_PlayerRespawnName = "PlayerRespawn";

    private static readonly HashSet<string> SkillNames = new(StringComparer.OrdinalIgnoreCase)
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

    private static readonly string EndGame_PicName = "pic";
    private static readonly string EndGame_MusicName = "music";
    private static readonly string EndGame_HScrollName = "hscroll";
    private static readonly string EndGame_VScollName = "vscroll";
    private static readonly string EndGame_CastName = "cast";

    private static readonly HashSet<string> EndGameNames = new(StringComparer.OrdinalIgnoreCase)
    {
        EndGame_PicName,
        EndGame_MusicName,
        EndGame_HScrollName,
        EndGame_VScollName,
        EndGame_CastName
    };

    private class MapOptionSet
    {
        public MapOptions Option { get; set; }
        public bool Value { get; set; }
    }

    private static readonly Dictionary<string, MapOptionSet> MapOptionsLookup = new(StringComparer.OrdinalIgnoreCase)
    {
        { "nojump",                 new MapOptionSet { Option = MapOptions.NoJump, Value = true } },
        { "allowjump",              new MapOptionSet { Option = MapOptions.NoJump, Value = false } },
        { "nocrouch",               new MapOptionSet { Option = MapOptions.NoCrouch, Value = true } },
        { "allowcouch",             new MapOptionSet { Option = MapOptions.NoCrouch, Value = false } },
        { "nofreelook",             new MapOptionSet { Option = MapOptions.NoFreelook, Value = true } },
        { "allowfreelook",          new MapOptionSet { Option = MapOptions.NoFreelook, Value = false } },
        { "nointermission",         new MapOptionSet { Option = MapOptions.NoIntermission, Value = true } },
        { "allowintermission",      new MapOptionSet { Option = MapOptions.NoIntermission, Value = false } },
        { "noclustertext",          new MapOptionSet { Option = MapOptions.NeedClusterText, Value = false } },
        { "needclustertext",        new MapOptionSet { Option = MapOptions.NeedClusterText, Value = true } },
        { "allowmonstertelefrags",  new MapOptionSet { Option = MapOptions.AllowMonsterTelefrags, Value = true } },
        { "compat_missileclip",     new MapOptionSet { Option = MapOptions.Compatibility_MissileClip, Value = true } },
        { "compat_shorttext",       new MapOptionSet { Option = MapOptions.Compatibility_ShortestTexture, Value = true } },
        { "compat_floormove",       new MapOptionSet { Option = MapOptions.Compatibility_FloorMove, Value = true } },
        { "compat_nopassover",      new MapOptionSet { Option = MapOptions.Compatibility_NoCrossOver, Value = true } },
        { "compat_limitpain",       new MapOptionSet { Option = MapOptions.Compatibility_LimitPain, Value = true } },
        { "compat_notossdrops",     new MapOptionSet { Option = MapOptions.COmpatibility_NoTossDrops, Value = true } },
    };
}
