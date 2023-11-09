using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.World.Entities.Players;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public class ConfigPlayer
{
    [ConfigInfo("The name of the player.")]
    [OptionMenu(OptionSectionType.General, "Player Name", spacer: true)]
    public readonly ConfigValue<string> Name = new("Player", IfEmptyDefaultTo("Player"));

    [ConfigInfo("The gender of the player.")]
    [OptionMenu(OptionSectionType.General, "Player Gender")]
    public readonly ConfigValue<PlayerGender> Gender = new(default, OnlyValidEnums<PlayerGender>());
}
