using System;
using Helion.Util.Extensions;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueBoolean : ConfigValue<bool>
    {
        public ConfigValueBoolean(bool value = default) : base(value)
        {
        }

        public static implicit operator bool(ConfigValueBoolean configValue) => configValue.Value;

        public override bool Set(object obj)
        {
            bool oldValue = Value;

            if (obj.GetType().IsEnum)
            {
                Value = (int)obj != 0;
                EmitEventIfChanged(oldValue);
                return true;
            }

            switch (obj)
            {
            case bool b:
                Value = b;
                EmitEventIfChanged(oldValue);
                return true;

            case int i:
                Value = i != 0;
                EmitEventIfChanged(oldValue);
                return true;

            case double d:
                Value = double.IsFinite(d) && d != 0;
                EmitEventIfChanged(oldValue);
                return true;

            case string s:
                Value = !s.Empty() && s != "0" && !s.Equals("false", StringComparison.OrdinalIgnoreCase);
                EmitEventIfChanged(oldValue);
                return true;

            default:
                return false;
            }
        }

    }
}
