using Helion.Util.Extensions;

namespace Helion.Util.Configs.Values
{
    public class ConfigValueString : ConfigValue<string>
    {
        public ConfigValueString(string value = "") : base(value)
        {
        }

        public static implicit operator string(ConfigValueString configValue) => configValue.Value;

        public override bool Set(object obj)
        {
            string oldValue = Value;
            Value = obj.ToString() ?? "";

            EmitEventIfChanged(oldValue);

            return !Value.Empty();
        }
    }
}
