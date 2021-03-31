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
        public static readonly CIString Empty = string.Empty;

        private readonly string str;

        public int Length => str.Length;
        public bool IsEmpty() => str.Empty();

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

        public override bool Equals(object? obj)
        {
            return obj switch
            {
                CIString otherCIStr => str.Equals(otherCIStr.str, StringComparison.OrdinalIgnoreCase),
                string otherStr => str.Equals(otherStr, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(str);

        public override string ToString() => str;

        public IEnumerator<char> GetEnumerator() => str.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}