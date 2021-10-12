using Helion.Resources.Definitions.Decorate.Flags;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Resources.Definitions.Decorate.States;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate;

public class ActorDefinition
{
    public readonly string Name;
    public readonly string? Parent;
    public readonly string? Replaces;
    public readonly int? EditorNumber;
    public readonly ActorFlags Flags = new ActorFlags();
    public readonly ActorProperties Properties = new ActorProperties();
    public readonly ActorFlagProperty FlagProperties = new ActorFlagProperty();
    public readonly ActorStates States = new ActorStates();

    public ActorDefinition(string name, string? parent, string? replaces, int? editorNumber)
    {
        Precondition(name.Length > 0, "Cannot have an empty actor definition name");
        Precondition(parent == null || parent.Length > 0, "Cannot have an empty actor parent name");
        Precondition(replaces == null || replaces.Length > 0, "Cannot have an empty actor replaces name");

        Name = name;
        Parent = parent;
        Replaces = replaces;
        EditorNumber = editorNumber;
    }

    public override string ToString() => $"{Name}";
}

