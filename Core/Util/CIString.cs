using System;
using System.Collections;
using System.Collections.Generic;

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
        
        public CIString(string s) => str = s;

        public static implicit operator CIString(string s) => new CIString(s);

        public char this[int index] => str[index];
        
        public static bool operator ==(CIString self, CIString other) => self.str.Equals(other.str, StringComparison.OrdinalIgnoreCase);
        
        public static bool operator !=(CIString self, CIString other) => !self.str.Equals(other.str, StringComparison.OrdinalIgnoreCase);
        
        public static bool operator ==(CIString self, string other) => self.str.Equals(other, StringComparison.OrdinalIgnoreCase);
        
        public static bool operator !=(CIString self, string other) => !self.str.Equals(other, StringComparison.OrdinalIgnoreCase);

        public bool Empty() => string.IsNullOrEmpty(str);
        
        public override bool Equals(object obj) => obj is CIString s && str.Equals(s.str, StringComparison.OrdinalIgnoreCase);
        
        public override int GetHashCode() => str.GetHashCode();
        
        public override string ToString() => str;

        public IEnumerator<char> GetEnumerator() => str.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}