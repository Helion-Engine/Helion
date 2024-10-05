namespace Helion.Util.Configs.Impl
{
    using Helion.Util.Configs.Options;
    using Helion.Util.Configs.Values;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;


    public interface IConfigElement
    {
        void PopulateComponentsRecursively(List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields, int depth);
    }

    public abstract class ConfigElement<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T> : IConfigElement
    {
        public void PopulateComponentsRecursively(List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields, int depth = 1)
        {
            const int RecursiveOverflowLimit = 100;
            if (depth > RecursiveOverflowLimit)
                throw new($"Overflow when trying to get options from the config: {this} ({GetType()})");

            foreach (FieldInfo fieldInfo in typeof(T).GetFields())
            {
                if (!fieldInfo.IsPublic)
                    continue;

                object? childObj = fieldInfo.GetValue(this);
                if (childObj == null || childObj == this)
                    continue;

                if (childObj is IConfigValue configValue)
                {
                    OptionMenuAttribute? attribute = fieldInfo.GetCustomAttribute<OptionMenuAttribute>();
                    ConfigInfoAttribute? configAttribute = fieldInfo.GetCustomAttribute<ConfigInfoAttribute>();
                    if (attribute != null && configAttribute != null)
                        fields.Add((configValue, attribute, configAttribute));
                    continue;
                }

                (childObj as IConfigElement)?.PopulateComponentsRecursively(fields, depth + 1);
            }
        }
    }
}
