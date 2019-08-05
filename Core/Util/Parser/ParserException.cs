using Helion.Util.Extensions;
using NLog;

namespace Helion.Util.Parser
{
    /// <summary>
    /// An exception thrown by the parser.
    /// </summary>
    public class ParserException : HelionException
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
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
        
        /// <summary>
        /// Takes the parsed text and the exception and logs a human readable
        /// error message.
        /// </summary>
        /// <param name="text">The entire parsed text.</param>
        public void LogToReadableMessage(string text)
        {
            if (text.Empty())
            {
                Log.Error("Cannot parse text when there are no tokens to read");
                return;
            }

            if (CharOffset < 0)
            {
                Log.Error("Unexpected character offset, cannot generate error message (report to a developer)");
                return;
            }

            if (CharOffset >= text.Length)
            {
                Log.Error("Error occurred past end of file, cannot generate error message (report to a developer)");
                return;
            }

            Log.Error("Error parsing text on line {0}, offset {1}:", LineNumber, LineCharOffset);
            LogContextualInformation(text);
        }
        
        private static int CalculateLeftIndex(string text, int originalIndex)
        {
            int startIndex = originalIndex;
            for (; startIndex > 0; startIndex--)
            {
                if (text[startIndex] == '\n')
                {
                    startIndex++;
                    break;
                }
            }
                
            // This keeps us in some reasonable range. We also don't need
            // to worry about it going negative because startIndex should
            // never go negative due to the loop exiting before that.
            return MathHelper.Clamp(startIndex, originalIndex - 256, originalIndex);
        }
            
        private static int CalculateRightNonInclusiveIndex(string text, int originalIndex)
        {
            int endIndex = originalIndex;
            for (; endIndex < text.Length; endIndex++)
            {
                if (text[endIndex] == '\n')
                {
                    endIndex--;
                    break;
                }
            }
                
            // This keeps us in some reasonable range. We also don't need
            // to worry about it going negative because startIndex should
            // never go negative due to the loop exiting before that.
            return MathHelper.Clamp(endIndex, originalIndex + 256, originalIndex);
        }

        private void LogContextualInformation(string text)
        {
            int leftIndex = CalculateLeftIndex(text, CharOffset);
            int rightIndexNonInclusive = CalculateRightNonInclusiveIndex(text, CharOffset);
            
            if (leftIndex == rightIndexNonInclusive)
            {
                Log.Error("Parsing error occurred on a blank line with no text, no contextual information available");
                return;
            }
            
            int numSpaces = CharOffset - leftIndex;
            string textContext = text.Substring(leftIndex, rightIndexNonInclusive);
            string caret = new string(' ', numSpaces) + "^";
            
            Log.Error(textContext);
            Log.Error(caret);
        }
    }
}