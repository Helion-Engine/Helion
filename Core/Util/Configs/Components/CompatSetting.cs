using System.ComponentModel;

namespace Helion.Util.Configs.Components;

public enum CompatSetting
{
    [Description("Off")]
    False,
    [Description("On")]
    True,
    [Description("Always On")]
    Always,
    [Description("Always Off")]
    Never
}

public static class CompatSettingExtensions
{
    public static bool ToBool(this CompatSetting setting)
    {
        return setting switch
        {
            CompatSetting.True or CompatSetting.Always => true,
            _ => false,
        };
    }
}
