using Helion.Resources;
using Helion.Util;

namespace Helion.ResourcesNew.Tracker
{
    /// <summary>
    /// A provider of resources for namespaces.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface INamespaceResourceProvider<out T>
    {
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
        T? Get(CIString name, Namespace priorityNamespace);

        /// <summary>
        /// Looks up the resource only from the namespace provided.
        /// </summary>
        /// <param name="name">The name of the resource. This is not intended
        /// to contain extensions, but rather just the name.</param>
        /// <param name="resourceNamespace">The namespace of the resource to
        /// only look at.</param>
        /// <returns>The value if it exists, empty otherwise.</returns>
        T? GetOnly(CIString name, Namespace resourceNamespace);
    }
}
