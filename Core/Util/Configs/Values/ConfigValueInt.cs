using System;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueInt : IConfigValue<int>
    {
        public int Value { get; private set; }

        public event EventHandler<int>? OnChanged;

        public ConfigValueInt(int value = default)
        {
            Value = value;
        }

        public static implicit operator int(ConfigValueInt configValue) => configValue.Value;

        public object Get() => Value;

        public bool Set(object obj)
        {
            int oldValue = Value;

            if (obj.GetType().IsEnum)
            {
                Value = (int)obj;
                EmitEventIfChanged(oldValue);
                return true;
            }

            switch (obj)
            {
            case bool b:
                Value = b ? 1 : 0;
                EmitEventIfChanged(oldValue);
                return true;

            case int i:
                Value = i;
                EmitEventIfChanged(oldValue);
                return true;

            case double d:
                if (!double.IsFinite(d))
                    return false;
                Value = (int)d;
                EmitEventIfChanged(oldValue);
                return true;

            case string s:
                if (!int.TryParse(s, out int newValue))
                    return false;
                Value = newValue;
                EmitEventIfChanged(oldValue);
                return true;

            default:
                return false;
            }
        }

        private void EmitEventIfChanged(int oldValue)
        {
            if (oldValue != Value)
                OnChanged?.Invoke(this, Value);
        }

        public override string ToString() => Value.ToString();
    }
}
