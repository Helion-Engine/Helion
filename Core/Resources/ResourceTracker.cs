using System.Collections;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources
{
    /// <summary>
    /// Tracks a specific resource by a namespace and name combination.
    /// </summary>
    /// <remarks>
    /// This was a common pattern that crops up, where entities need to be
    /// found based on their name and namespace. This data structure solves
    /// that problem.
    /// </remarks>
    /// <typeparam name="T">The resource type to track.</typeparam>
    public class ResourceTracker<T> : IEnumerable<HashTableEntry<ResourceNamespace, CiString, T>> where T : class
    {
        private readonly HashTable<ResourceNamespace, CiString, T> m_table = new HashTable<ResourceNamespace, CiString, T>();

        /// <summary>
        /// Clears all the tracked resources.
        /// </summary>
        public void Clear()
        {
            m_table.Clear();
        }

        /// <summary>
        /// Checks if the resource exists for the provided name and namespace.
        /// This only checks the namespace, not other ones.
        /// </summary>
        /// <param name="name">The resource name.</param>
        /// <param name="resourceNamespace">The namespace for the resource.
        /// </param>
        /// <returns>True if it exists, false if not.</returns>
        public bool Contains(CiString name, ResourceNamespace resourceNamespace)
        {
            return m_table.Get(resourceNamespace, name) != null;
        }

        /// <summary>
        /// Adds the element to the resource tracker, or overwrites an existing
        /// reference if already mapped.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource.</param>
        /// <param name="value">The value to add or overwrite.</param>
        public void AddOrOverwrite(CiString name, ResourceNamespace resourceNamespace, T value)
        {
            m_table.AddOrOverwrite(resourceNamespace, name, value);
        }

        /// <summary>
        /// Removes the element if its in the map.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource.</param>
        public void Remove(CiString name, ResourceNamespace resourceNamespace)
        {
            m_table.Remove(resourceNamespace, name);
        }

        /// <summary>
        /// Looks up the resource only from the namespace provided.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource to
        /// only look at.</param>
        /// <returns>The value if it exists, empty otherwise.</returns>
        public T? GetOnly(CiString name, ResourceNamespace resourceNamespace)
        {
            return m_table.Get(resourceNamespace, name);
        }

        /// <summary>
        /// Looks up the resource from the namespace provided. If it fails then
        /// it will look in the global namespace.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource to
        /// look at first. This should not be the global namespace.</param>
        /// <returns>The value if it exists, empty otherwise.</returns>
        public T? GetWithGlobal(CiString name, ResourceNamespace resourceNamespace)
        {
            Precondition(resourceNamespace != ResourceNamespace.Global, $"Doing redundant 'get with global' check for: {name}");

            T? desiredNamespaceElement = m_table.Get(resourceNamespace, name);
            if (desiredNamespaceElement != null)
                return desiredNamespaceElement;
            return m_table.Get(ResourceNamespace.Global, name);
        }

        /// <summary>
        /// Looks up the resource the namespace provided, and then will check
        /// all the other namespaces for the resource. Priority is given to the
        /// namespace argument type first.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="priorityNamespace">The namespace of the resource to
        /// look at before checking others.</param>
        /// <returns>The value if it exists, empty otherwise.</returns>
        public T? GetWithAny(CiString name, ResourceNamespace priorityNamespace)
        {
            T? desiredNamespaceElement = m_table.Get(priorityNamespace, name);
            if (desiredNamespaceElement != null)
                return desiredNamespaceElement;

            foreach (ResourceNamespace resourceNamespace in m_table.GetFirstKeys())
            {
                if (resourceNamespace != priorityNamespace)
                {
                    T? resource = null;
                    if (m_table.TryGet(resourceNamespace, name, ref resource))
                        return resource;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public IEnumerator<HashTableEntry<ResourceNamespace, CiString, T>> GetEnumerator()
        {
            foreach (var tableEntry in m_table)
                yield return tableEntry;
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
