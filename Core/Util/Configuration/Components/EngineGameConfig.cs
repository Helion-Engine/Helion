using Helion.Maps.Shared;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineGameConfig 
    {
        public readonly ConfigValue<SkillLevel> Skill = new ConfigValue<SkillLevel>(SkillLevel.Hard);
    }
}