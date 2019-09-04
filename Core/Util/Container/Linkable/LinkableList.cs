using System.Collections;
using System.Collections.Generic;

namespace Helion.Util.Container.Linkable
{
    /// <summary>
    /// A special kind of linked list which allows the user to trivially unlink
    /// nodes and insert them in O(1) time without any references to the main
    /// list being needed.
    /// </summary>
    /// <remarks>
    /// This class is needed because we don't have 'pointer to pointer' fields
    /// available. We need a way to be able to unlink from the head of the list
    /// without having a reference to the list. We don't want the overhead of
    /// checking for whether we're the head or not either, which this solves by
    /// having a dummy node at the front which is invisible to the user, so our
    /// unlinking always has some previous node to unlink from (which emulates
    /// having a pointer to the previous 'next pointer').
    /// </remarks>
    /// <typeparam name="T">The type contained in the nodes.</typeparam>
    public class LinkableList<T> : IEnumerable<T>
    {
        private readonly LinkableNode<T> m_dummyHead;

        /// <summary>
        /// Gets the head of the list, if any.
        /// </summary>
        public LinkableNode<T>? Head => m_dummyHead.Next;
        
        /// <summary>
        /// Creates an empty linkable list.
        /// </summary>
        public LinkableList()
        {
            m_dummyHead = new LinkableNode<T>();
        }

        /// <summary>
        /// Adds a new element to the list.
        /// </summary>
        /// <remarks>
        /// It's placement in the list is undefined. You are only guaranteed
        /// that it is inserted into the list somewhere.
        /// </remarks>
        /// <param name="value">The value to add to the list.</param>
        /// <returns>The node created that contains the value.</returns>
        public LinkableNode<T> Add(T value)
        {
            return new LinkableNode<T>(value, m_dummyHead);
        }
        
        /// <summary>
        /// Checks if an object is contained (checks via reference).
        /// </summary>
        /// <param name="obj">The object to check against.</param>
        /// <returns>True if is in the list, false otherwise.</returns>
        public bool Contains(T obj)
        {
            LinkableNode<T>? node = Head;
            while (node != null)
            {
                if (ReferenceEquals(obj, node.Value))
                    return true;
                node = node.Next;
            }

            return false;
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            LinkableNode<T>? node = Head;
            while (node != null)
            {
                yield return node.Value;
                node = node.Next;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}