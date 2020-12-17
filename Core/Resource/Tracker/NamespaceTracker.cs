using System.Collections;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Container;

namespace Helion.Resource.Tracker
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
    public class NamespaceTracker<T> : IEnumerable<(Namespace, CIString, T)> where T : class
    {
        private readonly HashTable<Namespace, CIString, T> m_table = new();

        /// <summary>
        /// Returns a count of all resources.
        /// </summary>
        public int Count => m_table.CountAll();

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
        public bool Contains(CIString name, Namespace resourceNamespace)
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
        public void Insert(CIString name, Namespace resourceNamespace, T value)
        {
            m_table.Insert(resourceNamespace, name, value);
        }

        /// <summary>
        /// Removes the element if its in the map.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource.</param>
        public void Remove(CIString name, Namespace resourceNamespace)
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
        public T? GetOnly(CIString name, Namespace resourceNamespace)
        {
            T? resource = null;
            return m_table.TryGet(resourceNamespace, name, ref resource) ? resource : null;
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
        public T? Get(CIString name, Namespace priorityNamespace)
        {
            T? desiredNamespaceElement = m_table.Get(priorityNamespace, name);
            if (desiredNamespaceElement != null)
                return desiredNamespaceElement;

            foreach (Namespace resourceNamespace in m_table.GetFirstKeys())
            {
                if (resourceNamespace == priorityNamespace)
                    continue;

                T? resource = null;
                if (m_table.TryGet(resourceNamespace, name, ref resource))
                    return resource;
            }

            return null;
        }

        public IEnumerator<(Namespace, CIString, T)> GetEnumerator() => m_table.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
