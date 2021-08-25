using Helion.Util.ConfigsNew.Values;
using Helion.World.Entities.Players;
using static Helion.Util.ConfigsNew.Values.ConfigFilters;

namespace Helion.Util.ConfigsNew.Components
{
    public class ConfigPlayer
    {
        [ConfigInfo("The name of the player.")]
        public readonly ConfigValue<string> Name = new("Player", IfEmptyDefaultTo("Player"));

        [ConfigInfo("The gender of the player.")]
        public readonly ConfigValue<PlayerGender> Gender = new(default, OnlyValidEnums<PlayerGender>());
    }
}
