using Helion.Maps.Shared;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components
{
    [ConfigInfo("Components that deal with gameplay.")]
    public class ConfigGame
    {
        [ConfigInfo("Whether vertical autoaiming should be used.")]
        public readonly ConfigValueBoolean AutoAim = new(true);

        public SkillLevel Skill { get; set; }
        public bool NoMonsters { get; set; }
        public bool SV_FastMonsters { get; set; }
    }
}
