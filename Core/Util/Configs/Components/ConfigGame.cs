using Helion.Maps.Shared;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigGame
{
    [ConfigInfo("", save: false)]
    [OptionMenu(OptionSectionType.General, "", disabled: true)]
    public readonly ConfigValueHeader ControlsHeader = new("Controls");

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



    [ConfigInfo("", save: false)]
    [OptionMenu(OptionSectionType.General, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader VisualHeader = new("Visual");

    [ConfigInfo("Scale red amount drawn to screen when the player takes damage.")]
    [OptionMenu(OptionSectionType.General, "Pain Intensity")]
    public readonly ConfigValue<double> PainIntensity = new(1.0);

    [ConfigInfo("Transition effect between levels/screens.")]
    [OptionMenu(OptionSectionType.General, "Transition Type")]
    public readonly ConfigValue<World.TransitionType> TransitionType = new(World.TransitionType.Melt);


    [ConfigInfo("", save: false)]
    [OptionMenu(OptionSectionType.General, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader SaveLoadHeader = new("Save/Load");

    [ConfigInfo("Attempt to load latest saved game on death.")]
    [OptionMenu(OptionSectionType.General, "Load Latest on Death")]
    public readonly ConfigValue<bool> LoadLatestOnDeath = new(true);

    [ConfigInfo("Automatically save at level start.", save: true)]
    [OptionMenu(OptionSectionType.General, "Autosave")]
    public readonly ConfigValue<bool> AutoSave = new(false);

    [ConfigInfo("Confirm overwrite when quick saving.")]
    [OptionMenu(OptionSectionType.General, "Confirm Quick Save")]
    public readonly ConfigValue<bool> QuickSaveConfirm = new(true);


    [ConfigInfo("", save: false)]
    [OptionMenu(OptionSectionType.General, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader CheatHeader = new("Cheats");

    [ConfigInfo("Mark lines and sectors that are activated by a special in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark Specials", spacer: false)]
    public readonly ConfigValue<bool> MarkSpecials = new(false);

    [ConfigInfo("Mark secret sectors in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark Secrets")]
    public readonly ConfigValue<bool> MarkSecrets = new(false);

    
    [ConfigInfo("", save: false)]
    [OptionMenu(OptionSectionType.General, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader DifficultyHeader = new("Difficulty");

    [ConfigInfo("Remove all monsters from the game.", save: false, serialize: true)]
    [OptionMenu(OptionSectionType.General, "No Monsters")]
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
