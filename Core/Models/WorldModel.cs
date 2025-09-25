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

    public DynamicArray<PlayerModel> Players = DynamicArray<PlayerModel>.Empty();
    public DynamicArray<EntityModel> Entities = DynamicArray<EntityModel>.Empty();
    public DynamicArray<SectorModel> Sectors = DynamicArray<SectorModel>.Empty();
    public DynamicArray<LineModel> Lines = DynamicArray<LineModel>.Empty();
    public DynamicArray<ISpecialModel> Specials = DynamicArray<ISpecialModel>.Empty();
    public DynamicArray<SectorMoveSpecialModel> MoveSpecials = DynamicArray<SectorMoveSpecialModel>.Empty();
    public DynamicArray<ScrollSpecialModel> ScrollSpecials = DynamicArray<ScrollSpecialModel>.Empty();
    public DynamicArray<LightChangeSpecialModel> LightChangeSpecials = DynamicArray<LightChangeSpecialModel>.Empty();
    public DynamicArray<LightFireFlickerDoomModel> LightFireFlickerDoomSpecials = DynamicArray<LightFireFlickerDoomModel>.Empty();
    public DynamicArray<LightFlickerDoomSpecialModel> LightFlickerDoomSpecials = DynamicArray<LightFlickerDoomSpecialModel>.Empty();
    public DynamicArray<LightPulsateSpecialModel> LightPulsateSpecials = DynamicArray<LightPulsateSpecialModel>.Empty();
    public DynamicArray<LightStrobeSpecialModel> LightStrobeSpecials = DynamicArray<LightStrobeSpecialModel>.Empty();
    public DynamicArray<PushSpecialModel> PushSpecials = DynamicArray<PushSpecialModel>.Empty();
    public DynamicArray<StairSpecialModel> StairSpecials = DynamicArray<StairSpecialModel>.Empty();
    public DynamicArray<ElevatorSpecialModel> ElevatorSpecials = DynamicArray<ElevatorSpecialModel>.Empty();
    public DynamicArray<SwitchChangeSpecialModel> SwitchSpecials = DynamicArray<SwitchChangeSpecialModel>.Empty();
    public DynamicArray<SectorDamageSpecialModel> DamageSpecials = DynamicArray<SectorDamageSpecialModel>.Empty();
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
