using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Helion.Util.Configs.Tree
{
    /// <summary>
    /// A node in a config tree. A node is a collection of child nodes, or
    /// child values. Any 'values' are `ConfigValue` instances. Each value is
    /// a leaf in the tree. A node can be a leaf if there are no children for
    /// either type under it, but this would be a developer mistake.
    /// </summary>
    public class ConfigNode
    {
        /// <summary>
        /// The name of this node. It will always be lower case.
        /// </summary>
        public readonly string Name;

        private readonly ConfigNode? m_parent;
        private readonly Dictionary<string, ConfigNode> m_nodes = new();
        private readonly Dictionary<string, object> m_values = new();

        public ConfigNode(string name, ConfigNode? parent)
        {
            Name = name.ToLower();
            m_parent = parent;
        }

        /// <summary>
        /// Tries to get a parent node with the name provided.
        /// </summary>
        /// <param name="name">The parent name, case insensitive.</param>
        /// <param name="node">The node, if it exists (otherwise null).</param>
        /// <returns>True on finding, false if not found.</returns>
        public bool TryGetNode(string name, [NotNullWhen(true)] out ConfigNode? node)
        {
            return m_nodes.TryGetValue(name.ToLower(), out node);
        }

        /// <summary>
        /// Tries to get a leaf value node with the name provided.
        /// </summary>
        /// <param name="name">The parent name, case insensitive.</param>
        /// <param name="value">The config value object if it exists, otherwise
        /// is set to null.</param>
        /// <returns>True on finding, false if not found.</returns>
        public bool TryGetValue(string name, [NotNullWhen(true)] out object? value)
        {
            return m_values.TryGetValue(name.ToLower(), out value);
        }

        /// <summary>
        /// Adds a new value into this node.
        /// </summary>
        /// <param name="name">The name of the value. Case does not matter.
        /// </param>
        /// <param name="value">The value object. This should be a ConfigValue
        /// generic type.</param>
        /// <exception cref="Exception">If a value with this name already
        /// exists.</exception>
        internal void AddValue(string name, object value)
        {
            string lowerName = name.ToLower();
            if (m_values.ContainsKey(lowerName))
                throw new Exception($"Trying to add the same value twice: {name}");
            m_values[lowerName] = value;
        }

        /// <summary>
        /// Adds a new child node.
        /// </summary>
        /// <param name="name">The name of the node. Case does not matter.
        /// </param>
        /// <returns>The created child node.</returns>
        /// <exception cref="Exception">If a node with this name already
        /// exists.</exception>
        internal ConfigNode AddNode(string name)
        {
            string lowerName = name.ToLower();
            if (m_nodes.ContainsKey(lowerName))
                throw new Exception($"Trying to add the same node twice: {name}");

            ConfigNode node = new(lowerName, this);
            m_nodes[lowerName] = node;
            return node;
        }

        /// <summary>
        /// Gets all the nodes in the tree.
        /// </summary>
        /// <returns>The child nodes at this node.</returns>
        public IEnumerable<(string, ConfigNode)> Nodes()
        {
            foreach (var (key, value) in m_nodes)
                yield return (key, value);
        }

        /// <summary>
        /// Gets all the values at this node.
        /// </summary>
        /// <returns>All the config values at the node.</returns>
        public IEnumerable<(string, object)> Values()
        {
            foreach (var (key, value) in m_values)
                yield return (key, value);
        }

        public override string ToString() => m_parent == null ? Name : $"{m_parent}.{Name}";
    }
}
