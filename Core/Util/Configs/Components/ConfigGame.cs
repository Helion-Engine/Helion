using Helion.Maps.Shared;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigGame
{
    [ConfigInfo("If the player should always run.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Always Run")]
    public readonly ConfigValue<bool> AlwaysRun = new(true);

    [ConfigInfo("If autoaiming should be used.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Autoaim")]
    public readonly ConfigValue<bool> AutoAim = new(true);

    [ConfigInfo("If horizontal autoaiming should be used for projectiles.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Horizontal Autoaim")]
    public readonly ConfigValue<bool> HorizontalAutoAim = new(false);

    [ConfigInfo("Scales red amount draw to screen when the player takes damage.")]
    [OptionMenu(OptionSectionType.General, "Pain Intensity")]
    public readonly ConfigValue<double> PainIntensity = new(1.0);

    [ConfigInfo("If the player should automatically use lines when bumping.", demo: true)]
    [OptionMenu(OptionSectionType.General, "Bump Use", spacer: true)]
    public readonly ConfigValue<bool> BumpUse = new(false);

    [ConfigInfo("The last loaded world state on death.")]
    [OptionMenu(OptionSectionType.General, "Load latest on death")]
    public readonly ConfigValue<bool> LoadLatestOnDeath = new(true);

    [ConfigInfo("Automatically saves at level start.", save: false)]
    [OptionMenu(OptionSectionType.General, "Autosave")]
    public readonly ConfigValue<bool> AutoSave = new(false);

    [ConfigInfo("Confirm overwriting when quick saving.")]
    [OptionMenu(OptionSectionType.General, "Confirm quick save")]
    public readonly ConfigValue<bool> QuickSaveConfirm = new(true);

    [ConfigInfo("Enables fast monsters.", save: false, demo: true, serialize: true)]
    [OptionMenu(OptionSectionType.General, "Fast monsters", spacer: true)]
    public readonly ConfigValue<bool> FastMonsters = new(false);

    [ConfigInfo("Enables monster closet detection and limited monster AI.", mapRestartRequired: true, demo: true)]
    [OptionMenu(OptionSectionType.General, "Monster closet detection")]
    public readonly ConfigValue<bool> MonsterCloset = new(true);

    [ConfigInfo("The skill level to use when starting a map.", save: false, demo: true)]
    public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Medium, ConfigSetFlags.OnNewWorld, OnlyValidEnums<SkillLevel>());

    [ConfigInfo("If stats should be written to levelstat.txt.", save: false)]
    public readonly ConfigValue<bool> LevelStat = new(false);

    [ConfigInfo("Whether no monsters should be spawned.", save: false, serialize: true)]
    public readonly ConfigValue<bool> NoMonsters = new(false);

    [ConfigInfo("Resets the players inventory at the start of each map.", save: false, serialize: true)]
    public readonly ConfigValue<bool> PistolStart = new(false);

    [ConfigInfo("Marks lines and secctors that are activated by a special in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark specials", spacer: true)]
    public readonly ConfigValue<bool> MarkSpecials = new(false);

    [ConfigInfo("Marks secret sectors in the automap.")]
    [OptionMenu(OptionSectionType.General, "Mark secrets")]
    public readonly ConfigValue<bool> MarkSecrets = new(false);

    public SkillDef? SelectedSkillDefinition { get; set; }
}
