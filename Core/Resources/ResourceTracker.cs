using Helion.Util;
using Helion.Util.Container;

namespace Helion.Resources
{
    // TODO: This probably should be called NamespaceTracker or something clearer...
    
    /// <summary>
    /// Tracks a specific resource by a namespace and name combination.
    /// </summary>
    /// <remarks>
    /// This was a common pattern that crops up, where entities need to be
    /// found based on their name and namespace. This data structure solves
    /// that problem.
    /// </remarks>
    /// <typeparam name="T">The resource type to track.</typeparam>
    public class ResourceTracker<T> where T : class
    {
        private readonly HashTable<ResourceNamespace, CIString, T> m_table = new HashTable<ResourceNamespace, CIString, T>();

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
        public bool Contains(CIString name, ResourceNamespace resourceNamespace)
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
        public void Insert(CIString name, ResourceNamespace resourceNamespace, T value)
        {
            m_table.Insert(resourceNamespace, name, value);
        }

        /// <summary>
        /// Removes the element if its in the map.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource.</param>
        public void Remove(CIString name, ResourceNamespace resourceNamespace)
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
        public T? GetOnly(CIString name, ResourceNamespace resourceNamespace)
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
        public T? Get(CIString name, ResourceNamespace priorityNamespace)
        {
            T? desiredNamespaceElement = m_table.Get(priorityNamespace, name);
            if (desiredNamespaceElement != null)
                return desiredNamespaceElement;

            foreach (ResourceNamespace resourceNamespace in m_table.GetFirstKeys())
            {
                if (resourceNamespace == priorityNamespace) 
                    continue;
                
                T? resource = null;
                if (m_table.TryGet(resourceNamespace, name, ref resource))
                    return resource;
            }

            return null;
        }
    }
}