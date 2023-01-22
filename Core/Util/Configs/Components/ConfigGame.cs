using Helion.Maps.Shared;
using Helion.Util.Configs.Values;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigGame
{
    [ConfigInfo("If the player should always run.", demo: true)]
    public readonly ConfigValue<bool> AlwaysRun = new(true);

    [ConfigInfo("If the player should automatically use lines when bumping.", demo: true)]
    public readonly ConfigValue<bool> BumpUse = new(false);

    [ConfigInfo("Whether vertical autoaiming should be used.", demo: true)]
    public readonly ConfigValue<bool> AutoAim = new(true);

    [ConfigInfo("The last loaded world state on death.")]
    public readonly ConfigValue<bool> LoadLatestOnDeath = new(true);

    [ConfigInfo("Enables fast monsters.", save: false, demo: true)]
    public readonly ConfigValue<bool> FastMonsters = new(false);

    [ConfigInfo("If stats should be written to levelstat.txt.", save: false)]
    public readonly ConfigValue<bool> LevelStat = new(false);

    [ConfigInfo("Whether no monsters should be spawned.", save: false)]
    public readonly ConfigValue<bool> NoMonsters = new(false);

    [ConfigInfo("Automatically saves at level start.", save: false)]
    public readonly ConfigValue<bool> AutoSave = new(false);

    [ConfigInfo("The skill level to use when starting a map.", save: false, demo: true)]
    public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Medium, ConfigSetFlags.OnNewWorld, OnlyValidEnums<SkillLevel>());

    [ConfigInfo("Enables monster closet detection and limited monster AI.", restartRequired: true, demo: true)]
    public readonly ConfigValue<bool> MonsterCloset = new(true);

    [ConfigInfo("Confirm overwriting when quick saving.")]
    public readonly ConfigValue<bool> QuickSaveConfirm = new(true);
}
