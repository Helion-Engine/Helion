using Helion.Entries;

namespace Helion.Resources
{
    /// <summary>
    /// An interface for objects that help classify what chunks of data are 
    /// with respect to any resources to be used.
    /// </summary>
    /// <remarks>
    /// This is an interface because there may be other resource classifiers
    /// needed for different situations. For example, we may want a classifier
    /// that detects everything for an engine or an editor.On the other hand,
    /// if we are running a server, we do not want to waste any extra time 
    /// classifying resources we will not be using such as images or sounds.
    /// 
    /// This would help servers classify faster because they will reduce the
    /// time spent classifying and it will also reduce any processing done from
    /// the classification result.
    /// </remarks>
    public interface ResourceClassifier
    {
        /// <summary>
        /// Classifies a resource by using the path and data to determine what 
        /// it likely is.
        /// </summary>
        /// <param name="path">The path of the resource.</param>
        /// <param name="data">The resource data.</param>
        /// <param name="Namespace">The namespace of the resource.</param>
        /// <returns>The resource type it is.</returns>
        ResourceType Classify(EntryPath path, byte[] data, ResourceNamespace Namespace);
    }
}
