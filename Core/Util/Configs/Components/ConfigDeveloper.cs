using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigDeveloperRender
{
    [ConfigInfo("If rendering should have debugging information drawn.", save: false)]
    public readonly ConfigValue<bool> Debug = new(false);
}

public class ConfigDeveloper
{
    public readonly ConfigDeveloperRender Render = new();
}

