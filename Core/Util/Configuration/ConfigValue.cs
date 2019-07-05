using System;
using static Helion.Util.Assert;

namespace Helion.Util.Configuration
{
    public class ConfigValueEvent<T> where T : IConvertible
    {
        public readonly T NewValue;

        public ConfigValueEvent(T newValue) => NewValue = newValue;
    }

    [ConfigValueComponent]
    public class ConfigValue<T> where T : IConvertible
    {
        public event EventHandler<ConfigValueEvent<T>> OnChanged;
        private T value;

        public ConfigValue(T defaultValue) => value = defaultValue;
    
        public static implicit operator T(ConfigValue<T> configValue) => configValue.value;

        private bool CheckForChange(T newValue)
        {
            if (newValue is Enum newEnumValue)
            {
                if (value is Enum enumValue)
                    return Equals(enumValue, newEnumValue);
                Fail($"Unexpected argument type for enum type {typeof(T)}");
                return true;
            }
            
            switch (newValue)
            {
            case bool newBoolValue:
                if (value is bool boolValue)
                    return boolValue == newBoolValue;
                Fail($"Unexpected argument type, expected a boolean but got {typeof(T)}");
                return true;
            
            case double newDoubleValue:
                if (value is double doubleValue)
                    return MathHelper.AreEqual(doubleValue, newDoubleValue);
                Fail($"Unexpected argument type, expected a double but got {typeof(T)}");
                return true;
            
            case int newIntValue:
                if (value is int intValue)
                    return intValue == newIntValue;
                Fail($"Unexpected argument type, expected an int but got {typeof(T)}");
                return true;
            
            case string newStringValue:
                if (value is string stringValue)
                    return stringValue == newStringValue;
                Fail($"Unexpected argument type, expected a string but got {typeof(T)}");
                return true;
            
            default:
                Fail($"Unexpected config value type: {typeof(T)}");
                return true;
            }
        }

        public T Get() => value;
        
        public void Set(T newValue)
        {
            bool changed = CheckForChange(newValue);
            
            value = newValue;
            
            if (changed)
                OnChanged?.Invoke(this, new ConfigValueEvent<T>(newValue));
        }

        public override string ToString() => $"{value}";
    }
}
