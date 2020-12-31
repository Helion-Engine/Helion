using System;
using Helion.Util.Extensions;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueString : IConfigValue<string>
    {
        public string Value { get; private set; }

        public event EventHandler<string>? OnChanged;

        public ConfigValueString(string value = "")
        {
            Value = value;
        }

        public static implicit operator string(ConfigValueString configValue) => configValue.Value;

        public object Get() => Value;

        public bool Set(object obj)
        {
            string oldValue = Value;
            Value = obj.ToString() ?? "";

            if (oldValue != Value)
                OnChanged?.Invoke(this, Value);

            return !Value.Empty();
        }

        public override string ToString() => Value;
    }
}
