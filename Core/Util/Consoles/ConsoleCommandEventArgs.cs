using System;
using System.Collections.Generic;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Consoles
{
    /// <summary>
    /// An event fired by a console when the user submits an 'enter' character.
    /// </summary>
    public class ConsoleCommandEventArgs : EventArgs
    {
        /// <summary>
        /// The upper case command this event is.
        /// </summary>
        /// <remarks>
        /// This is always the first string in the command. For example, if the
        /// console was firing out "map map01" then the command would be "MAP".
        /// </remarks>
        public readonly string Command = "";

        /// <summary>
        /// The arguments (if any) that came with the command.
        /// </summary>
        public readonly List<string> Args = new();

        /// <summary>
        /// Parses the text provided into a console command event.
        /// </summary>
        /// <param name="text">The input to parse. This should not be empty.
        /// </param>
        public ConsoleCommandEventArgs(string text)
        {
            string[] tokens = text.Split(' ');
            if (tokens.Length == 0)
                return;

            Command = tokens[0];
            for (int i = 1; i < tokens.Length; i++)
                Args.Add(tokens[i]);
        }

        public override string ToString() => $"{Command} [{string.Join(", ", Args)}]";
    }
}
