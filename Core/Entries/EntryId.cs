using System;

namespace Helion.Entries
{
    /// <summary>
    /// A lightweight entry ID, which represents a unique identifier in a
    /// project.
    /// </summary>
    public struct EntryId
    {
        /// <summary>
        /// The raw value for the entry.
        /// </summary>
        public ulong Value { get; }

        public EntryId(ulong id)
        {
            Value = id;
        }

        public static bool operator ==(EntryId self, EntryId other) => self.Value == other.Value;
        public static bool operator !=(EntryId self, EntryId other) => self.Value != other.Value;

        public override bool Equals(object obj) => obj is EntryId id && Value == id.Value;
        public override int GetHashCode() => HashCode.Combine(Value);
    }
}
