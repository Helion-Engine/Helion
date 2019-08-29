using Helion.Util;

namespace Helion.Resources.Definitions.Decorate
{
    public class ActorDefinition
    {
        public readonly CIString Name;
        public readonly CIString? Parent;
        public readonly CIString? ReplacesClass;
        public readonly int? EditorNumber;
        public readonly ActorFlags Flags = new ActorFlags();
        public readonly ActorProperties Properties = new ActorProperties();
        public readonly ActorStates States = new ActorStates();

        public ActorDefinition(CIString name, CIString? parent, CIString? replacesClass, int? editorNumber)
        {
            Name = name;
            Parent = parent;
            ReplacesClass = replacesClass;
            EditorNumber = editorNumber;
        }

        public bool IsValid()
        {
            // TODO
            return true;
        }
    }
}