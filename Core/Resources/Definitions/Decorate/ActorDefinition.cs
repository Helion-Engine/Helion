using Helion.Resources.Definitions.Decorate.Flags;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate
{
    public class ActorDefinition
    {
        public readonly CIString Name;
        public readonly CIString? Parent;
        public readonly CIString? Replaces;
        public readonly int? EditorNumber;
        public readonly ActorFlags Flags = new ActorFlags();
        public readonly ActorProperties Properties = new ActorProperties();
        public readonly ActorFlagProperty FlagProperties = new ActorFlagProperty();
        public readonly ActorStates States = new ActorStates();

        public ActorDefinition(CIString name, CIString? parent, CIString? replaces, int? editorNumber)
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
}