using System;

namespace Helion.Util.Consoles.Commands;

public class ConsoleCommandAttribute : Attribute
{
    public readonly string Command;
    public readonly string Description;

    public ConsoleCommandAttribute(string command, string description)
    {
        Command = command;
        Description = description;
    }
}
