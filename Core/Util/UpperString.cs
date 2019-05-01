using Helion.Util.Extensions;
using System.Collections;
using System.Collections.Generic;

namespace Helion.Util
{
    /// <summary>
    /// An immutable string that contains only uppercase characters.
    /// </summary>
    /// <remarks>
    /// This class was made to give us an invariant that all the characters are
    /// in the upper case format. It is hard for us to guarantee in our code
    /// that it always is upper case when passing between many functions, so
    /// this class solves that issue.
    /// </remarks>
    public sealed class UpperString : IEnumerable<char>
    {
        private readonly string str;

        /// <summary>
        /// How many characters are in this string.
        /// </summary>
        public int Length => str.Length;

        public UpperString() => str = "";
        public UpperString(string s) => str = s.ToUpper();

        public static implicit operator UpperString(string s) => new UpperString(s);

        public char this[int index] => str[index];
        public static bool operator ==(UpperString self, UpperString other) => self.str == other.str;
        public static bool operator !=(UpperString self, UpperString other) => self.str != other.str;
        public static bool operator ==(UpperString self, string other) => self.str == other;
        public static bool operator !=(UpperString self, string other) => self.str != other;

        public bool Empty() => str.Empty();
        public bool NotEmpty() => str.NotEmpty();

        /// <summary>
        /// Checks if the current string ends with the string provided.
        /// </summary>
        /// <param name="ending">The ending to check.</param>
        /// <returns>True if it ends with the string, false if not.</returns>
        public bool EndsWith(string ending) => str.EndsWith(ending);

        /// <summary>
        /// Checks if the current string ends with the string provided.
        /// </summary>
        /// <param name="ending">The ending to check.</param>
        /// <returns>True if it ends with the string, false if not.</returns>
        public bool EndsWith(UpperString ending)
        {
            if (ending.Length > Length)
                return false;

            int thisIndex = Length - ending.Length;
            for (int endingIndex = 0; endingIndex < ending.Length; endingIndex++)
            {
                if (str[thisIndex] != ending[endingIndex])
                    return false;
                thisIndex++;
            }

            return true;
        }

        public override bool Equals(object obj) => obj is UpperString s && str == s.str && Length == s.Length;
        public override int GetHashCode() => str.GetHashCode();
        public override string ToString() => str;

        public IEnumerator<char> GetEnumerator() => str.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
