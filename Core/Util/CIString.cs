using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Util.Extensions;

namespace Helion.Util
{
    /// <summary>
    /// An immutable string that operates with case-insensitivity.
    /// </summary>
    public sealed class CIString : IEnumerable<char>
    {
        private readonly string str;

        /// <summary>
        /// How many characters are in this string.
        /// </summary>
        public int Length => str.Length;

        public bool Empty => str.Empty();

        public CIString(string s)
        {
            str = s;
        }

        public static implicit operator CIString(string s) => new(s);

        public char this[int index] => str[index];
        
        public static bool operator ==(CIString? self, CIString? other)
        {
            if (ReferenceEquals(self, null) && ReferenceEquals(other, null))
                return true;
            if (ReferenceEquals(self, null) || ReferenceEquals(other, null))
                return false;
            return self!.str.Equals(other!.str, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(CIString? self, CIString? other)
        {
            return !(self == other);
        }
        
        public static bool operator ==(CIString? self, string? other)
        {
            if (ReferenceEquals(self, null) && ReferenceEquals(other, null))
                return true;
            if (ReferenceEquals(self, null) || ReferenceEquals(other, null))
                return false;
            return self!.str.Equals(other, StringComparison.OrdinalIgnoreCase);
        }
        
        public static bool operator !=(CIString? self, string? other)
        {
            return !(self == other);
        }

        public override bool Equals(object? obj) => obj is CIString s && str.Equals(s.str, StringComparison.OrdinalIgnoreCase);
        
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(str);

        public override string ToString() => str;

        public IEnumerator<char> GetEnumerator() => str.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}