namespace Helion.Util.Configs.Impl
{
    using Helion.Util.Configs.Options;
    using Helion.Util.Configs.Values;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using Helion.Util.Configs.Components;
    using System;


    public interface IConfigElement
    {
        void RecursivelyGetConfigFieldsOrThrow(List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields, int depth);
        void PopulateComponentsRecursively(Dictionary<string, ConfigComponent> components, string path, int depth);
    }

    public abstract class ConfigElement<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T> : IConfigElement
    {
        public void RecursivelyGetConfigFieldsOrThrow(List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields, int depth = 1)
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

                (childObj as IConfigElement)?.RecursivelyGetConfigFieldsOrThrow(fields, depth + 1);
            }
        }

        public void PopulateComponentsRecursively(Dictionary<string, ConfigComponent> components, string path, int depth)
        {
            const int RecursiveOverflowLimit = 100;

            if (depth > RecursiveOverflowLimit)
                throw new Exception($"A public instance is missing the [ConfigComponentIgnore] attribute, possibly at: {path}");

            foreach (FieldInfo fieldInfo in typeof(T).GetFields())
            {
                if (!fieldInfo.IsPublic)
                    continue;

                object? childObj = fieldInfo.GetValue(this);
                if (childObj == null)
                    throw new Exception($"Missing config object instantiation {fieldInfo.Name} at '{path}'");

                string newPath = (path != "" ? $"{path}." : "") + fieldInfo.Name.ToLower();

                if (childObj is IConfigValue configValue)
                {
                    ConfigInfoAttribute? attribute = fieldInfo.GetCustomAttribute<ConfigInfoAttribute>();
                    if (attribute == null)
                        throw new Exception($"Config field at '{newPath}' is missing attribute {nameof(ConfigInfoAttribute)}");

                    components[newPath] = new ConfigComponent(newPath, attribute, configValue);
                    continue;
                }

                (childObj as IConfigElement)?.PopulateComponentsRecursively(components, newPath, depth + 1);
            }
        }
    }
}
