using System;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueDouble : IConfigValue<double>
    {
        public double Value { get; private set; }
        public bool Changed { get; private set; }
        public event EventHandler<double>? OnChanged;

        public ConfigValueDouble(double value = default)
        {
            Value = value;
        }

        public static implicit operator double(ConfigValueDouble configValue) => configValue.Value;

        public object Get() => Value;

        public bool Set(object obj)
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

        private void EmitEventIfChanged(double oldValue)
        {
            if (!oldValue.Equals(Value))
            {
                Changed = true;
                OnChanged?.Invoke(this, Value);
            }
        }

        public override string ToString() => Value.ToString();
    }
}
