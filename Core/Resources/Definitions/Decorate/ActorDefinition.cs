using Helion.Util;

namespace Helion.Resources.Definitions.Decorate
{
    public class ActorDefinition
    {
        public readonly CIString Name;
        public readonly ActorFlags Flags = new ActorFlags();
        public readonly ActorProperties Properties = new ActorProperties();
        public readonly ActorStates States = new ActorStates();
        public readonly int? EditorNumber;

        public ActorDefinition(CIString name, int? editorNumber = null)
        {
            Name = name;
            EditorNumber = editorNumber;
        }
    }
}