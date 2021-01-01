using System;
using System.Reflection;
using Helion.Util.Extensions;

namespace Helion.Util.Configs.Tree
{
    /// <summary>
    /// A tree of all the different config values. Allows for a user to lookup
    /// config values.
    /// </summary>
    public class ConfigTree
    {
        /// <summary>
        /// The separator that divides nodes from each other when storing them
        /// in a text file.
        /// </summary>
        public const char Separator = '.';

        private readonly ConfigNode m_root = new("", null);

        public ConfigTree(Config config)
        {
            PopulateTreeRecursively(m_root, config, "");
        }

        private void PopulateTreeRecursively(ConfigNode node, object component, string path)
        {
            foreach (FieldInfo fieldInfo in component.GetType().GetFields())
            {
                bool hasAttribute = Config.HasConfigAttribute(fieldInfo);
                bool isValue = Config.IsConfigValue(fieldInfo);

                if (!hasAttribute && !isValue)
                    continue;

                string lowerName = fieldInfo.Name.ToLower();
                string newPath = path.Empty() ? lowerName : $"{path}.{lowerName}";
                object childComponent = fieldInfo.GetValue(component) ?? throw new Exception($"Failed to get tree field at path '{path}'");

                if (isValue)
                {
                    node.AddValue(lowerName, childComponent);
                    continue;
                }

                // Make it if it does not already exist.
                if (!node.TryGetNode(lowerName, out ConfigNode? parentNode))
                    parentNode = node.AddNode(lowerName);

                PopulateTreeRecursively(parentNode, childComponent, newPath);
            }
        }
    }
}
