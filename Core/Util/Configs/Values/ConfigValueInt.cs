namespace Helion.Util.Configs.Values
{
    public class ConfigValueInt : ConfigValue<int>
    {
        private readonly int m_min, m_max;

        public ConfigValueInt(int value = default, int min = int.MinValue, int max = int.MaxValue) : base(value)
        {
            m_min = min;
            m_max = max;
        }

        public static implicit operator int(ConfigValueInt configValue) => configValue.Value;

        public override bool Set(object obj)
        {
            int oldValue = Value;

            if (obj.GetType().IsEnum)
            {
                Value = MathHelper.Clamp((int)obj, m_min, m_max);
                EmitEventIfChanged(oldValue);
                return true;
            }

            switch (obj)
            {
                case bool b:
                    Value = b ? 1 : 0;
                    break;

                case int i:
                    Value = i;
                    break;

                case double d:
                    if (!double.IsFinite(d))
                        return false;
                    Value = (int)d;
                    break;

                case string s:
                    if (!int.TryParse(s, out int newValue))
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
