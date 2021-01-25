using Helion.Maps.Shared;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with gameplay.")]
    public class ConfigGame
    {
        [ConfigInfo("Whether vertical autoaiming should be used.")]
        public readonly ConfigValueBoolean AutoAim = new(true);

        [ConfigInfo("The skill level to use when starting a map.", save: false)]
        public readonly ConfigValueEnum<SkillLevel> Skill = new(SkillLevel.None);

        [ConfigInfo("Whether no monsters should be spawned.", save: false)]
        public readonly ConfigValueBoolean NoMonsters = new();

        [ConfigInfo("Enables fast monsters.", save: false)]
        public readonly ConfigValueBoolean SV_FastMonsters = new();
    }
}
