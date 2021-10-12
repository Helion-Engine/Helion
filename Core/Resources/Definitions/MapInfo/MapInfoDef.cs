using System;
using System.Collections;

namespace Helion.Resources.Definitions.MapInfo;

public class MapInfoDef : ICloneable
{
    private BitArray m_levelOptions = new(Enum.GetValues(typeof(MapOptions)).Length);

    public string Map { get; set; } = string.Empty;
    public string MapName { get; set; } = string.Empty;
    public string TitlePatch { get; set; } = string.Empty;
    public string Next { get; set; } = string.Empty;
    public string SecretNext { get; set; } = string.Empty;
    public SkyDef Sky1 { get; set; } = new();
    public SkyDef Sky2 { get; set; } = new();
    public string Music { get; set; } = string.Empty;
    public string LookupName { get; set; } = string.Empty;
    public string NiceName { get; set; } = string.Empty;
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

    public bool HasOption(MapOptions option) => m_levelOptions[(int)option];
    public void SetOption(MapOptions option, bool set) => m_levelOptions[(int)option] = set;

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
            EndGame = EndGame,
            EnterPic = EnterPic,
            ExitPic = ExitPic,
            EndPic = EndPic,

            Sky1 = (SkyDef)Sky1.Clone(),
            Sky2 = (SkyDef)Sky2.Clone()
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

