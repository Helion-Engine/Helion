using Helion.Util.ConfigsNew.Values;

namespace Helion.Util.ConfigsNew.Components
{
    public class ConfigDeveloperRender
    {
        [ConfigInfo("If rendering should have debugging information drawn.", save: false)]
        public readonly ConfigValue<bool> Debug = new(false);
    }
    
    public class ConfigDeveloper
    {
        public readonly ConfigDeveloperRender Render = new();
    }
}
