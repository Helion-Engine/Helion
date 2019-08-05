namespace Helion.Util.Parser
{
    /// <summary>
    /// An exception thrown by the parser.
    /// </summary>
    public class ParserException : HelionException
    {
        /// <summary>
        /// The line number in the source file that the parsing error occurred
        /// at. This starts at 1 (should never be zero).
        /// </summary>
        public readonly int LineNumber;
        
        /// <summary>
        /// The offset in characters from the start of the line. This starts
        /// from zero.
        /// </summary>
        public readonly int LineCharOffset;
        
        /// <summary>
        /// The offset in characters from the beginning of the text stream
        /// before it was tokenized. This starts from zero.
        /// </summary>
        public readonly int CharOffset;
        
        /// <summary>
        /// Creates a parser exception at some point in a text stream.
        /// </summary>
        /// <param name="lineNumber">The line number.</param>
        /// <param name="lineCharOffset">The character offset at the line.
        /// </param>
        /// <param name="charOffset">The character offset from the character
        /// stream.</param>
        /// <param name="message">The error message.</param>
        public ParserException(int lineNumber, int lineCharOffset, int charOffset, string message) : base(message)
        {
            LineNumber = lineNumber;
            LineCharOffset = lineCharOffset;
            CharOffset = charOffset;
        }

        /// <summary>
        /// Creates an exception at a provided token.
        /// </summary>
        /// <param name="token">The token that caused the problem.</param>
        /// <param name="message">The error message.</param>
        public ParserException(Token token, string message) : this(token.LineNumber, token.LineCharOffset, token.CharOffset, message)
        {
        }
    }
}