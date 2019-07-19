using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Container.Linkable
{
    /// <summary>
    /// A node in a <see cref="LinkableList{T}"/>, which gives full control to
    /// the user for being able to unlink it safely.
    /// </summary>
    /// <typeparam name="T">The type to hold.</typeparam>
    public class LinkableNode<T>
    {
        /// <summary>
        /// The value contained in this node.
        /// </summary>
        public T Value;
        
        /// <summary>
        /// The next element in the list.
        /// </summary>
        public LinkableNode<T>? Next;
        
        /// <summary>
        /// The previous element in the list. This value does not exist if it
        /// is the first dummy element in a list, but this is an implementation
        /// detail.
        /// </summary>
        private LinkableNode<T> m_previous;

        /// <summary>
        /// Creates a dummy node which should only ever be used for the head of
        /// the list.
        /// </summary>
        internal LinkableNode()
        {
#pragma warning disable nullable
            // Due to how we implemented a linkable list, we need to have some
            // dummy node at the front to emulated a 'pointer to a pointer'
            // which you get in C.
            m_previous = null;
            Next = null;
            Value = default;
#pragma warning restore nullable
        }

        /// <summary>
        /// Creates a new node from a value and links it to the node that will
        /// be the 'previous' node for this newly created one. This implies
        /// that the 'next' node after 'previous' (if any) will reference this
        /// node via it's 'previous'.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <param name="previous">The node to link after.</param>
        internal LinkableNode(T value, LinkableNode<T> previous)
        {
            Value = value;
            
            Next = previous.Next;
            m_previous = previous;

            previous.Next = this;
            if (Next != null)
                Next.m_previous = this;
        }

        /// <summary>
        /// Unlinks this node from whatever linkable list it belongs to.
        /// </summary>
        public void Unlink()
        {
            Precondition(m_previous != null, "Trying to call Unlink() on the first node in the linkable list");

            if (Next != null) 
                Next.m_previous = m_previous;
            m_previous.Next = Next;
        }
    }
}