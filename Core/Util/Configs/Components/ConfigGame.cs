using Helion.Maps.Shared;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with gameplay.")]
    public class ConfigGame
    {
        [ConfigInfo("Whether vertical autoaiming should be used.")]
        public readonly ConfigValueBoolean AutoAim = new(true);

        [ConfigInfo("The skill level in the game.")]
        public readonly ConfigValueEnum<SkillLevel> Skill = new(SkillLevel.Hard);
    }
}
