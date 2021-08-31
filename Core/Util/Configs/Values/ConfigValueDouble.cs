using Helion.Util.Parser;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueDouble : ConfigValue<double>
    {
        private readonly double m_min, m_max;

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
                    break;

                case int i:
                    Value = i;
                    break;

                case double d:
                    if (!double.IsFinite(d))
                        return false;
                    Value = d;
                    break;

                case string s:
                    if (!SimpleParser.TryParseDouble(s, out double newValue))
                        return false;
                    Value = newValue;
                    break;

                default:
                    return false;
            }

            Value = MathHelper.Clamp(Value, m_min, m_max);
            EmitEventIfChanged(oldValue);
            return true;
        }
    }
}
