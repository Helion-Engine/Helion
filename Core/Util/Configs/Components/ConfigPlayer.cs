using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.Entities.Players;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigPlayer
{
    [ConfigInfo("", save: false)]
    [OptionMenu(OptionSectionType.General, "", disabled: true, spacer: true)]
    public readonly ConfigValueHeader PlayerHeader = new("Player");

    [ConfigInfo("Name of the player.")]
    [OptionMenu(OptionSectionType.General, "Player Name")]
    public readonly ConfigValue<string> Name = new("Player", IfEmptyDefaultTo("Player"));

    [ConfigInfo("Gender of the player.")]
    [OptionMenu(OptionSectionType.General, "Player Gender")]
    public readonly ConfigValue<PlayerGender> Gender = new(default, OnlyValidEnums<PlayerGender>());
}
