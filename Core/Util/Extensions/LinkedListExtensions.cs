using System;
using System.Collections.Generic;

namespace Helion.Util.Extensions
{
    /// <summary>
    /// A list of extensions for linked lists.
    /// </summary>
    public static class LinkedListExtensions
    {
        /// <summary>
        /// Checks if the list is empty or not.
        /// </summary>
        /// <param name="list">The list to check.</param>
        /// <typeparam name="T">The generic type.</typeparam>
        /// <returns>True if it has no elements, false otherwise.</returns>
        public static bool Empty<T>(this LinkedList<T> list) => list.Count == 0;

        /// <summary>
        /// Removes and returns the item from the linked list that are to be
        /// removed based on the predicate. This is useful when you want to
        /// cut out some items and then operate on the excised ones.
        /// </summary>
        /// <param name="list">The list to remove from.</param>
        /// <param name="predicate">The predicate that determines whether the
        /// item is to be unlinked or not. This is the same thing you'd put in
        /// a .Where() LINQ call.</param>
        /// <typeparam name="T">The type for the list.</typeparam>
        /// <returns>The list of items that have been removed which matched the
        /// predicate provided.</returns>
        public static IEnumerable<T> RemoveWhere<T>(this LinkedList<T> list, Func<T, bool> predicate)
        {
            List<T> removedItems = new List<T>();
            
            LinkedListNode<T>? node = list.First;
            while (node != null)
            {
                if (predicate(node.Value))
                {
                    removedItems.Add(node.Value);
                    list.Remove(node);
                }
                
                node = node.Next;
            }

            return removedItems;
        }
    }
}