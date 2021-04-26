using Helion.Util.Configs.Values;
using Helion.World.Entities.Players;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Player configuration")]
    public class ConfigPlayer
    {
        [ConfigInfo("The name of the player.")]
        public readonly ConfigValueString Name = new("Player");

        [ConfigInfo("The gender of the player.")]
        public readonly ConfigValueEnum<PlayerGender> Gender = new(PlayerGender.Other);
    }
}
