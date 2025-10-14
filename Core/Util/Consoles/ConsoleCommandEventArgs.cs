using System;
using System.Collections.Generic;

namespace Helion.Util.Consoles;

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
    public readonly List<string> Args = [];

    /// <summary>
    /// Parses the text provided into a console command event.
    /// </summary>
    /// <param name="text">The input to parse. This should not be empty.
    /// </param>
    public ConsoleCommandEventArgs(string text)
    {
        var tokens = text.Contains('"') ? GetCommandLineArgs(text) : text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Count == 0)
            return;

        Command = tokens[0];
        for (int i = 1; i < tokens.Count; i++)
            Args.Add(tokens[i]);
    }

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
    private static IList<string> GetCommandLineArgs(string commandLine)
#pragma warning restore CA1859 // Use concrete types when possible for improved performance
    {
        List<string> args = [];
        int start = 0;
        int lastAddedIndex = -1;
        bool quote = false;
        for (int i = 0; i < commandLine.Length; i++)
        {
            if (!quote && commandLine[i] == ' ')
            {
                lastAddedIndex = i;
                if (i != start)
                    AddArg(args, commandLine[start..i]);
                start = i + 1;
            }

            if (commandLine[i] == '"')
            {
                if (quote)
                {
                    lastAddedIndex = i;
                    if (i != start)
                        AddArg(args, commandLine[start..i]);
                    quote = false;
                }
                else
                {
                    start = i + 1;
                    quote = true;
                }
            }
        }

        if (lastAddedIndex != commandLine.Length - 1)
            AddArg(args, commandLine[start..]);

        return args;
    }

    private static void AddArg(List<string> args, string text)
    {
        text = text.Trim();
        if (text.Length > 0)
            args.Add(text);
    }

    public override string ToString() => $"{Command} [{string.Join(", ", Args)}]";
}
