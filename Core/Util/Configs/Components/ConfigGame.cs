using Helion.Maps.Shared;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigGame
{
    // Controls/input

    [ConfigInfo("Always run.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Always Run")]
    public readonly ConfigValue<bool> AlwaysRun = new(true);

    [ConfigInfo("Enable vertical autoaiming.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Autoaim")]
    public readonly ConfigValue<bool> AutoAim = new(true);

    [ConfigInfo("Enable horizontal autoaiming for projectiles (rockets, plasma).  Only applies if vertical autoaiming is enabled.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Horizontal Autoaim")]
    public readonly ConfigValue<bool> HorizontalAutoAim = new(false);

    [ConfigInfo("Player can use lines (doors, switches) by bumping into them.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Bump Use")]
    public readonly ConfigValue<bool> BumpUse = new(false);

    [ConfigInfo("Enable game controller support.")]
    [OptionMenu(OptionSectionType.General, "Enable Game Controller")]
    public readonly ConfigValue<bool> EnableGameController = new(true);

    [ConfigInfo("Dead zone for game controller analog inputs.")]
    //[OptionMenu(OptionSectionType.General, "Game Controller Dead Zone")]
    public readonly ConfigValue<double> GameControllerDeadZone = new(0.0, Clamp(0.0, 1.0));

    // Visual effects

    [ConfigInfo("Scale red amount drawn to screen when the player takes damage.")]
    [OptionMenu(OptionSectionType.General, "Pain Intensity", spacer: true)]
    public readonly ConfigValue<double> PainIntensity = new(1.0);

    [ConfigInfo("Transition effect between levels/screens.")]
    [OptionMenu(OptionSectionType.General, "Transition Type")]
    public readonly ConfigValue<World.TransitionType> TransitionType = new(World.TransitionType.Melt);


    // Saving and loading

    [ConfigInfo("Attempt to load latest saved game on death.")]
    [OptionMenu(OptionSectionType.General, "Load Latest on Death", spacer: true)]
    public readonly ConfigValue<bool> LoadLatestOnDeath = new(true);

    [ConfigInfo("Automatically save at level start.")]
    [OptionMenu(OptionSectionType.General, "Autosave")]
    public readonly ConfigValue<bool> AutoSave = new(true);

    [ConfigInfo("Number of autosaves before the oldest is replaced. 0 = No limit.")]
    [OptionMenu(OptionSectionType.General, "Rotating Autosaves")]
    public readonly ConfigValue<int> RotatingAutoSaves = new(4, GreaterOrEqual(0));

    [ConfigInfo("Number of quicksaves before the oldest is replaced. 0 = Use regular save slots instead.")]
    [OptionMenu(OptionSectionType.General, "Rotating Quicksaves")]
    public readonly ConfigValue<int> RotatingQuickSaves = new(4, GreaterOrEqual(0));

    [ConfigInfo("Confirm overwrite when quicksaving to regular save slots.")]
    [OptionMenu(OptionSectionType.General, "Confirm Quicksave to Slot")]
    public readonly ConfigValue<bool> QuickSaveConfirm = new(true);


    // Cheats

    [ConfigInfo("Mark lines and sectors that are activated by a special in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark Specials", spacer: true)]
    public readonly ConfigValue<bool> MarkSpecials = new(false);

    [ConfigInfo("Mark secret sectors in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark Secrets")]
    public readonly ConfigValue<bool> MarkSecrets = new(false);

    
    // Difficulty modifiers

    [ConfigInfo("Remove all monsters from the game.", save: false, serialize: true)]
    [OptionMenu(OptionSectionType.General, "No Monsters", spacer: true)]
    public readonly ConfigValue<bool> NoMonsters = new(false);

    [ConfigInfo("Reset the player's inventory at the start of each map.", save: false, serialize: true)]
    [OptionMenu(OptionSectionType.General, "Pistol Starts")]
    public readonly ConfigValue<bool> PistolStart = new(false);

    [ConfigInfo("Enable fast monsters.", save: false, demo: true, serialize: true)]
    [OptionMenu(OptionSectionType.General, "Fast Monsters")]
    public readonly ConfigValue<bool> FastMonsters = new(false);


    // Non-menu items
    [ConfigInfo("Write stats to levelstat.txt.", save: false)]
    public readonly ConfigValue<bool> LevelStat = new(false);

    [ConfigInfo("Skill level to use when starting a map.", save: false, demo: true)]
    public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Medium, ConfigSetFlags.OnNewWorld, OnlyValidEnums<SkillLevel>());

    [ConfigInfo("Enable monster closet detection and limited monster AI (Map restart required).", mapRestartRequired: true, demo: true)]
    public readonly ConfigValue<bool> MonsterCloset = new(true);

    public SkillDef? SelectedSkillDefinition { get; set; }
}
