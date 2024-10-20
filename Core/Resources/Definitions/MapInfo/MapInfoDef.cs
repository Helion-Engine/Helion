using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Resources.Definitions.MapInfo;

public class MapInfoDef : ICloneable
{
    private BitArray m_levelOptions = new(Enum.GetValues<MapOptions>().Length);
    private bool m_hasOptions;

    public string Map { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? DisplayNameWithPrefix { get; set; }
    public string TitlePatch { get; set; } = string.Empty;
    public string Next { get; set; } = string.Empty;
    public string SecretNext { get; set; } = string.Empty;
    public SkyDef Sky1 { get; set; } = new();
    public SkyDef Sky2 { get; set; } = new();
    public string Music { get; set; } = string.Empty;
    public string LookupName { get; set; } = string.Empty;
    public string NiceName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int LevelNumber { get; set; }
    public int Cluster { get; set; }
    public int ParTime { get; set; }
    public int SuckTime { get; set; }
    public MapSpecial MapSpecial { get; set; }
    public MapSpecialAction MapSpecialAction { get; set; }
    public EndGameDef? EndGame { get; set; }
    public EndGameDef? EndGameSecret { get; set; }
    public string EnterPic { get; set; } = string.Empty;
    public string ExitPic { get; set; } = string.Empty;
    public string EndPic { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<BossAction> BossActions { get; set; } = [];
    public ClusterDef? ClusterDef { get; set; }

    public bool HasOption(MapOptions option) => m_levelOptions[(int)option];
    public void SetOption(MapOptions option, bool set)
    {
        if (set)
            m_hasOptions = true;
        m_levelOptions[(int)option] = set;
    }
    public bool HasOptions() => m_hasOptions;
    public void SetOptions(MapInfoDef mapInfo) => m_levelOptions = (BitArray)mapInfo.m_levelOptions.Clone();

    public object Clone()
    {
        return new MapInfoDef
        {
            Map = Map,
            MapName = MapName,
            TitlePatch = TitlePatch,
            Next = Next,
            SecretNext = SecretNext,
            Music = Music,
            LookupName = LookupName,
            LevelNumber = LevelNumber,
            Cluster = Cluster,
            ParTime = ParTime,
            SuckTime = SuckTime,
            MapSpecial = MapSpecial,
            MapSpecialAction = MapSpecialAction,
            m_levelOptions = (BitArray)m_levelOptions.Clone(),
            m_hasOptions = m_hasOptions,
            EndGame = EndGame,
            EnterPic = EnterPic,
            ExitPic = ExitPic,
            EndPic = EndPic,

            Sky1 = (SkyDef)Sky1.Clone(),
            Sky2 = (SkyDef)Sky2.Clone(),

            BossActions = [.. BossActions],
            ClusterDef = ClusterDef,
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is MapInfoDef map && map.MapName.Equals(MapName, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public override int GetHashCode()
    {
        return MapName.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
}
