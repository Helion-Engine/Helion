using Helion.Maps.Shared;
using Helion.Util.Container;
using Helion.World;
using System;
using System.Collections.Generic;

namespace Helion.Models;

public class WorldModel
{
    public GameFilesModel Files { get; set; }
    public IList<ConfigValueModel> ConfigValues { get; set; } = Array.Empty<ConfigValueModel>();

    public string MapName { get; set; } = string.Empty;
    public WorldState WorldState { get; set; }
    public int Gametick { get; set; }
    public int LevelTime { get; set; }
    public int SoundCount { get; set; }
    public int RandomIndex { get; set; }
    public double Gravity { get; set; }
    public SkillLevel Skill { get; set; }
    public int CurrentBossTarget { get; set; }
    public IList<PlayerModel> Players { get; set; } = Array.Empty<PlayerModel>();
    public IList<EntityModel> Entities { get; set; } = Array.Empty<EntityModel>();
    public IList<SectorModel> Sectors { get; set; } = Array.Empty<SectorModel>();
    public IList<LineModel> Lines { get; set; } = Array.Empty<LineModel>();
    public IList<ISpecialModel> Specials { get; set; } = Array.Empty<ISpecialModel>();
    public IList<SectorMoveSpecialModel> MoveSpecials { get; set; } = Array.Empty<SectorMoveSpecialModel>();
    public IList<ScrollSpecialModel> ScrollSpecials { get; set; } = Array.Empty<ScrollSpecialModel>();
    public IList<LightChangeSpecialModel> LightChangeSpecials { get; set; } = Array.Empty<LightChangeSpecialModel>();
    public IList<LightFireFlickerDoomModel> LightFireFlickerDoomSpecials { get; set; } = Array.Empty<LightFireFlickerDoomModel>();
    public IList<LightFlickerDoomSpecialModel> LightFlickerDoomSpecials { get; set; } = Array.Empty<LightFlickerDoomSpecialModel>();
    public IList<LightPulsateSpecialModel> LightPulsateSpecials { get; set; } = Array.Empty<LightPulsateSpecialModel>();
    public IList<LightStrobeSpecialModel> LightStrobeSpecials { get; set; } = Array.Empty<LightStrobeSpecialModel>();
    public IList<PushSpecialModel> PushSpecials { get; set; } = Array.Empty<PushSpecialModel>();
    public IList<StairSpecialModel> StairSpecials { get; set; } = Array.Empty<StairSpecialModel>();
    public IList<ElevatorSpecialModel> ElevatorSpecials { get; set; } = Array.Empty<ElevatorSpecialModel>();
    public IList<SwitchChangeSpecialModel> SwitchSpecials { get; set; } = Array.Empty<SwitchChangeSpecialModel>();
    public IList<SectorDamageSpecialModel> DamageSpecials { get; set; } = Array.Empty<SectorDamageSpecialModel>();
    public IList<string> VisitedMaps { get; set; } = Array.Empty<string>();
    public int TotalTime { get; set; }

    public int TotalMonsters { get; set; }
    public int TotalItems { get; set; }
    public int TotalSecrets { get; set; }

    public int KillCount { get; set; }
    public int ItemCount { get; set; }
    public int SecretCount { get; set; }

    public string? MusicName { get; set; }
}
