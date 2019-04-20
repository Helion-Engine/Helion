namespace Helion.Entries
{
    /// <summary>
    /// An allocator for entry IDs.
    /// </summary>
    /// <remarks>
    /// <para>This is an extremely simplistic allocator right now since we will
    /// never have enough entries to overflow. Because of this, there will not
    /// be a case where entries will have clashing IDs and therefore will be
    /// safe to transmit through an internet protocol when we're trying to
    /// specifically find an entry.</para>
    /// <para>This exists over an entry path because some of the most common
    /// archives have duplicate values, so we need another way of uniquely
    /// identifying entries with the exact same path.</para>
    /// </remarks>
    public class EntryIdAllocator
    {
        private ulong nextId = 0;

        /// <summary>
        /// Allocates an unused ID.
        /// </summary>
        /// <returns>An ID that has not been used.</returns>
        public EntryId AllocateId()
        {
            return new EntryId(nextId++);
        }
    }
}
