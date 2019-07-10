using Helion.Util.Extensions;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Helion.Util
{
    /// <summary>
    /// An immutable string that is case-insensitive when doing compares.
    /// </summary>
    public sealed class CiString : IEnumerable<char>
    {
        private readonly string str;

        /// <summary>
        /// How many characters are in this string.
        /// </summary>
        public int Length => str.Length;

        public CiString() => str = string.Empty;
        public CiString(string s) => str = s;

        public static implicit operator CiString(string s) => new CiString(s);

        public char this[int index] => str[index];
        public static bool operator ==(CiString self, CiString other) => self.str.Equals(other.str, StringComparison.OrdinalIgnoreCase);
        public static bool operator !=(CiString self, CiString other) => !self.str.Equals(other.str, StringComparison.OrdinalIgnoreCase);
        public static bool operator ==(CiString self, string other) => self.str.Equals(other, StringComparison.OrdinalIgnoreCase);
        public static bool operator !=(CiString self, string other) => self.str.Equals(other, StringComparison.OrdinalIgnoreCase);

        public bool Empty() => string.IsNullOrEmpty(str);
        public override bool Equals(object obj) => obj is CiString s && str.Equals(s.str, StringComparison.OrdinalIgnoreCase);
        public override int GetHashCode() => str.GetHashCode();
        public override string ToString() => str;

        public IEnumerator<char> GetEnumerator() => str.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}