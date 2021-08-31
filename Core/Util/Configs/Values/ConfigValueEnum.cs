using System;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueEnum<E> : ConfigValue<E> where E : struct, Enum
    {
        public ConfigValueEnum(E value = default) : base(value)
        {
        }

        public static implicit operator E(ConfigValueEnum<E> configValue) => configValue.Value;

        public override bool Set(object obj)
        {
            E oldValue = Value;

            if (obj.GetType().IsEnum)
            {
                Value = (E)obj;
                EmitEventIfChanged(oldValue);
                return true;
            }

            switch (obj)
            {
            case int i:
                Value = (E)(object)i;
                EmitEventIfChanged(oldValue);
                return true;

            case string s:
                E[] enumValues = Enum.GetValues<E>();
                string[] names = Enum.GetNames(typeof(E));
                for (int i = 0; i < names.Length; i++)
                {
                    if (!names[i].Equals(s, StringComparison.OrdinalIgnoreCase))
                        continue;
                    Value = enumValues[i];
                    EmitEventIfChanged(oldValue);
                    return true;
                }
                return false;

            default:
                return false;
            }
        }
    }
}