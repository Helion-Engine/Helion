namespace Helion.Util.Parser
{
    /// <summary>
    /// A token from a stream of characters.
    /// </summary>
    public struct Token
    {
        /// <summary>
        /// The line number this token was read on.
        /// </summary>
        public readonly int LineNumber;
        
        /// <summary>
        /// The character offset from the beginning of the line. Zero implies
        /// it is the first character.
        /// </summary>
        public readonly int CharOffset;
        
        /// <summary>
        /// The text for this token.
        /// </summary>
        public readonly string Text;
        
        /// <summary>
        /// The type of token this is.
        /// </summary>
        public readonly TokenType Type;

        /// <summary>
        /// Creates a new token.
        /// </summary>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="charOffset">The offset in characters from the left of
        /// the line.</param>
        /// <param name="text">The actual characters.</param>
        /// <param name="type">The type of token this is.</param>
        public Token(int lineNumber, int charOffset, string text, TokenType type)
        {
            LineNumber = lineNumber;
            CharOffset = charOffset;
            Text = text;
            Type = type;
        }
    }
}