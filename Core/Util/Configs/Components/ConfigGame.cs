using Helion.Maps.Shared;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigGame
{
    [ConfigInfo("Player always runs.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Always Run")]
    public readonly ConfigValue<bool> AlwaysRun = new(true);

    [ConfigInfo("Enable autoaiming.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Autoaim")]
    public readonly ConfigValue<bool> AutoAim = new(true);

    [ConfigInfo("Horizontal autoaiming enabled for projectiles (rockets, plasma).", demo: true)]
    [OptionMenu(OptionSectionType.General, "Horizontal Autoaim")]
    public readonly ConfigValue<bool> HorizontalAutoAim = new(false);

    [ConfigInfo("Scale red amount drawn to screen when the player takes damage.")]
    [OptionMenu(OptionSectionType.General, "Pain Intensity")]
    public readonly ConfigValue<double> PainIntensity = new(1.0);

    [ConfigInfo("Player can use lines by bumping into them.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Bump Use", spacer: true)]
    public readonly ConfigValue<bool> BumpUse = new(false);

    [ConfigInfo("Attempt to load latest saved game on death.")]
    [OptionMenu(OptionSectionType.General, "Load Latest on Death")]
    public readonly ConfigValue<bool> LoadLatestOnDeath = new(true);

    [ConfigInfo("Automatically save at level start.", save: false)]
    [OptionMenu(OptionSectionType.General, "Autosave")]
    public readonly ConfigValue<bool> AutoSave = new(false);

    [ConfigInfo("Confirm overwrite when quick saving.")]
    [OptionMenu(OptionSectionType.General, "Confirm Quick Save")]
    public readonly ConfigValue<bool> QuickSaveConfirm = new(true);

    [ConfigInfo("Enable fast monsters.", save: false, demo: true, serialize: true)]
    [OptionMenu(OptionSectionType.General, "Fast Monsters", spacer: true)]
    public readonly ConfigValue<bool> FastMonsters = new(false);

    [ConfigInfo("Enable monster closet detection and limited monster AI.", mapRestartRequired: true, demo: true)]
    [OptionMenu(OptionSectionType.General, "Monster Closet Detection")]
    public readonly ConfigValue<bool> MonsterCloset = new(true);

    [ConfigInfo("Skill level to use when starting a map.", save: false, demo: true)]
    public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Medium, ConfigSetFlags.OnNewWorld, OnlyValidEnums<SkillLevel>());

    [ConfigInfo("Write stats to levelstat.txt.", save: false)]
    public readonly ConfigValue<bool> LevelStat = new(false);

    [ConfigInfo("Whether no monsters should be spawned.", save: false, serialize: true)]
    public readonly ConfigValue<bool> NoMonsters = new(false);

    [ConfigInfo("Reset the player's inventory at the start of each map.", save: false, serialize: true)]
    public readonly ConfigValue<bool> PistolStart = new(false);

    [ConfigInfo("Mark lines and sectors that are activated by a special in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark Specials", spacer: true)]
    public readonly ConfigValue<bool> MarkSpecials = new(false);

    [ConfigInfo("Mark secret sectors in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark Secrets")]
    public readonly ConfigValue<bool> MarkSecrets = new(false);

    public SkillDef? SelectedSkillDefinition { get; set; }
}
