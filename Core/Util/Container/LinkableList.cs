using System.Collections;
using System.Collections.Generic;

namespace Helion.Util.Container;

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
    /// Adds a node to the front of the list.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public LinkableNode<T> Add(T value)
    {
        var node = LinkableNode<T>.Pool.Length > 0 ? LinkableNode<T>.Pool.RemoveLast() : new LinkableNode<T>(default!);
        node.Value = value;
        var previous = m_dummyHead;

        node.Next = previous.Next;
        node.Previous = previous;

        previous.Next = node;
        node.Next?.Previous = node;
        return node;
    }

    /// <summary>
    /// Checks if an object is contained (checks via Equals).
    /// </summary>
    /// <param name="obj">The object to check against.</param>
    /// <returns>True if is in the list, false otherwise.</returns>
    public bool Contains(T obj)
    {
        LinkableNode<T>? node = Head;
        while (node != null)
        {
            if (Equals(obj, node.Value))
                return true;
            node = node.Next;
        }

        return false;
    }

    /// <summary>
    /// Checks if an object is contained (checks via Equals).
    /// </summary>
    /// <param name="obj">The object to check against.</param>
    /// <returns>True if is in the list, false otherwise.</returns>
    public bool ContainsReference(T obj)
    {
        LinkableNode<T>? node = Head;
        while (node != null)
        {
            // TODO: This will break with structs? Use `where T : class`?
            if (ReferenceEquals(obj, node.Value))
                return true;
            node = node.Next;
        }

        return false;
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<T>
    {
        private readonly LinkableList<T> m_list;
        private LinkableNode<T>? m_node;
        private T? m_current;

        internal Enumerator(LinkableList<T> list)
        {
            m_list = list;
            m_node = list.Head;
            m_current = default;
        }

        public readonly T Current => m_current!;
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (m_node == null)
                return false;

            m_current = m_node.Value;
            m_node = m_node.Next;
            return true;
        }

        public void Reset() => m_node = m_list.Head;
        public readonly void Dispose() { }
    }
}

/// <summary>
/// A node in a <see cref="LinkableList{T}"/>, which gives full control to
/// the user for being able to unlink it safely.
/// </summary>
/// <typeparam name="T">The type to hold.</typeparam>
public class LinkableNode<T>
{
    internal static readonly DynamicArray<LinkableNode<T>> Pool = new(1024, capacityAlloc: () => new LinkableNode<T>(default!));

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
    public LinkableNode<T> Previous;

    public LinkableNode(T value)
    {
        Previous = null!;
        Next = null;
        Value = value;
    }

    /// <summary>
    /// Creates a dummy node which should only ever be used for the head of
    /// the list.
    /// </summary>
    internal LinkableNode()
    {
        // Due to how we implemented a linkable list, we need to have some
        // dummy node at the front to emulated a 'pointer to a pointer'
        // which you get in C.
        Previous = null!;
        Next = null;
        Value = default!;
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
        Previous = previous;

        previous.Next = this;
        if (Next != null)
            Next.Previous = this;
    }

    /// <summary>
    /// Unlinks this node from whatever linkable list it belongs to.
    /// </summary>
    public void Unlink()
    {
        Next?.Previous = Previous!;
        Previous!.Next = Next;
        Value = default!;
        Pool.Add(this);
    }
}
