using Helion.Maps.Shared;
using Helion.Resources.Definitions.MapInfo;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Util.RandomGenerators;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigGame : ConfigElement<ConfigGame>
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

    // Visual effects

    [ConfigInfo("Scale red amount drawn to screen when the player takes damage.")]
    [OptionMenu(OptionSectionType.General, "Pain Intensity", spacer: true, sliderMin: 0, sliderMax: 5.0, sliderStep: .1)]
    public readonly ConfigValue<double> PainIntensity = new(1.0);

    [ConfigInfo("Scale red amount drawn to screen when the player pickups up a berserk.")]
    [OptionMenu(OptionSectionType.General, "Berserk Intensity", sliderMin: 0, sliderMax: 5.0, sliderStep: .1)]
    public readonly ConfigValue<double> BerserkIntensity = new(1.0);

    [ConfigInfo("Transition effect between levels/screens.")]
    [OptionMenu(OptionSectionType.General, "Transition Type")]
    public readonly ConfigValue<World.TransitionType> TransitionType = new(World.TransitionType.Melt);

    [ConfigInfo("Display ENDOOM text screen on quit.")]
    [OptionMenu(OptionSectionType.General, "Display ENDOOM")]
    public readonly ConfigValue<bool> DisplayEndoom = new(true);

    // Saving and loading

    [ConfigInfo("Attempt to load latest saved game on death.")]
    [OptionMenu(OptionSectionType.General, "Load Latest on Death", spacer: true)]
    public readonly ConfigValue<bool> LoadLatestOnDeath = new(true);

    [ConfigInfo("Automatically save at level start.")]
    [OptionMenu(OptionSectionType.General, "Autosave")]
    public readonly ConfigValue<bool> AutoSave = new(true);

    [ConfigInfo("Number of autosaves before the oldest is replaced. 0 = No limit.")]
    [OptionMenu(OptionSectionType.General, "Rotating Autosaves", sliderMin: 0, sliderMax: 50, sliderStep: 1)]
    public readonly ConfigValue<int> RotatingAutoSaves = new(4, GreaterOrEqual(0));

    [ConfigInfo("Number of quicksaves before the oldest is replaced. 0 = Use regular save slots instead.")]
    [OptionMenu(OptionSectionType.General, "Rotating Quicksaves", sliderMin: 0, sliderMax: 50, sliderStep: 1)]
    public readonly ConfigValue<int> RotatingQuickSaves = new(4, GreaterOrEqual(0));

    [ConfigInfo("Confirm overwrite when quicksaving to regular save slots.")]
    [OptionMenu(OptionSectionType.General, "Confirm Quicksave to Slot")]
    public readonly ConfigValue<bool> QuickSaveConfirm = new(true);

    [ConfigInfo("Automatically create a quicksave every x seconds. 0 = never.")]
    [OptionMenu(OptionSectionType.General, "Quicksave Seconds")]
    public readonly ConfigValue<int> QuickSaveSeconds = new(0, GreaterOrEqual(0));

    [ConfigInfo("Display screenshot and extended information in Save/Load menu.")]
    [OptionMenu(OptionSectionType.General, "Display Savegame Details")]
    public readonly ConfigValue<bool> ExtendedSaveGameInfo = new(true);

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

    [ConfigInfo("Starts a cooperative game in single player.", save: false, serialize: true)]
    [OptionMenu(OptionSectionType.General, "Solo-Net")]
    public readonly ConfigValue<bool> SoloNet = new(false);

    [ConfigInfo("Enable fast monsters.", save: false, demo: true, serialize: true)]
    [OptionMenu(OptionSectionType.General, "Fast Monsters")]
    public readonly ConfigValue<bool> FastMonsters = new(false);

    [ConfigInfo("Modifies damage to players.")]
    [OptionMenu(OptionSectionType.General, "Damage Receive Multiplier", sliderMin: 0, sliderMax: 10.0, sliderStep: .1)]
    public readonly ConfigValue<double> DamageReceiveMultiplier = new(1.0, GreaterOrEqual(0.0));

    [ConfigInfo("Modifies damage to non-players from players.")]
    [OptionMenu(OptionSectionType.General, "Damage Multiplier", sliderMin: 0, sliderMax: 10.0, sliderStep: .1)]
    public readonly ConfigValue<double> DamageApplyMultiplier = new(1.0, GreaterOrEqual(0.0));

    [ConfigInfo("Random number generator method.", save: false, demo: true, serialize: true, mapRestartRequired: true)]
    [OptionMenu(OptionSectionType.General, "RNG Method", spacer: true)]
    public readonly ConfigValue<RngMethod> Rng = new(RngMethod.Boom);

    [ConfigInfo("Shows the currently played WAD/map in Discord.")]
    [OptionMenu(OptionSectionType.General, "Discord Integration", spacer: true)]
    public readonly ConfigValue<bool> DiscordIntegration = new(true);

    [ConfigInfo("Randomly mirror corpse, blood, and bullet puffs.")]
    [OptionMenu(OptionSectionType.General, "Randomly Mirror Corpses", spacer: true)]
    public readonly ConfigValue<bool> MirrorCorpse = new(true);

    // Non-menu items
    [ConfigInfo("Write stats to levelstat.txt.", save: false)]
    public readonly ConfigValue<bool> LevelStat = new(false);

    [ConfigInfo("Skill level to use when starting a map.", save: false, demo: true, mapRestartRequired: true)]
    public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Medium, ConfigSetFlags.OnNewWorld, OnlyValidEnums<SkillLevel>());

    [ConfigInfo("Enable monster closet detection and limited monster AI (Map restart required).", mapRestartRequired: true, demo: true)]
    public readonly ConfigValue<bool> MonsterCloset = new(true);

    public SkillDef? SelectedSkillDefinition { get; set; }
}
