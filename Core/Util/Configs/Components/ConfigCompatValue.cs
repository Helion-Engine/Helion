using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Components;

public class ConfigCompatValue(CompatSetting value) : ConfigValue<CompatSetting>(value)
{
    public override object ObjectValueSerialize => Value.ToBool();

    public override ConfigSetResult Set(object newValue, bool writeToConfig = true, bool fireChangeEvents = true)
    {
        if (writeToConfig)
            return base.Set(newValue, writeToConfig, fireChangeEvents);

        if (!TryConvertInternal(newValue, out var converted))
            return ConfigSetResult.NotSetByBadConversion;

        if (this.IsMutable())
            return base.Set(converted, writeToConfig, fireChangeEvents);

        return ConfigSetResult.Unchanged;
    }
}

public static class ConfigValueCompatSettingExtensions
{
    public static void SetIfMutable(this ConfigCompatValue configValue, bool newValue)
    {
        if (configValue.IsMutable())
            configValue.Set(newValue, writeToConfig: false);
    }

    public static bool IsMutable(this ConfigCompatValue configValue)
    {
        return configValue.UserValue != CompatSetting.Always && configValue.UserValue != CompatSetting.Never;
    }
}
