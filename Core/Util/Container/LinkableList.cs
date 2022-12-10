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
public class LinkableList<T>
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
    /// Adds a new element to the list. It is placed at the front.
    /// </summary>
    /// <remarks>
    /// It's placement in the list could be anywhere. You are only
    /// guaranteed that it is inserted into the list somewhere.
    /// </remarks>
    /// <param name="value">The value to add to the list.</param>
    /// <returns>The node created that contains the value.</returns>
    public LinkableNode<T> Add(T value)
    {
        return new(value, m_dummyHead);
    }

    /// <summary>
    /// Adds a node to the front of the list.
    /// </summary>
    /// <param name="node">The node to add.</param>
    public void Add(LinkableNode<T> node)
    {
        var previous = m_dummyHead;

        node.Next = previous.Next;
        node.Previous = previous;

        previous.Next = node;
        if (node.Next != null)
            node.Next.Previous = node;
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

    public IEnumerable<T> Enumerate()
    {
        LinkableNode<T>? node = Head;
        while (node != null)
        {
            yield return node.Value;
            node = node.Next;
        }
    }
}

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
    public LinkableNode<T> Previous;

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
        if (Next != null)
            Next.Previous = Previous!;
        Previous!.Next = Next;
    }
}
