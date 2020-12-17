using Helion.Maps.Components.Things;
using Helion.Util.Configuration.Attributes;

namespace Helion.Util.Configuration.Components
{
    [ConfigComponent]
    public class EngineGameConfig
    {
        public readonly ConfigValue<SkillLevel> Skill = new(SkillLevel.Hard);
    }
}