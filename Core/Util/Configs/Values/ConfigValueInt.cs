namespace Helion.Util.Configs.Values
{
    public class ConfigValueInt : ConfigValue<int>
    {
        public ConfigValueInt(int value = default) : base(value)
        {
        }

        public static implicit operator int(ConfigValueInt configValue) => configValue.Value;

        public override bool Set(object obj)
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
    }
}
