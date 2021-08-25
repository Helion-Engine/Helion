using Helion.Maps.Shared;
using Helion.Util.ConfigsNew.Values;
using static Helion.Util.ConfigsNew.Values.ConfigFilters;

namespace Helion.Util.ConfigsNew.Components
{
    public class ConfigGame
    {
        [ConfigInfo("If the player should always run.")]
        public readonly ConfigValue<bool> AlwaysRun = new(true);
        
        [ConfigInfo("Whether vertical autoaiming should be used.")]
        public readonly ConfigValue<bool> AutoAim = new(true);
        
        [ConfigInfo("Enables fast monsters.", save: false)]
        public readonly ConfigValue<bool> FastMonsters = new(false);

        [ConfigInfo("If stats should be written to levelstat.txt.", save: false)]
        public readonly ConfigValue<bool> LevelStat = new(false);
        
        [ConfigInfo("Whether no monsters should be spawned.", save: false)]
        public readonly ConfigValue<bool> NoMonsters = new(false);
        
        [ConfigInfo("The skill level to use when starting a map.", save: false)]
        public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Medium, OnlyValidEnums<SkillLevel>());
    }
}
