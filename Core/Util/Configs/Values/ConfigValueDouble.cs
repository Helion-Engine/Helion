namespace Helion.Util.Configs.Values
{
    public class ConfigValueDouble : ConfigValue<double>
    {
        public ConfigValueDouble(double value = default) : base(value)
        {
        }

        public static implicit operator double(ConfigValueDouble configValue) => configValue.Value;

        public override bool Set(object obj)
        {
            double oldValue = Value;

            if (obj.GetType().IsEnum)
            {
                Value = (double)obj;
                EmitEventIfChanged(oldValue);
                return true;
            }

            switch (obj)
            {
            case bool b:
                Value = b ? 1.0 : 0.0;
                EmitEventIfChanged(oldValue);
                return true;

            case int i:
                Value = i;
                EmitEventIfChanged(oldValue);
                return true;

            case double d:
                if (!double.IsFinite(d))
                    return false;
                Value = d;
                EmitEventIfChanged(oldValue);
                return true;

            case string s:
                if (!double.TryParse(s, out double newValue))
                    return false;
                Value = newValue;
                EmitEventIfChanged(oldValue);
                return true;

            default:
                return false;
            }
        }
    }
}
