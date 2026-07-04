namespace Helion.Util.Configs.Components;

public enum CompatSetting
{
    False,
    True,
    Always,
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
