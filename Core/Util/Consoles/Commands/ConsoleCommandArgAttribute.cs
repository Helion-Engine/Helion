using System;

namespace Helion.Util.Consoles.Commands;

public class ConsoleCommandArgAttribute : Attribute
{
    public readonly string Name;
    public readonly string Description;
    public readonly bool Optional;

    public ConsoleCommandArgAttribute(string name, string description, bool optional = false)
    {
        Name = name;
        Description = description;
        Optional = optional;
    }
}

