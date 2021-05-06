using Helion.Util.Parser;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueDouble : ConfigValue<double>
    {
        private double m_min, m_max;

        public ConfigValueDouble(double value = default, double min = double.MinValue, double max = double.MaxValue) : base(value)
        {
            m_min = min;
            m_max = max;
        }

        public static implicit operator double(ConfigValueDouble configValue) => configValue.Value;

        public override bool Set(object obj)
        {
            double oldValue = Value;

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
                Value = MathHelper.Clamp((double)obj, m_min, m_max);
                EmitEventIfChanged(oldValue);
                return true;

            case string s:
                if (!SimpleParser.TryParseDouble(s, out double newValue))
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
